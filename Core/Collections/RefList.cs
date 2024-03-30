using System;
using System.Collections;
using System.Collections.Generic;

namespace Projections.Core.Collections
{
    public class RefList<T> : IList<T>
    {
        public int Count => _count;
        public bool IsReadOnly => false;

        public Span<T> Span => _buffer.AsSpan().Slice(0, _count);

        private int _count = 0;
        private T[] _buffer = new T[0];

        public ref T this[int index] => ref _buffer[index];
        T IList<T>.this[int index]
        {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public RefList() : this(8) { }
        public RefList(int capacity)
        {
            Array.Resize(ref _buffer, Math.Max(capacity, 0));
        }

        public void Add(T item)
        {
            EnsureSize(_count + 1);
            _buffer[_count++] = item;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_buffer, 0, array, arrayIndex, Math.Min(_count, array.Length - arrayIndex));
        }

        public void Resize(int size)
        {
            if (size < 0) { return; }
            _count = size;
            if (size == _buffer.Length) { return; }
            Array.Resize(ref _buffer, size);
        }

        public void Reserve(int size)
        {
            if (size <= _buffer.Length) { return; }
            Array.Resize(ref _buffer, size);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _buffer[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void EnsureSize(int size)
        {
            int cap = _buffer.Length;
            while (cap <= size)
            {
                cap += Math.Max(_buffer.Length >> 1, 1);
            }
            if (cap != _buffer.Length)
            {
                Array.Resize(ref _buffer, cap);
            }
        }

        int IList<T>.IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
