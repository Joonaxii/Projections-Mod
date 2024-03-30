using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Projections.Core.Maths
{
    public static class PMath
    {
        public const float DEG_2_RAD = 3.14159265359f / 180.0f;
        public const float RAD_2_DEG = 180 / 3.14159265359f;

        private static byte[] _UI8_MULT_TABLE;
        private static byte[] _UI8_DIV_TABLE;
        private static float[] _UI8_NORMALIZED;

        static PMath()
        {
            const float NORMALIZE_UI8 = 1.0f / 255.0f;
            _UI8_MULT_TABLE = new byte[256 * 256];
            _UI8_DIV_TABLE = new byte[256 * 256];
            _UI8_NORMALIZED = new float[256];
            for (uint i = 0; i < 256; i++)
            {
                _UI8_NORMALIZED[i] = MathF.Min(i * NORMALIZE_UI8, 1.0f);
                for (uint j = 0; j < 256; j++)
                {
                    uint ind = i | j << 8;
                    _UI8_DIV_TABLE[ind] = (byte)(j == 0 ? 0 : i / (float)j * 255.0f);
                    _UI8_MULT_TABLE[ind] = MultUI8(i, j);
                }
            }
        }

        public static float InverseLerp(float a, float b, float v)
        {
            if (v <= a)
            {
                return 0.0f;
            }
            else if (v >= b)
            {
                return 1.0f;
            }
            return (v - a) / (b - a);
        }

        public static byte MultUI8(uint a, uint b)
        {
            return (byte)(a * b * 0x10101U + 0x800000U >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte MultUI8LUT(int a, int b)
        {
            return _UI8_MULT_TABLE[a | b << 8];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte DivUI8LUT(int a, int b)
        {
            return _UI8_DIV_TABLE[a | b << 8];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizeUI8(byte value)
        {
            return _UI8_NORMALIZED[value];
        }

        public static float Lerp(float a, float b, float t) => a * (1.0f - t) + b * t;
        public static int Lerp(int a, int b, float t) => (int)(a * (1.0f - t) + b * t);

        public static int NextDivBy16(int value)
        {
            return (value & 0xF) == 0 ? value : value + 0xF & ~0xF;
        }

        public static int NextDivBy8(int value)
        {
            return (value & 0x7) == 0 ? value : value + 0x7 & ~0x7;
        }

        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static float Angle(Vector2 a, Vector2 b)
        {
            float den = MathF.Sqrt((a.X * a.X + a.Y * a.Y) * (b.X * b.X + b.Y * b.Y));
            if (den < 0.000001f) { return 0; }

            Vector2.Dot(ref a, ref b, out float dotP);
            dotP /= den;
            return MathF.Acos(dotP) * RAD_2_DEG;
        }
        public static float SignedAngle(Vector2 a, Vector2 b)
        {
            return Angle(a, b) * (Cross(a, b) < 0 ? -1 : 1);
        }
    }
}