using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Projections.Core.Collections
{
    public class OrderedList<T> : IList<T>
    {
        public delegate int CompareT(in T lhs, in T rhs);
        public delegate bool PredicateT(in T lhs);

        public int Count => _count;
        public bool IsReadOnly => false;

        /// <summary>
        /// NOTE: Should only be used for reading, this returns a ref to avoid needless copying of larger structs etc. Downside being that you can write into it as well which is not adviced unless you know what you're doing!
        /// </summary>
        public ref T this[int i]
        {
            get => ref _buffer[i];
        }
        T IList<T>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        protected T[] _buffer;
        protected int _count;
        protected bool _allowDupes;
        protected CompareT _comparer;

        public OrderedList() : this(8, true, null) { }
        public OrderedList(CompareT comparer) : this(8, true, comparer) { }
        public OrderedList(int capacity) : this(capacity, true, null) { }
        public OrderedList(int capacity, CompareT comparer) : this(capacity, true, comparer) { }
        public OrderedList(int capacity, bool allowDuplicates) : this(capacity, allowDuplicates, null) { }
        public OrderedList(int capacity, bool allowDuplicates, CompareT comparer)
        {
            _comparer = comparer ?? DefaultCompare;
            _allowDupes = allowDuplicates;
            Reserve(capacity);
        }

        private static int DefaultCompare(in T lhs, in T rhs)
        {
            return Comparer<T>.Default.Compare(lhs, rhs);
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _count);
            _count = 0;
        }

        public bool Add(T item)
        {
            int ind = Nearest(item);
            if (!_allowDupes && ind < _count && _comparer.Invoke(in _buffer[ind], in item) == 0)
            {
                return false;
            }

            CheckForExpansion(_count + 1);
            if (ind < _count)
            {
                Array.Copy(_buffer, ind, _buffer, ind + 1, _count - ind);
            }
            _buffer[ind] = item;
            _count++;
            return true;
        }

        public int IndexOf(T value)
        {
            int l = 0;
            int r = _count - 1;

            while (l <= r)
            {
                int m = l + (r - l >> 1);
                int res = _comparer.Invoke(in _buffer[m], in value);

                if (res == 0)
                {
                    return m;
                }
                else if (res < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }
            return -1;
        }
        public int Nearest(T value)
        {
            int l = 0;
            int r = _count - 1;
            int m;
            if (_count <= 0)
            {
                return 0;
            }

            while (l <= r)
            {
                m = l + (r - l >> 1);
                int res = _comparer.Invoke(in _buffer[m], in value);
                if (res == 0)
                {
                    return m;
                }
                else if (res < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }
            return l;
        }

        public void Update(int index)
        {
            if (_count < 2 || index < 0 || index >= _count) { return; }

            int startIndex = -1;
            int idx;
            while (true)
            {
                if (startIndex == index) { break; }
                startIndex = startIndex < 0 ? index : startIndex;

                int ret = 0;

                ref var cur = ref _buffer[index];

                idx = index - 1;
                if (startIndex != idx && idx > -1)
                {
                    ref var rhs = ref _buffer[idx];
                    ret = _comparer.Invoke(in cur, in rhs);
                    if (ret < 0)
                    {
                        Swap(ref cur, ref rhs);
                        index--;
                        continue;
                    }
                }

                idx = index + 1;
                if (startIndex != idx && idx < _count)
                {
                    ref var rhs = ref _buffer[idx];
                    ret = _comparer.Invoke(in cur, in rhs);
                    if (ret > 0)
                    {
                        Swap(ref cur, ref rhs);
                        index++;
                        continue;
                    }
                }
                break;
            }
        }

        public bool Contains(T item) => IndexOf(item) > -1;
        public void CopyTo(T[] array, int arrayIndex)
        {
            int len = Math.Min(array.Length - arrayIndex, _count);
            Array.Copy(_buffer, 0, array, arrayIndex, len);
        }

        public bool Remove(T item)
        {
            return RemoveAt(IndexOf(item));
        }
        public bool RemoveAt(int ind)
        {
            if (ind < 0 || ind >= _count) { return false; }

            _count--;
            if (ind < _count)
            {
                Array.Copy(_buffer, ind + 1, _buffer, ind, _count - ind);
            }
            return true;
        }

        public bool RemoveIf(PredicateT predicate, bool stopAtFirst = false)
        {
            bool removed = false;
            for (int i = Count - 1; i >= 0; i--)
            {
                if(predicate.Invoke(in _buffer[i]))
                {
                    removed |= RemoveAt(i);
                    if (stopAtFirst)
                    {
                        break;
                    }
                }
            }
            return removed;
        }

        private void CheckForExpansion(int count)
        {
            int cap = _buffer.Length;
            while (count > cap)
            {
                cap += Math.Max(cap >> 1, 1);
            }
            Reserve(cap);
        }
        private void Reserve(int count)
        {
            if (_buffer == null)
            {
                _buffer = new T[count];
                return;
            }

            if (count > _buffer.Length)
            {
                Array.Resize(ref _buffer, count);
            }
        }

        void ICollection<T>.Add(T item) { }
        void IList<T>.Insert(int index, T item) { }
        void IList<T>.RemoveAt(int index) { }
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

        private void Swap(ref T lhs, ref T rhs)
        {
            (rhs, lhs) = (lhs, rhs);
        }
    }

    public class OrderedList<T, U> : OrderedList<T>
    {
        public delegate int CompareU(in T lhs, in U rhs);

        private CompareU _compToU;

        public OrderedList(CompareT compT = null, CompareU compU = null) : this(8, true, compT, compU) { }
        public OrderedList(int capacity, CompareT compT = null, CompareU compU = null) : this(capacity, true, compT, compU) { }
        public OrderedList(int capacity, bool allowDuplicates, CompareT compT = null, CompareU compU = null) : base(capacity, allowDuplicates, compT)
        {
            _compToU = compU ?? CompareToU;
        }

        private static int CompareToU(in T lhs, in U rhs)
        {
            if (lhs is IComparable<U> lhsComp)
            {
                return lhsComp.CompareTo(rhs);
            }
            return Comparer.Default.Compare(lhs, rhs);
        }

        public int IndexOf(U value)
        {
            int l = 0;
            int r = _count - 1;

            while (l <= r)
            {
                int m = l + (r - l >> 1);
                int res = _compToU.Invoke(in _buffer[m], in value);

                if (res == 0)
                {
                    return m;
                }
                else if (res < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }
            return -1;
        }
        public bool Contains(U item) => IndexOf(item) > -1;

        public T SelectBy(U item)
        {
            int ind = IndexOf(item);
            return ind > -1 ? _buffer[ind] : default;
        }
        public bool RemoveBy(U value)
        {
            return RemoveAt(IndexOf(value));
        }
    }
}