using System;
using System.Runtime.InteropServices;

namespace Projections.Core.Utilities
{
    public static class CRC32
    {
        public const uint INIT_VAL = 0xFFFFFFFFU;

        private const uint POLYNOMIAL = 0xEDB88320U;
        private static uint[] _table;

        static CRC32()
        {
            _table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                ref uint c = ref _table[i];
                c = i;
                for (uint j = 0; j < 8; j++)
                {
                    if ((c & 0x1) != 0)
                    {
                        c = POLYNOMIAL ^ c >> 1;
                    }
                    else
                    {
                        c >>= 1;
                    }
                }
            }
        }

        public static uint Update(uint crc, ReadOnlySpan<byte> data)
        {
            if (data.Length < 1) { return crc; }

            for (int i = 0; i < data.Length; i++)
            {
                crc = _table[(crc ^ data[i]) & 0xFF] ^ crc >> 8;
            }
            return crc;
        }

        public static uint Update<T>(uint crc, T data) where T : struct =>
            Update(crc, ref data);

        public static uint Update<T>(uint crc, ref T data) where T : struct =>
            Update(crc, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)));

        public static uint Update<T>(uint crc, ReadOnlySpan<T> data) where T : struct =>
           Update(crc, MemoryMarshal.AsBytes(data));


        public static uint Calculate<T>(T data) where T : struct =>
          Calculate(ref data);

        public static uint Calculate<T>(ref T data) where T : struct =>
            Update(INIT_VAL, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1))) ^ INIT_VAL;

        public static uint Calculate<T>(ReadOnlySpan<T> data) where T : struct =>
           Update(INIT_VAL, MemoryMarshal.AsBytes(data)) ^ INIT_VAL;
    }
}