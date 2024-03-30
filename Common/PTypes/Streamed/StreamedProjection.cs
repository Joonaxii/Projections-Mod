using System;
using System.Runtime.InteropServices;
using Projections.Core.Systems;
using System.IO;
using Projections.Core.Data.Structures;
using System.Collections.Generic;
using Projections.Core.Data;
using Projections.Core.Textures;
using Projections.Core.Utilities;
using Projections.Core.Maths;
using Projections.Common.PTypes.OnDisk;

namespace Projections.Common.PTypes.Streamed
{
    /// <summary>
    /// Projection class My own format for Streamed version of the <see cref="Projection"/>.
    /// </summary>
    public sealed class StreamedProjection : OnDiskProjection
    {
        public const LayerFlags LAYER_IS_TRANSPARENT = (LayerFlags)0x0001_0000U;

        public ushort TransparencyMask
        {
            get
            {
                ushort state = 0;
                for (int i = 0, j = 1; i < Math.Min(_layers.Count, MAX_LAYERS); i++, j <<= 1)
                {
                    if ((_layers[i].Flags & LAYER_IS_TRANSPARENT) != 0)
                    {
                        state |= (ushort)j;
                    }
                }
                return state;
            }
        }
        public override ref readonly AudioInfo AudioInfo => ref _audioInfo;

        public override PMaterial Material => _material;

        public override int Width => _width;
        public override int Height => _height;
        public override int FrameCount => _layers.Count;
        public override int MaskCount => _masks.Length;
        public override AnimationMode AnimationMode => _animMode;

        public override float Duration
        {
            get
            {
                return _animMode switch
                {
                    AnimationMode.LoopAudio => HasAudio ? _durationA : _durationV,
                    AnimationMode.LoopVideo => _durationV,
                    _ => 0,
                };
            }
        }
        public override float LoopTime => _loopTime;
        internal ReadOnlySpan<Frame> Frames => _frames;

        private float _loopTime;
        private AnimationMode _animMode;
        private int _width;
        private int _height;

        private Frame[] _frames = new Frame[0];
        private FramePointer[] _framesPtrs = new FramePointer[0];
        private Color32[] _palette = new Color32[0];
        private FrameMask[] _masks = new FrameMask[0];

        private AudioInfo _audioInfo;
        private long _audioPosition;

        private float _durationV;
        private float _durationA;

        internal StreamedProjection(string path, uint id) : base(path, id) { }

        protected override void OnFirstUserIn() => OpenStream();
        protected override void OnLastUserOut() => CloseStream();

        protected override bool OnLoad()
        {
            OpenStream();
            bool result = Deserialize(Reader, IOStream);
            CloseStream();
            return result;
        }
        protected override void OnUnload()
        {
            CloseStream();
            _material.Unload();
        }

        public override float GetFrameDuration(int frame)
        {
            if (frame < 0 || frame >= FrameCount) { return 0.0f; }
            return _frames[frame].duration;
        }

