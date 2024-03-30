using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Maths;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Projections.Core.Textures
{
    public static class Texture2DUtils
    {
        private static byte[] _buffer = new byte[4096 * 4096 * 4];

        enum TextureFormat : byte
        {
            Unknown,

            R8,
            RGB24,
            RGB48,
            RGBA32,
            RGBA64,
            Indexed8,
            Indexed16,

            RGBA4444,

            Count,
        }

        enum JTEXFlags : uint
        {
            JTEX_None,
            JTEX_Compressed = 0x1,
        }

        public const uint JTEX_SIG = 0x5845544AU;
        public const uint DDS_SIG = 0x20534444U;

        [StructLayout(LayoutKind.Sequential, Size = 5, Pack = 1)]
        public struct RLEHeader
        {
            public int Width => _width;
            public int Height => _height;

            private byte _flags; //The flags are currently not used but could be used for palette related flags in the future
            private int _width;
            private int _height;

            public RLEHeader(int width, int height) : this()
            {
                _width = width;
                _height = height;
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 22, Pack = 1)]
        private unsafe struct JTEXHeader
        {
            public uint signature;
            public JTEXFlags flags;
            public int width;
            public int height;
            public TextureFormat format;
            public int paletteSize;
            public byte imgFlags;
        }

        private unsafe struct DDSFormat
        {
            public uint flags;
            public uint compression;
            public uint bitCount;
            public fixed uint compMasks[4];
        }

        public static Texture2D LoadJTEX(GraphicsDevice device, Stream stream)
        {
            Span<byte> header = stackalloc byte[22];
            stream.Read(header);

            var hdr = MemoryMarshal.Read<JTEXHeader>(header);
            if (hdr.signature != JTEX_SIG)
            {
                return null;
            }

            SurfaceFormat fmt = SurfaceFormat.Color;
            Texture2D tex = null;
            int size = 0;
            switch (hdr.format)
            {
                default: return null;
                case TextureFormat.RGB24:
                    {
                        fmt = SurfaceFormat.Color;
                        Span<byte> scan = stackalloc byte[hdr.width * 3];
                        size = hdr.width * hdr.height * 4;
                        int scanSize = hdr.width * 4;
                        Span<uint> c32 = MemoryMarshal.Cast<byte, uint>(_buffer);
                        for (int y = 0, yP = 0; y < hdr.height; y++, yP += hdr.width)
                        {
                            stream.Read(scan);
                            var tmp = c32.Slice(yP, hdr.width);
                            for (int x = 0, xP = 0; x < hdr.width; x++, xP += 3)
                            {
                                ref var clr = ref tmp[x];
                                clr = (uint)scan[xP + 0] << 16;
                                clr |= (uint)scan[xP + 1] << 8;
                                clr |= (uint)scan[xP + 2] << 0;
                                clr |= 0xFF000000U;
                            }
                        }
                        break;
                    }
                case TextureFormat.RGBA32:
                    {
                        fmt = SurfaceFormat.Color;
                        size = hdr.width * hdr.height * 4;
                        stream.Read(_buffer.AsSpan(0, size));
                        break;
                    }
                case TextureFormat.RGBA4444:
                    fmt = SurfaceFormat.Bgra4444;
                    size = hdr.width * hdr.height * 2;
                    stream.Read(_buffer.AsSpan(0, size));
                    break;
            }

            tex = new Texture2D(device, hdr.width, hdr.height, false, fmt);
            tex.SetData(_buffer, 0, size);
            return tex;
        }

        public static Texture2D LoadDDS(GraphicsDevice device, Stream stream)
        {
            SurfaceFormat format = SurfaceFormat.Color;
            Span<byte> temp = stackalloc byte[8];
            long dataStart = stream.Position;
            stream.Seek(4, SeekOrigin.Current);
            stream.Read(temp.Slice(0, 8));

            dataStart += MemoryMarshal.Read<uint>(temp.Slice(0, 4)) + 4;
            stream.Read(temp.Slice(0, 8));

            int width = MemoryMarshal.Read<int>(temp.Slice(4, 4));
            int height = MemoryMarshal.Read<int>(temp.Slice(0, 4));
            stream.Seek(60, SeekOrigin.Current);

            DDSFormat fmt = default;
            stream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref fmt, 1)));

            int pitch = 0;
            int bpp = 0, bppEnd = 0;
            switch (fmt.bitCount)
            {
                default: return null;

                case 16:
                    format = SurfaceFormat.Bgra4444;
                    pitch = (width * 16 + 7) / 8;
                    bpp = 2;
                    bppEnd = 2;
                    break;
                case 24:
                    format = SurfaceFormat.Color;
                    pitch = (width * 24 + 7) / 8;
                    bpp = 3;
                    bppEnd = 4;
                    break;
                case 32:
                    format = SurfaceFormat.Color;
                    pitch = (width * 32 + 7) / 8;
                    bpp = 4;
                    bppEnd = 4;
                    break;
            }

            Span<byte> scan = stackalloc byte[pitch];
            Span<byte> buf = _buffer;
            int scanSizeR = width * bpp;
            int scanSizeE = width * bppEnd;
            unsafe
            {
                Span<int> maskOffsets = stackalloc int[4]
                {
                    MathLow.FindFirstLSB(fmt.compMasks[0]),
                    MathLow.FindFirstLSB(fmt.compMasks[1]),
                    MathLow.FindFirstLSB(fmt.compMasks[2]),
                    MathLow.FindFirstLSB(fmt.compMasks[3]),
                };

                stream.Seek(dataStart, SeekOrigin.Begin);
                int reso = width * height;
                switch (format)
                {
                    case SurfaceFormat.Color:
                        {
                            Span<Color> bufC32 = MemoryMarshal.Cast<byte, Color>(buf.Slice(0, width * height * 4));
                            for (int y = 0, yP = 0, yPP = 0; y < height; y++, yP += scanSizeE, yPP += width)
                            {
                                var tmp = buf.Slice(yP, scanSizeE);
                                stream.Read(scan);
                                if (bpp == 32)
                                {
                                    scan.Slice(0, scanSizeE).CopyTo(tmp);
                                    continue;
                                }

                                var tmp32 = bufC32.Slice(yPP, width);
                                for (int x = 0, xP = 0; x < width; x++, xP += bpp)
                                {
                                    tmp32[x] = new Color(scan[xP], scan[xP + 1], scan[xP + 2]);
                                }
                            }

                            if (bpp == 32)
                            {
                                for (int i = 0; i < reso; i++)
                                {
                                    ref Color clr = ref bufC32[i];
                                    uint packed = clr.PackedValue;

                                    clr.R = (byte)((packed & fmt.compMasks[0]) >> maskOffsets[0]);
                                    clr.G = (byte)((packed & fmt.compMasks[1]) >> maskOffsets[1]);
                                    clr.B = (byte)((packed & fmt.compMasks[2]) >> maskOffsets[2]);
                                    clr.A = (byte)((packed & fmt.compMasks[3]) >> maskOffsets[3]);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < reso; i++)
                                {
                                    ref Color clr = ref bufC32[i];
                                    uint packed = clr.PackedValue;

                                    clr.R = (byte)((packed & fmt.compMasks[0]) >> maskOffsets[0]);
                                    clr.G = (byte)((packed & fmt.compMasks[1]) >> maskOffsets[1]);
                                    clr.B = (byte)((packed & fmt.compMasks[2]) >> maskOffsets[2]);
                                    clr.A = 0xFF;
                                }
                            }

                            break;
                        }
                    case SurfaceFormat.Bgra4444:
                        {
                            for (int y = 0, yP = 0, yPP = 0; y < height; y++, yP += scanSizeE, yPP += width)
                            {
                                var tmp = buf.Slice(yP, scanSizeE);
                                stream.Read(scan);
                                scan.Slice(0, scanSizeE).CopyTo(tmp);
                            }

                            Span<ushort> bufC16 = MemoryMarshal.Cast<byte, ushort>(buf.Slice(0, width * height * 2));
                            for (int i = 0; i < reso; i++)
                            {
                                ref ushort clr = ref bufC16[i];
                                uint packed = clr;
                                clr = (ushort)((packed & fmt.compMasks[0]) >> maskOffsets[0]);
                                clr |= (ushort)((packed & fmt.compMasks[1]) >> maskOffsets[1] << 4);
                                clr |= (ushort)((packed & fmt.compMasks[2]) >> maskOffsets[2] << 8);
                                clr |= (ushort)((packed & fmt.compMasks[2]) >> maskOffsets[2] << 12);
                            }
                            break;
                        }
                }

                Texture2D tex = new Texture2D(device, width, height, false, format);
                tex.SetData(_buffer, 0, width * height * bppEnd);
                return tex;
            }
        }

        public static Span<T> LoadRLE<T>(Stream stream, Span<T> buffer, ref RLEHeader header) where T : unmanaged
        {
            if (header.Width <= 0 || header.Height <= 0)
            {
                var hdr = MemoryMarshal.CreateSpan(ref header, 1);
                stream.Read(MemoryMarshal.AsBytes(hdr));
            }
            return LoadRLE(stream, buffer, header.Width * header.Height);
        }

        public static Span<T> LoadRLE<T>(Stream stream, Span<T> buffer, int reso) where T : unmanaged
        {
            int pos = 0;
            var bufArea = buffer.Slice(0, reso);

            int runLen = 0;
            T runPixel = default;

            const int READ_BUFFER_SIZE = 4096;
            Span<byte> readBuffer = stackalloc byte[READ_BUFFER_SIZE];
            int bufferLeft = 0;
            int bufferPos = 0;

            // We will assume the pixel/RLE data is 100% intact
            unsafe
            {
                int tSize = sizeof(T);
                int minReq = tSize + 1;
                int maxReq = tSize + 3;

                fixed (T* pPtr = bufArea)
                fixed (byte* rBuf = readBuffer)
                {
                    T* cPtr = pPtr;
                    while (pos < reso)
                    {
                        if (bufferLeft < maxReq)
                        {
                            if (bufferLeft != 0)
                            {
                                stream.Seek(-bufferLeft, SeekOrigin.Current);
                            }
                            if (stream.Read(readBuffer) < minReq)
                            {
                                break;
                            }
                            bufferPos = 0;
                        }

                        byte rle = rBuf[bufferPos];
                        runLen = (rle >> 2);
                        switch (rle & 0x3)
                        {
                            default: // Run (Max 64)
                                bufferPos++;
                                break;
                            case 1: // Run (Max 16 384)
                                runLen |= (rBuf[bufferPos++] << 6);
                                break;
                            case 2: // Run (Max 4 194 304)
                                runLen |= (rBuf[bufferPos++] << 6) | (rBuf[bufferPos++] << 14);
                                break;
                            case 3: // Run (Max 1 073 741 824)
                                runLen |= (rBuf[bufferPos++] << 6) | (rBuf[bufferPos++] << 14) | (rBuf[bufferPos++] << 22);
                                break;
                        }
                        runLen++;
                        runPixel = *(T*)(rBuf + bufferPos);
                        bufferPos += tSize;

                        // If the run length is long enough, we can unroll the loop partially.
                        if (runLen > 32)
                        {
                            int lenDiv = runLen >> 3;
                            int leftOver = runLen - (lenDiv << 3);

                            while (lenDiv-- > 0)
                            {
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                                *cPtr++ = runPixel;
                            }

                            while (leftOver-- > 0)
                            {
                                *cPtr++ = runPixel;
                            }
                        }
                        else
                        {
                            //Finally apply the run length to the next N pixels based on the run's length
                            for (int i = 0; i < runLen; i++)
                            {
                                *cPtr++ = runPixel;
                            }
                        }

                        bufferLeft = READ_BUFFER_SIZE - bufferPos;
                    }
                    return bufArea;
                }
            }
        }

        public static Texture2D LoadRLE(GraphicsDevice device, Stream stream)
        {
            RLEHeader header = default;
            LoadRLE(stream, MemoryMarshal.Cast<byte, Color>(_buffer), ref header);

            var tex = new Texture2D(device, header.Width, header.Height, false, SurfaceFormat.Color);
            tex.SetData(_buffer, 0, header.Width * header.Height << 2);
            return tex;
        }
    }
}