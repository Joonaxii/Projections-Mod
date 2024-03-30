using Projections.Core.Data.Structures;
using System;
using System.Runtime.CompilerServices;

namespace Projections.Core.Maths
{
    public unsafe static class UnsafeMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U Reinterpret<T, U>(this T input) where T : unmanaged where U : unmanaged
        {
            return *(U*)&input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color32 AlphaBlend_Bit(Color32 lhs, Color32 rhs)
        {
            uint colA = *(uint*)&lhs;
            uint colB = *(uint*)&rhs;

            uint a2 = (colA & 0xFF000000) >> 24;
            uint alpha = a2;

            if (alpha == 0) return lhs;
            if (alpha == 255) return rhs;

            uint a1 = (colA & 0xFF000000) >> 24;
            uint nalpha = 0x100 - alpha;
            uint rb1 = nalpha * (colB & 0xFF00FF) >> 8;
            uint rb2 = alpha * (colB & 0xFF00FF) >> 8;
            uint g1 = nalpha * (colA & 0x00FF00) >> 8;
            uint g2 = alpha * (colB & 0x00FF00) >> 8;
            uint anew = a1 + a2;
            if (anew > 255) { anew = 255; }
            uint ret = (rb1 + rb2 & 0xFF00FF) + (g1 + g2 & 0x00FF00) + (anew << 24);
            return *(Color32*)&ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AlphaBlend(ref Color32 src, Color32 dst)
        {
            int srcDiff = 0xFF - src.a;
            src.r = MathLow.SumUI8(src.r, MathLow.MultUI8_LUT(srcDiff, dst.r));
            src.g = MathLow.SumUI8(src.g, MathLow.MultUI8_LUT(srcDiff, dst.g));
            src.b = MathLow.SumUI8(src.b, MathLow.MultUI8_LUT(srcDiff, dst.b));
            src.a = MathLow.SumUI8(src.a, dst.a);
        }

        public static void AlphaBlend(Span<Color32> lhs, Span<Color32> rhs)
        {
            fixed (Color32* lPtr = lhs)
            fixed (Color32* rPtr = rhs)
            {
                int len = lhs.Length;

                var lhsP = lPtr;
                var rhsP = rPtr;
                while (len-- > 0)
                {
                    AlphaBlend(ref *lhsP++, *rhsP++);
                }
            }
        }

        public static void ApplyMask(Span<Color32> lhs, Span<byte> alphaMask)
        {
            fixed (Color32* lPtr = lhs)
            fixed (byte* aPtr = alphaMask)
            {
                int len = lhs.Length;

                var lhsP = lPtr;
                var alpP = aPtr;
                while (len-- > 0)
                {
                    MathLow.Mult(ref *lhsP++, *alpP++);
                }
            }
        }

        public static void AlphaBlend(Span<Color32> lhs, Span<Color32> rhs, Span<byte> alphaMask)
        {
            fixed (Color32* lPtr = lhs)
            fixed (Color32* rPtr = rhs)
            fixed (byte* aPtr = alphaMask)
            {
                int len = lhs.Length;

                var lhsP = lPtr;
                var rhsP = rPtr;
                var alpP = aPtr;
                while (len-- > 0)
                {
                    MathLow.Mult(ref *rhsP, *alpP++);
                    AlphaBlend(ref *lhsP++, *rhsP++);
                }
            }
        }
    }
}
