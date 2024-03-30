using Projections.Core.Maths;

namespace Projections.Core.Data.Structures
{
    public struct Color24
    {
        public byte r;
        public byte g;
        public byte b;

        public Color24(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public void ToFloat(out float r, out float g, out float b)
        {
            r = PMath.NormalizeUI8(this.r);
            g = PMath.NormalizeUI8(this.g);
            b = PMath.NormalizeUI8(this.b);
        }

        public static Color24 Premult(Color32 color)
        {
            return new Color24(
                PMath.MultUI8(color.r, color.a),
                PMath.MultUI8(color.g, color.a),
                PMath.MultUI8(color.b, color.a));
        }
    }
}
