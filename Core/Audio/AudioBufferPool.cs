using Projections.Core.Collections;
using System;
using System.Collections.Generic;

namespace Projections.Core.Audio
{
    public class AudioBufferPool : IDisposable
    {
        private Queue<AudioBuffer> _bufferPool = new Queue<AudioBuffer>();
        private List<AudioBuffer> _allBuffers = new List<AudioBuffer>();
        private int _initCap;

        public AudioBufferPool(int initBuffers)
        {
            _initCap = initBuffers;
            _bufferPool = new Queue<AudioBuffer>(initBuffers * 2);
            _allBuffers = new List<AudioBuffer>(initBuffers * 2);
        }

        ~AudioBufferPool()
        {
            Clear();
        }

        public void Init(bool alloc)
        {
            if (_allBuffers.Count > 0) { return; }
            for (int i = 0; i < _initCap; i++)
            {
                var buff = GetNew();
                _bufferPool.Enqueue(buff);
                _allBuffers.Add(buff);

                if (alloc) { buff.Use(); }
            }
        }

        public AudioBuffer Return(AudioBuffer buffer, bool reset = false)
        {
            if (buffer != null)
            {
                buffer.MarkUnused();
                _bufferPool.Enqueue(buffer);
                if (reset) { buffer.MakeStale(); }
            }
            //Returns null just so this can be called to "assign" null to the returned buffer
            return null;
        }

        public AudioBuffer GetBuffer()
        {
            AudioBuffer buffer = _bufferPool.Count < 1 ? GetNew() : _bufferPool.Dequeue();
            buffer.Use();
            return buffer;
        }

        private AudioBuffer GetNew()
        {
            AudioBuffer newBuf = new AudioBuffer();
            _allBuffers.Add(newBuf);
            return newBuf;
        }

        public void Tick()
        {
            for (int i = 0; i < _allBuffers.Count; i++)
            {
                _allBuffers[i].Tick();
            }
        }

        public void MarkBuffersStale()
        {
            for (int i = 0; i < _allBuffers.Count; i++)
            {
                _allBuffers[i].MakeStale();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _allBuffers.Count; i++)
            {
                _allBuffers[i].Dispose();
            }
            _bufferPool.Clear();
            _allBuffers.Clear();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Clear();
        }
    }

    public class AudioBuffer : IDisposable
    {
        public const int STALE_DELAY = 60 * 600;
        public const int MAX_CHANNELS = 2;
        public const int BYTES_PER_SAMPLE = 2;
        public const int MAX_SAMPLE_RATE = 48000;

        public const int BUFFER_SIZE = MAX_SAMPLE_RATE * MAX_CHANNELS * BYTES_PER_SAMPLE;

        public bool IsStale => _fullBuffer.Capacity <= 0;

        public Span<byte> Span => _fullBuffer.AsSpan().Slice(0, _dataSize);
        public int SampleRate => _sampleRate;
        public int Channels => _channels;

        private int _sampleRate;
        private int _channels;
        private int _ticksUntilStale = STALE_DELAY;
        private int _dataSize = 0;
        private bool _inUse;
        private UnmanagedBuffer<byte> _fullBuffer = new UnmanagedBuffer<byte>();

        ~AudioBuffer()
        {
            _fullBuffer.Release();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _inUse = false;
            _fullBuffer.Dispose();
        }

        public void SetSize(int newLen)
        {
            _dataSize = newLen;
        }

        public Span<byte> Setup(int sampleRate, bool isStereo)
        {
            _channels = isStereo ? 2 : 1;
            _sampleRate = sampleRate;
            Use();

            // The AND 0x3 is there to be align to 4 bytes (downwards), partial or half samples aren't supported!
            _dataSize = Math.Min(sampleRate * _channels * 2, BUFFER_SIZE) & 0x3;
            return Span;
        }

        public void Use()
        {
            if (IsStale)
            {
                _fullBuffer.Resize(BUFFER_SIZE);
            }
            _inUse = true;
            _ticksUntilStale = STALE_DELAY;
        }

        public void MarkUnused()
        {
            _inUse = false;
        }

        public void MakeStale()
        {
            _inUse = false;
            _ticksUntilStale = 0;
            _fullBuffer.Release();
        }

        public void Tick()
        {
            if (IsStale || _inUse) { return; }

            _ticksUntilStale--;
            if (_ticksUntilStale <= 0)
            {
                MakeStale();
            }
        }
    }
}