        public override bool GetFrame(ushort layerMask, int frame, int mask, ProjectorTexture texture)
        {
            texture.Setup(1);
            if (frame < 0 || frame >= _frames.Length || layerMask == 0)
            {
                goto failure;
            }
            ref var fInfo = ref _frames[frame];

            if (!ProjectorSystem.GetFrameBuffers(Width, Height, Layers.Length, out Span<byte> indexBuffer, out var tempBuffer, out var colorBuffer, out var alphaBuffer))
            {
                goto failure;
            }

            int reso = _width * _height;
            Span<byte> maskBuf = default;
            bool useMask = mask > -1 && mask < _masks.Length;

            if (useMask)
            {
                //useMask &= _masks[mask].ReadMask(IOStream, maskBuf);
            }

            ReadFrameFlags flags = ReadFrameFlags.None;
            unsafe
            {
                fixed (Color32* tgtPtr = colorBuffer)
                fixed (Color32* bufPtr = tempBuffer)
                fixed (byte* maskPtr = maskBuf)
                {
                    if (fInfo.ReadFrame(false, Layers.Length, reso, TransparencyMask, layerMask, IOStream, indexBuffer, _palette, colorBuffer, tempBuffer, alphaBuffer))
                    {
                        if(useMask)
                        {
                            byte* mPtr = maskPtr;
                            Color32* tPtr = tgtPtr;

                            int temp = reso;
                            while(temp-- > 0)
                            {
                                MathLow.Mult(ref *tPtr++, *mPtr++);
                            }
                        }

                        texture.SetLayer(new IntPtr(tgtPtr), false, Width, Height);
                        flags |= ReadFrameFlags.Diffuse;
                    }

                    if (fInfo.ReadFrame(true, Layers.Length, reso, TransparencyMask, layerMask, IOStream, indexBuffer, _palette, colorBuffer, tempBuffer, alphaBuffer))
                    {
                        if (useMask)
                        {
                            byte* mPtr = maskPtr;
                            Color32* tPtr = tgtPtr;

                            int temp = reso;
                            while (temp-- > 0)
                            {
                                MathLow.Mult(ref *tPtr++, *mPtr++);
                            }
                        }

                        texture.SetLayer(new IntPtr(tgtPtr), true, Width, Height);
                        flags |= ReadFrameFlags.Emission;
                    }
                }
            }
            texture.AddLayer();
            return flags != ReadFrameFlags.None;

        failure:
            texture.AddLayer(IntPtr.Zero, IntPtr.Zero, Width, Height);
            return false;
        }
        public override bool GetAudio(int variant, long sample, Span<byte> buffer, ref AudioState sampleRead)
        {
            ref readonly var audioI = ref AudioInfo;
            sample = Math.Min(sample, audioI.sampleCount);

            long byteLen = audioI.ByteLength;
            long bytePos = sample * 2 * (audioI.stereo ? 2 : 1);
            long streamPos = _audioPosition + bytePos + _audioInfo.GetOffset(variant);
            long left = byteLen - bytePos;

            if (_audioInfo.stereo && (left & 0x1) != 0)
            {
                left--;
            }

            int len = (int)Math.Min(left, _audioInfo.BytesPerSec);
            len = Math.Min(len, buffer.Length);

            if (len <= 0)
            {
                sampleRead.data = buffer.Slice(0, 0);
                return false;
            }

            IOStream.Seek(streamPos, SeekOrigin.Begin);
            sampleRead.data = buffer.Slice(0, len);
            IOStream.Read(sampleRead.data);

            int samplesRead = sampleRead.data.Length >> 1;
            if (_audioInfo.stereo)
            {
                samplesRead >>= 1;
            }
            sampleRead.position += samplesRead;
            return samplesRead > 0;
        }

        private static bool ContainsTag(IList<int> tags, int length, int value)
        {
            for (int i = 0; i < length; i++)
            {
                if (tags[i] == value) { return true; }
            }
            return false;
        }
        public override bool Deserialize(BinaryReader br, Stream stream)
        {
            _loopTime = Math.Max(br.ReadSingle(), 0.0f);

            _width = br.ReadInt32();
            _height = br.ReadInt32();
            _animMode = (AnimationMode)br.ReadByte();

            int stackT = Math.Max(br.ReadInt32(), 0);
            _stackThresholds.Resize(stackT);
            for (int i = 0; i < stackT; i++)
            {
                _stackThresholds[i].Deserialize(br);
            }

            int tagCount = br.ReadUInt16();
            _tags.Clear();
            _tags.Reserve(stackT);

            Span<char> tagBuf = stackalloc char[256];
            int aPos = 0;
            for (int i = 0; i < tagCount; i++)
            {
                int tag = Projections.AddTag(br.ReadShortString(tagBuf));
                if (tag < 0 || ContainsTag(_tags, aPos, tag)) { continue; }
                _tags.Add(tag);
            }

            int layerCount = Math.Max(br.ReadInt32(), 1);
            _layers.Resize(layerCount);
            if (layerCount <= 0)
            {
                _layers.Add(new Layer("Default", LayerFlags.DefaultOn, 1));
            }

            for (int i = 0; i < layerCount; i++)
            {
                _layers[i].Deserialize(br);
            }

            layerCount = Math.Min(layerCount, MAX_LAYERS);
            _layers.Resize(layerCount);

            int frameCount = br.ReadInt32();
            int fPtrScan = layerCount * 2;
            Array.Resize(ref _frames, frameCount);
            Array.Resize(ref _framesPtrs, frameCount * fPtrScan);

            for (int i = 0; i < _framesPtrs.Length; i++) 
            {
                _framesPtrs[i].Clear();
            }

            Span<FramePointer> fPtr = _framesPtrs;

            _durationV = 0;
            for (int i = 0, j = 0; i < frameCount; i++ , j += fPtrScan)
            {
                ref var frame = ref _frames[i];
                frame.Deserialize(layerCount, br, stream, _frames);
                _durationV += frame.duration;
            }
            Array.Resize(ref _palette, br.ReadInt32());
            stream.Read(MemoryMarshal.AsBytes(_palette.AsSpan()));

            Array.Resize(ref _masks, br.ReadInt32());
            for (int i = 0; i < _masks.Length; i++)
            {
                _masks[i].Deserialize(br, stream);
            }

            _audioInfo.type = br.Read<AudioType>();
            _audioInfo.stereo = (_audioInfo.type & AudioType.__StereoFlag) != 0;
            _audioInfo.sampleRate = br.ReadUInt32();
            _audioInfo.sampleCount = br.ReadInt64();
            _audioInfo.variants = br.ReadUInt16();
            _audioInfo.Setup();
            _audioPosition = stream.Position;
            stream.Seek(_audioInfo.variants * _audioInfo.ByteLength, SeekOrigin.Current);

            _durationA = HasAudio ? _audioInfo.sampleCount / (float)_audioInfo.sampleRate : 0.0f;
            return true;
        }

