using System;
using System.Runtime.InteropServices;

namespace Projections.Core.Collections
{
    public unsafe class UnmanagedBuffer<T> : IDisposable where T : unmanaged
    {
        public int Capacity => _capacity;
        public IntPtr Data => _ptr;
        public T* Raw => _raw;

        private int _capacity;
        private IntPtr _ptr;
        private T* _raw;

        public UnmanagedBuffer() { }

        public UnmanagedBuffer(int capacity)
        {
            Resize(capacity);
        }

        ~UnmanagedBuffer()
        {
            Release();
        }

        public ref T this[int i]
        {
            get => ref _raw[i];
        }

        public IntPtr GetAt(int i) => IntPtr.Add(_ptr, i * sizeof(T));
        public Span<T> AsSpan() => new Span<T>(_raw, _capacity);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Release();
        }

        public void Resize(int size, bool shrink = false)
        {
            if (_raw == null)
            {
                _capacity = size;
                _ptr = Marshal.AllocHGlobal(size);
                _raw = (T*)_ptr.ToPointer();
                return;
            }

            if (shrink & size != _capacity || size > _capacity)
            {
                _ptr = Marshal.ReAllocHGlobal(_ptr, new IntPtr(size));
                _raw = (T*)_ptr.ToPointer();
                _capacity = size;
            }
        }

        public void Release()
        {
            if (_raw != null)
            {
                Marshal.FreeHGlobal(_ptr);
                _ptr = IntPtr.Zero;
                _raw = null;
            }
            _capacity = 0;
        }
    }
}