using Microsoft.Xna.Framework;
using Projections.Core.Data.Structures;
using Projections.Core.Maths;
using Projections.Core.Utilities;
using System;

namespace Projections.Core.Data
{
    public static class Lightmap
    {
        private static void GetAverage(int x, int y, int width, int height, ReadOnlySpan<Color32> pixels, Span<int> output)
        {
            output.Memset(0);

            int scan = y * width;
            int reso = width * height;
            for (int yP = 0, yV = scan; yP < 16; yP++, yV += width)
            {
                if(yV < 0 || yV >= reso) 
                {
                    continue; 
                }

                for (int xP = 0, xV = x; xP < 16; xP++, xV++)
                {
                    if (xV < 0 || xV >= width)
                    {
                        continue;
                    }

                    ref readonly var color = ref pixels[xV + yV];
                    output[0] += color.r;
                    output[1] += color.g;
                    output[2] += color.b;
                    output[3] += color.a;
                }
            }
        }

        public static void DrawLights(Span<Color3F> lights, int pWidth, int pHeight, ReadOnlySpan<Color32> pixels, ref RectF area, Vector2 origin, float rotation)
        {
            int nW = PMath.NextDivBy16(pWidth);
            int nH = PMath.NextDivBy16(pHeight);

            int width = nW >> 4;
            int height = nH >> 4;

            int xOff = nW >> 1;
            int yOff = nH >> 1;

            Span<int> temp = stackalloc int[4];
            for (int y = -yOff, yT = 0; y < nH; y++, yT += width)
            {
                for (int x = -xOff, xT = yT; y < nW; y++, xT++)
                {
                    GetAverage(x, y, pWidth, pHeight, pixels, temp);
                    temp[0] /= 256;
                    temp[1] /= 256;
                    temp[2] /= 256;
                    temp[3] /= 256;
                    lights[xT] = new Color3F(Color24.Premult(new Color32((byte)temp[0], (byte)temp[1], (byte)temp[2], (byte)temp[3])));
                }
            }

            var center = area.Center;

            center.X /= 16.0f;
            center.Y /= 16.0f;

            RectF dst = new RectF(center.X - width * 0.5f, center.Y - height * 0.5f, width * 0.5f, height * 0.5f);

            float oX = dst.x * origin.X;
            float oY = dst.y * origin.Y;

            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Point loc = default;
            for (int y = 0, yT = 0; y < height; y++, yT += width)
            {
                float yP = (dst.y + y) - oY;
                for (int x = 0; x < width; x++)
                {
                    float xP = (dst.x + x) - oX;
                    loc.X = (int)((cos * xP) + (sin * yP) + oX);
                    loc.Y = (int)((sin * xP) + (cos * yP) + oY);

                  //  lights[yT + x];
                }
            }
        }
    }
}