        internal float LoopTimeFrame(float time)
        {
            if (_durationV <= 0) { return 0; }
            return time % _durationV;
        }
        internal float LoopTimeAudio(float time)
        {
            if (_durationA <= 0) { return 0; }
            return time % _durationA;
        }

        internal int TimeToFrame(float time)
        {
            if (_durationV <= 0) { return 0; }
            while (time > _durationV)
            {
                time -= _durationV;
            }

            int curI = 0;
            unsafe
            {
                float accu = 0;
                fixed (Frame* fPtr = _frames)
                {
                    int count = _frames.Length;
                    var frames = fPtr;

                    while (count-- > 0)
                    {
                        if (accu > time) { break; }
                        accu += frames++->duration;
                        curI++;
                    }
                }
            }
            return curI;
        }

        protected override PMaterial GetMaterial(string path, uint id) => new StreamedPMaterial(path, id);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct FramePointer
        {
            public static FramePointer Empty { get; } = default;

            public bool IsEmpty => position <= 0;

            internal const int SIZE = 15;
            public FrameTarget target;
            public FrameFormat format;
            public ushort pOffset;
            public long position;

            public void Clear()
            {
                target = FrameTarget.Zero;
                format = FrameFormat.RGBA32;
                pOffset = 0;
                position = 0;
            }

            public void Deserialize(BinaryReader br)
            {
                target.Deserialize(br);
                format = br.Read<FrameFormat>();
                pOffset = (ushort)((format == FrameFormat.Indexed8 || format == FrameFormat.Indexed16) ? br.ReadUInt16() : 0);
                position = 0;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct Frame
        {
            //private const int EMISSION_OFFSET = MAX_LAYERS;

            public FrameFlags flags;
            public int offset; 
            public float duration;
            //private fixed uint _frameInfo[StreamedProjection.MAX_LAYERS * FramePointer.SIZE * 2];

            public void Deserialize(int offset, int layers, BinaryReader br, Stream stream, FramePointer* ptrs)
            {
                this.offset = offset;
                flags = (FrameFlags)br.ReadUInt32();
                duration = br.ReadSingle();

                int scan = layers * 2;
                long pos = stream.Position;
                var fPtr = ptrs + offset;
                {
                    for (int i = 0; i < layers; i++)
                    {
                        pos = ParseFrame(pos, br, stream, fPtr + i, frames);
                        pos = ParseFrame(pos, br, stream, fPtr + j, frames);
                    }
                }
            }
            public bool ReadFrame(bool isEmissive, int layers, int reso, ushort transparencyMask, ushort layerMask, Stream stream, Span<byte> indexBuffer, ReadOnlySpan<Color32> palette, Span<Color32> target, Span<Color32> buffer, Span<byte> alpha)
            {
                bool returnVal = false;
                FramePointer* framePtr = AsFramePtr();
                target.Memset(default);

                if (isEmissive)
                {
                    framePtr += MAX_LAYERS;
                    returnVal |= ReadFrameData(in *framePtr++, reso, stream, target, indexBuffer, palette);

                    if (layers > 1)
                    {
                        UnsafeMath.ApplyMask(target, alpha.Slice(0, reso));
                        for (int i = 1, j = reso, k = 1; i < layers; i++, j += reso, k <<= 1)
                        {
                            if ((layerMask & k) != 0 && ReadFrameData(in *framePtr++, reso, stream, buffer, indexBuffer, palette))
                            {
                                UnsafeMath.AlphaBlend(target, buffer, alpha.Slice(j, reso));
                                returnVal = true;
                            }
                        }
                    }
                    return returnVal;
                }

                returnVal |= ReadFrameData(in *framePtr++, reso, stream, target, indexBuffer, palette);
                if (layers > 1)
                {
                    alpha.Slice(0, layers * reso).Memset<byte>(0xFF);
                    for (int i = 1, k = 1; i < layers; i++, k <<= 1)
                    {
                        if ((layerMask & k) != 0 && ReadFrameData(in *framePtr++, reso, stream, buffer, indexBuffer, palette))
                        {
                            if ((transparencyMask & k) != 0)
                            {
                                MathLow.GenerateAlphaMap(alpha, buffer, reso, i);
                            }
                            UnsafeMath.AlphaBlend(target, buffer);
                            returnVal = true;
                        }
                    }
                }
                return returnVal;
            }

            private long ParseFrame(long position, FramePointer* self, BinaryReader br, Stream stream, int selfOff, FramePointer* allPtrs, int scan)
            {
                int prevInd = -1;
                self->Deserialize(br);

                position += 4;
                FramePointer* fPtrs = self;
                while (fPtrs->target.IsTargeted)
                {
                    int targetI = fPtrs->target.Value;
                    if (prevInd == targetI)
                    {
                        *ptrTo = FramePointer.Empty;
                        break;
                    }
                    prevInd = targetI;
                    fPtrs = allPtrs + (targetI * scan) + (fPtrs->target.AuxValue * 2) + (fPtrs->target.AuxFlag ? 1 : 0);
                }

                if (ptrTo == fPtrs)
                {
                    ptrTo->position = position;
                    stream.Seek(ptrTo->target.SeekAmount, SeekOrigin.Current);
                    position += ptrTo->target.SeekAmount;
                }
                else
                {
                    *ptrTo = *fPtrs;
                }
                return position;
            }
            private static bool ReadFrameData(in FramePointer ptr, int reso, Stream stream, Span<Color32> data, Span<byte> indexBuffer, ReadOnlySpan<Color32> palette)
            {
                if (ptr.IsEmpty)
                {
                    return false;
                }

                var pixels = data.Slice(0, reso);
                var ui8Index = indexBuffer.Slice(0, reso);
                var ui16Index = MemoryMarshal.Cast<byte, ushort>(indexBuffer).Slice(0, reso);

                stream.Seek(ptr.position, SeekOrigin.Begin);

                //Is Compressed with RLE
                if (ptr.target.AuxFlag)
                {
                    switch (ptr.format)
                    {
                        case FrameFormat.RGBA32:
                            Texture2DUtils.LoadRLE(stream, pixels, reso);
                            break;
                        case FrameFormat.Indexed8:
                            Texture2DUtils.LoadRLE(stream, ui8Index, reso);
                            for (int i = 0; i < reso; i++) 
                            {
                                pixels[i] = palette[ui8Index[i]];
                            }
                            break;
                        case FrameFormat.Indexed16:
                            Texture2DUtils.LoadRLE(stream, ui16Index, reso);
                            for (int i = 0; i < reso; i++)
                            {
                                pixels[i] = palette[ui16Index[i]];
                            }
                            break;
                    } 
                }
                else
                {
                    switch (ptr.format)
                    {
                        case FrameFormat.RGBA32:
                            stream.Read(MemoryMarshal.AsBytes(pixels));
                            break;
                        case FrameFormat.Indexed8:
                            stream.Read(ui8Index);
                            for (int i = 0; i < reso; i++)
                            {
                                pixels[i] = palette[ui8Index[i]];
                            }
                            break;
                        case FrameFormat.Indexed16:
                            stream.Read(MemoryMarshal.AsBytes(ui16Index));
                            for (int i = 0; i < reso; i++)
                            {
                                pixels[i] = palette[ui16Index[i]];
                            }
                            break;
                    }
                }
                return true;
            }
        }
    }
}