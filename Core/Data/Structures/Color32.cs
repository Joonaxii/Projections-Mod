using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using Projections.Core.Maths;

namespace Projections.Core.Data.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Color32
    {
        public static Color32 White { get; } = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        [FieldOffset(0)] public byte r;
        [FieldOffset(1)] public byte g;
        [FieldOffset(2)] public byte b;
        [FieldOffset(3)] public byte a;
        [FieldOffset(0)] private uint _value;

        public Color32(byte r, byte g, byte b, byte a) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public Color32(uint value) : this()
        {
            _value = value;
        }

        public static implicit operator Color(Color32 lhs)
        {
            unsafe
            {
                return *(Color*)&lhs;
            }
        }

        public static implicit operator Color32(Color lhs)
        {
            unsafe
            {
                return *(Color32*)&lhs;
            }
        }

        public static Color32 Multiply(Color32 a, Color32 b)
        {
            return new Color32(
                PMath.MultUI8LUT(a.r, b.r),
                PMath.MultUI8LUT(a.g, b.g),
                PMath.MultUI8LUT(a.b, b.b),
                PMath.MultUI8LUT(a.a, b.a));
        }

        public static Color Multiply(Color32 a, Color b)
        {
            return new Color(
                PMath.MultUI8LUT(a.r, b.R),
                PMath.MultUI8LUT(a.g, b.G),
                PMath.MultUI8LUT(a.b, b.B),
                PMath.MultUI8LUT(a.a, b.A));
        }

        public static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            unsafe
            {
                uint val = Lerp(*(uint*)&a, *(uint*)&b, t);
                return *(Color32*)&val;
            }
        }

        public static uint Lerp(uint a, uint b, float t)
        {
            const uint RB_MASK = 0x00ff00ff;
            const uint GA_MASK = 0xff00ff00;

            const uint ONE_Q8 = 1 << 8;
            uint tQ8 = (uint)(ONE_Q8 * t);
            unsafe
            {
                uint rbA = a & RB_MASK;
                uint rbB = b & RB_MASK;

                uint gaA = (a & GA_MASK) >> 8;
                uint gaB = (b & GA_MASK) >> 8;

                uint rb = rbA * (ONE_Q8 - tQ8) + rbB * tQ8 >> 8 & RB_MASK;
                uint ga = gaA * (ONE_Q8 - tQ8) + gaB * tQ8 & GA_MASK;
                return rb | ga;
            }
        }

        public static Color32 Multiply(Color32 lhs, byte alpha)
        {
            lhs.r = MathLow.MultUI8_LUT(lhs.r, alpha);
            lhs.g = MathLow.MultUI8_LUT(lhs.g, alpha);
            lhs.b = MathLow.MultUI8_LUT(lhs.b, alpha);
            lhs.a = MathLow.MultUI8_LUT(lhs.a, alpha);
            return lhs;
        }
    }
}