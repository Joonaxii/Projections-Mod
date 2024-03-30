using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Projections.Core.Data.Structures;

namespace Projections.Core.Maths
{
    public static class MathLow
    {
        private static readonly byte[] LOG2_TABLE_64 = new byte[64]
        {
             63,  0, 58,  1, 59, 47, 53,  2,
             60, 39, 48, 27, 54, 33, 42,  3,
             61, 51, 37, 40, 49, 18, 28, 20,
             55, 30, 34, 11, 43, 14, 22,  4,
             62, 57, 46, 52, 38, 26, 32, 41,
             50, 36, 17, 19, 29, 10, 13, 21,
             56, 45, 25, 31, 35, 16,  9, 12,
             44, 24, 15,  8, 23,  7,  6,  5
        };

        private static readonly byte[] BIT7_LUT;
        private static readonly byte[] MULTUI8_LUT;
        private static readonly byte[] SUMTUI8_LUT;

        static MathLow()
        {
            const int BIT7_LEN = 1 << 7;
            const int BIT7_MASK = (1 << 7) - 1;
            BIT7_LUT = new byte[BIT7_LEN];
            for (int i = 0; i < BIT7_LEN; i++)
            {
                BIT7_LUT[i] = (byte)(i / (float)BIT7_MASK * 255.0f);
            }

            MULTUI8_LUT = new byte[ushort.MaxValue + 1];
            SUMTUI8_LUT = new byte[ushort.MaxValue + 1];
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    MULTUI8_LUT[i | j << 8] = MultUI8(i, j);
                    SUMTUI8_LUT[i | j << 8] = (byte)Math.Min(i + j, 0xFF);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RemapBit7(byte value) => BIT7_LUT[value & 0x7F];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SumUI8(int a, int b)
            => SUMTUI8_LUT[a & 0xFF | (b & 0xFF) << 8];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte MultUI8(int a, int b) =>
            (byte)(a * b * 0x10101U + 0x800000U >> 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte MultUI8_LUT(int a, int b)
            => MULTUI8_LUT[a & 0xFF | (b & 0xFF) << 8];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(ulong value)
        {
            if (value == 0) { return 0; }
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return LOG2_TABLE_64[(value - (value >> 1)) * 0x07EDD5E59A4E28C2 >> 58];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindFirstLSB(ulong value)
        {
            return Log2(value ^ value & value - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextDivBy16(int input)
        {
            return (input & 0xF) != 0 ? input + 0xF & ~0xF : input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Multiply(Color lhs, Color rhs)
        {
            lhs.R = MultUI8_LUT(lhs.R, rhs.R);
            lhs.G = MultUI8_LUT(lhs.G, rhs.G);
            lhs.B = MultUI8_LUT(lhs.B, rhs.B);
            lhs.A = MultUI8_LUT(lhs.A, rhs.A);
            return lhs;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Premultiply(ref Color color)
        {
            color.R = MultUI8_LUT(color.R, color.A);
            color.G = MultUI8_LUT(color.G, color.A);
            color.B = MultUI8_LUT(color.B, color.A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Premultiply(ref Color32 color)
        {
            color.r = MultUI8_LUT(color.r, color.a);
            color.g = MultUI8_LUT(color.g, color.a);
            color.b = MultUI8_LUT(color.b, color.a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mult(ref Color32 color, byte alpha)
        {
            color.r = MultUI8_LUT(color.r, alpha);
            color.g = MultUI8_LUT(color.g, alpha);
            color.b = MultUI8_LUT(color.b, alpha);
            color.a = MultUI8_LUT(color.a, alpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mult(ref Color color, byte alpha)
        {
            color.R = MultUI8_LUT(color.R, alpha);
            color.G = MultUI8_LUT(color.G, alpha);
            color.B = MultUI8_LUT(color.B, alpha);
            color.A = MultUI8_LUT(color.A, alpha);
        }

        public static void GenerateAlphaMap(Span<byte> alpha, Span<Color32> pixels, int resolution, int layerI)
        {
            var layer = alpha.Slice(layerI * resolution, resolution);
            for (int k = 0; k < resolution; k++)
            {
                layer[k] = pixels[k].a;
            }

            for (int k = 0, l = 0; k < layerI; k++)
            {
                for (int i = 0; i < resolution; i++)
                {
                    ref byte val = ref alpha[l++];
                    val -= Math.Min(val, layer[i]);
                }
            }
        }
    }
}