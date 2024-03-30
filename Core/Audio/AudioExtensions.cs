using SDL2;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Projections.Core.Audio
{
    internal class FAudioContextDummy
    {
        public static FAudioContextDummy Context;

        public readonly IntPtr Handle;

        public readonly byte[] Handle3D;

        public readonly IntPtr MasterVoice;

        public readonly FAudio.FAudioDeviceDetails DeviceDetails;

        public float CurveDistanceScaler;

        public float DopplerScale;

        public float SpeedOfSound;

        private FAudioContextDummy(IntPtr ctx, uint devices)
        {
            Handle = ctx;
            uint num;
            for (num = 0u; num < devices; num++)
            {
                FAudio.FAudio_GetDeviceDetails(Handle, num, out DeviceDetails);
                if ((DeviceDetails.Role & FAudio.FAudioDeviceRole.FAudioDefaultGameDevice) == FAudio.FAudioDeviceRole.FAudioDefaultGameDevice)
                {
                    break;
                }
            }

            if (num == devices)
            {
                num = 0u;
                FAudio.FAudio_GetDeviceDetails(Handle, num, out DeviceDetails);
            }

            if (FAudio.FAudio_CreateMasteringVoice(Handle, out MasterVoice, 0u, 0u, 0u, num, IntPtr.Zero) != 0)
            {
                FAudio.FAudio_Release(ctx);
                Handle = IntPtr.Zero;
                return;
            }

            CurveDistanceScaler = 1f;
            DopplerScale = 1f;
            SpeedOfSound = 343.5f;
            Handle3D = new byte[20];
            FAudio.F3DAudioInitialize(DeviceDetails.OutputFormat.dwChannelMask, SpeedOfSound, Handle3D);
            Context = this;
        }

        ~FAudioContextDummy()
        {
            Release();
        }

        public void Release()
        {
            if (MasterVoice != IntPtr.Zero)
            {
                FAudio.FAudioVoice_DestroyVoice(MasterVoice);
            }

            if (Handle != IntPtr.Zero)
            {
                FAudio.FAudio_Release(Handle);
            }

            Context = null;
        }

        public static void Create()
        {
            IntPtr ppFAudio;
            try
            {
                FAudio.FAudioCreate(out ppFAudio, 0u, uint.MaxValue);
            }
            catch (Exception ex)
            {
                Projections.Log(Data.LogType.Error, $"Failed to Initialize FAudio device handle!\n\n{ex.Message}");
                return;
            }

            FAudio.FAudio_GetDeviceCount(ppFAudio, out var pCount);
            if (pCount == 0)
            {
                FAudio.FAudio_Release(ppFAudio);
                return;
            }

            FAudioContextDummy fAudioContext = new FAudioContextDummy(ppFAudio, pCount);
            if (fAudioContext.Handle == IntPtr.Zero)
            {
                fAudioContext.Release();
            }
            else
            {
                Context = fAudioContext;
            }
        }
    }

    public class AudioSource
    {
        private static object _creationLock = new object();

        public Action<long> OnBufferEnd;

        public SoundState State => _state;

        public long SamplePosition
        {
            get
            {
                if (_state == SoundState.Stopped)
                {
                    return _position + _length;
                }
                FAudio.FAudioSourceVoice_GetState(_handle, out var state, 0);
                return _position + (long)state.SamplesPlayed;
            }
        }
        public float Volume => _volume;

        public bool IsPlaying => _state == SoundState.Playing;

        private IntPtr _handle;
        private FAudio.FAudioWaveFormatEx _waveFormat;
        private FAudio.FAudioBuffer _buffer;
        private IntPtr _cb;
        private SoundState _state;
        private float _volume = 1.0f;
        private long _position;
        private long _length;

        internal static FAudioContextDummy GetContext()
        {
            if (FAudioContextDummy.Context != null)
            {
                return FAudioContextDummy.Context;
            }

            lock (_creationLock)
            {
                if (FAudioContextDummy.Context != null)
                {
                    return FAudioContextDummy.Context;
                }

                FAudioContextDummy.Create();
                if (FAudioContextDummy.Context == null)
                {
                    throw new NoAudioHardwareException();
                }
            }
            return FAudioContextDummy.Context;
        }

        ~AudioSource()
        {
            Release();
        }

        public void Init(int sampleRate, int channels)
        {
            if (sampleRate != _waveFormat.nSamplesPerSec || channels != _waveFormat.nChannels)
            {
                Release();
            }

            unsafe
            {
                if (_handle == IntPtr.Zero)
                {
                    GC.ReRegisterForFinalize(this);

                    _waveFormat.nChannels = (ushort)channels;
                    _waveFormat.nSamplesPerSec = (uint)sampleRate;
                    _waveFormat.wFormatTag = 1;
                    _waveFormat.nBlockAlign = (ushort)(16 * channels >> 3);
                    _waveFormat.nAvgBytesPerSec = (uint)sampleRate * _waveFormat.nBlockAlign;
                    _waveFormat.wBitsPerSample = 16;
                    _waveFormat.cbSize = 0;
                    _cb = AudioExtensions.Malloc(sizeof(FAudio.FAudioVoiceCallback));

                    var fAudioContext = GetContext();
                    FAudio.FAudio_CreateSourceVoice(fAudioContext.Handle, out _handle, ref _waveFormat, 8u, 2f, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }

        public void Play(AudioBuffer buffer, long position, float? volume = null)
        {
            _volume = volume ?? _volume;
            _position = position;
            unsafe
            {
                Init(buffer.SampleRate, buffer.Channels);
                var data = buffer.Span;
                if (_handle != IntPtr.Zero)
                {
                    if (_buffer.pAudioData == IntPtr.Zero || _buffer.AudioBytes < data.Length)
                    {
                        if (_buffer.pAudioData != IntPtr.Zero)
                        {
                            AudioExtensions.Free(_buffer.pAudioData);
                        }
                        _buffer.pAudioData = AudioExtensions.Malloc(data.Length);
                    }
                    _buffer.AudioBytes = (uint)data.Length;

                    Span<byte> tmp = new Span<byte>(_buffer.pAudioData.ToPointer(), data.Length);
                    data.CopyTo(tmp);
                    _buffer.PlayBegin = 0u;
                    _buffer.PlayLength = (uint)(data.Length / _waveFormat.nChannels / 2);
                    _buffer.LoopBegin = 0u;
                    _buffer.LoopLength = 0u;
                    _buffer.LoopCount = 0u;

                    FAudio.FAudioVoice_SetVolume(_handle, _volume, 0u);
                    FAudio.FAudioSourceVoice_SubmitSourceBuffer(_handle, ref _buffer, IntPtr.Zero);
                    FAudio.FAudioSourceVoice_Start(_handle, 0u, 0u);
                    _state = SoundState.Playing;
                    _length = _buffer.PlayLength;
                }
            }
        }

        public bool SetVolume(float volume)
        {
            if (volume == _volume) { return false; }
            _volume = volume;
            if (_handle != IntPtr.Zero)
            {
                FAudio.FAudioVoice_SetVolume(_handle, _volume, 0u);
            }
            return true;
        }

        public void Pause()
        {
            if (_handle != IntPtr.Zero && _state == SoundState.Playing)
            {
                FAudio.FAudioSourceVoice_Stop(_handle, 0u, 0u);
                _state = SoundState.Paused;
            }
        }

        public void Resume()
        {
            if (_handle != IntPtr.Zero)
            {
                if (_state == SoundState.Paused)
                {
                    FAudio.FAudioSourceVoice_Start(_handle, 0u, 0u);
                    _state = SoundState.Playing;
                }
            }
        }

        public void Stop()
        {
            if (_state == SoundState.Stopped) { return; }
            if (_handle != IntPtr.Zero)
            {
                FAudio.FAudioSourceVoice_Stop(_handle, 0u, 0u);
                FAudio.FAudioSourceVoice_FlushSourceBuffers(_handle);
            }
            _state = SoundState.Stopped;
        }

        public void Update()
        {
            if (_state == SoundState.Playing)
            {
                FAudio.FAudioSourceVoice_GetState(_handle, out var pVoiceState, 256u);
                if (pVoiceState.BuffersQueued == 0)
                {
                    Stop();
                    OnBufferEnd?.Invoke(_position + _length);
                }
            }
        }

        public void Release()
        {
            if (_cb != IntPtr.Zero)
            {
                AudioExtensions.Free(_cb);
                _cb = IntPtr.Zero;
            }

            if (_handle != IntPtr.Zero)
            {
                FAudio.FAudioSourceVoice_Stop(_handle, 0u, 0u);
                FAudio.FAudioSourceVoice_FlushSourceBuffers(_handle);
                FAudio.FAudioVoice_DestroyVoice(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }

    public static class AudioExtensions
    {
        private static MethodInfo _malloc;
        private static MethodInfo _free;
        private static object[] _tempBuf0 = new object[1];

        static AudioExtensions()
        {
            _malloc = typeof(SDL).GetMethod("SDL_malloc", BindingFlags.Static | BindingFlags.NonPublic, new Type[] { typeof(IntPtr) });
            _free = typeof(SDL).GetMethod("SDL_free", BindingFlags.Static | BindingFlags.NonPublic, new Type[] { typeof(IntPtr) });
        }

        public static void Free(IntPtr handle)
        {
            lock (_tempBuf0)
            {
                _tempBuf0[0] = handle;
                _free.Invoke(null, _tempBuf0);
            }
        }

        public static IntPtr Malloc(long size)
        {
            lock (_tempBuf0)
            {
                _tempBuf0[0] = new IntPtr(size);
                return (IntPtr)_malloc.Invoke(null, _tempBuf0);
            }
        }

    }
}
