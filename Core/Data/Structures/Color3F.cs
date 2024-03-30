using Projections.Core.Data.Structures;

namespace Projections.Core.Data
{
    public struct Color3F
    {
        public float r, g, b;

        public Color3F(Color24 color)
        {
            color.ToFloat(out r, out g, out b);
        }
    }
}