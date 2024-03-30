using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Utilities;
using System;
using System.Reflection;

namespace Projections.Core.Textures
{
    public static class TextureExtensions
    {
        private static readonly ReflectableProperty<Texture2D, int> TexWidth;
        private static readonly ReflectableProperty<Texture2D, int> TexHeight;

        private static readonly ReflectableField<GraphicsDevice, IntPtr> GLDevice;
        private static readonly ReflectableField<Texture, IntPtr> TexturePtr;

        static TextureExtensions()
        {
            TexWidth = new ReflectableProperty<Texture2D, int>("Width", BindingFlags.Instance);
            TexHeight = new ReflectableProperty<Texture2D, int>("Height", BindingFlags.Instance);
            GLDevice = new ReflectableField<GraphicsDevice, IntPtr>("GLDevice", BindingFlags.Instance | BindingFlags.NonPublic);
            TexturePtr = new ReflectableField<Texture, IntPtr>("texture", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static void SetTextureResolution(Texture2D texture, int width, int height)
        {
            TexWidth.Set(texture, width);
            TexHeight.Set(texture, height);
        }

        public static void ResizeTexture(GraphicsDevice device, ref Texture2D texture, int width, int height)
        {
            if (texture == null)
            {
                texture = new Texture2D(device, width, height, false, SurfaceFormat.Color);
                return;
            }
            if (width == texture.Width && height == texture.Height) { return; }
            device = texture.GraphicsDevice;
            SetTextureResolution(texture, width, height);

            texture.Dispose();
            GC.ReRegisterForFinalize(texture);
            TexturePtr.Set(texture, FNA3D.FNA3D_CreateTexture2D(GLDevice.Get(device), SurfaceFormat.Color, width, height, 1, 0));
        }

        public static unsafe void SetTextureDataFast(this Texture2D texture, Color* pixels)
        {
            if (texture == null || pixels == null) { return; }
            FNA3D.FNA3D_SetTextureData2D(GLDevice.Get(texture.GraphicsDevice), TexturePtr.Get(texture), 0, 0, texture.Width, texture.Height, 0, new IntPtr(pixels), texture.Width * texture.Height * 4);
        }

        public static unsafe void SetTextureDataFastSafe(GraphicsDevice device, ref Texture2D texture, int width, int height, IntPtr pixels)
        {
            if (pixels == IntPtr.Zero) { return; }
            texture ??= new Texture2D(device, width, height, false, SurfaceFormat.Color);
            if (texture.Width != width || texture.Height != height)
            {
                SetTextureResolution(texture, width, height);
            }
            FNA3D.FNA3D_SetTextureData2D(GLDevice.Get(texture.GraphicsDevice), TexturePtr.Get(texture), 0, 0, width, height, 0, pixels, texture.Width * texture.Height * 4);
        }
    }
}