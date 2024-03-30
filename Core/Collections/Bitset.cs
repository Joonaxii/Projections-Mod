using Projections.Core.Maths;
using Projections.Core.Utilities;
using System;

namespace Projections.Core.Collections
{
    public class Bitset
    {
        public int Count => _count;

        private int _count;
        private byte[] _bits;
        public Bitset() : this(8) { }

        public Bitset(int capacity)
        {
            _count = 0;
            _bits = new byte[capacity];
        }

        public Bitset(bool value, int count)
        {
            _count = count;
            _bits = new byte[PMath.NextDivBy8(_count)];
            SetAll(value);
        }

        public bool this[int i]
        {
            get
            {
                ExtractInd(i, out int bInd, out int lInd);
                return bInd < _bits.Length && (_bits[bInd] & 1 << lInd) != 0;
            }

            set
            {
                ExtractInd(i, out int bInd, out int lInd);
                if (bInd >= _bits.Length) { return; }
                if (value)
                {
                    _bits[bInd] |= (byte)(1 << lInd);
                }
                else
                {
                    _bits[bInd] &= (byte)~(1 << lInd);
                }
            }
        }

        public void SetAll(bool value)
        {
            int fullBytes = PMath.NextDivBy8(_count);
            if (value)
            {
                int usedBytes = (_count & 0x7) == 0 ? fullBytes : fullBytes - 1;
                _bits.AsSpan().Slice(0, usedBytes).Memset<byte>(0xFF);

                int left = _count - (usedBytes << 3) - 1;
                if (left >= 0)
                {
                    int mask = (1 << left) - 1;
                    _bits[usedBytes] |= (byte)mask;
                }
            }
            else
            {
                _bits.AsSpan().Slice(0, fullBytes).Memset<byte>(0);
            }
        }

        public void Resize(int count)
        {
            if (count <= _count)
            {
                _count = count;
                return;
            }
            int needed = PMath.NextDivBy8(count);
            if (needed > _bits.Length)
            {
                Array.Resize(ref _bits, needed);
            }
        }

        private static void ExtractInd(int input, out int byteI, out int localI)
        {
            byteI = input >> 3;
            localI = input - (byteI << 3);
        }
    }
}
