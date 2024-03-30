using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using System;
using Projections.Core.Data;

namespace Projections.Core.Textures
{
    public class ProjectorTexture : IDisposable
    {
        public bool CanDraw => _actualLayers > 0;
        public int LayerCount => _actualLayers;
        private TextureLayer[] _layers = new TextureLayer[0];
        private GraphicsDevice _device;
        private int _actualLayers = 0;

        ~ProjectorTexture()
        {
            Reset(true);
        }

        /// <summary>
        /// Has to be called before adding any layers, calls Setup with the Main.graphics.GraphicsDevice.
        /// </summary>
        public ProjectorTexture Setup(int layers)
            => Setup(Main.graphics.GraphicsDevice, layers);

        /// <summary>
        /// Has to be called before adding any layers!
        /// </summary>
        public ProjectorTexture Setup(GraphicsDevice gfxDevice, int layers)
        {
            if (layers <= 0)
            {
                Projections.Log(LogType.Error, $"Layer count must be dreater than 0, but got {layers}!");
                return this;
            }

            if (gfxDevice == null)
            {
                Projections.Log(LogType.Error, $"Given GraphicsDevice was null!");
                return this;
            }

            _device = gfxDevice;
            _actualLayers = 0;

            for (int i = layers; i < _layers.Length; i++)
            {
                _layers[i].Clear();
            }
            Array.Resize(ref _layers, layers);
            return this;
        }

        /// <summary>
        /// Sets up a ProjectorTexture with "external" textures which aren't disposed when this ProjectorTexture disposes.
        /// </summary>
        public ProjectorTexture AddLayer(Texture2D diffuse, Texture2D emission)
        {
            if (_device == null)
            {
                Projections.Log(LogType.Error, "You have to call Setup before AddLayer!");
                return this;
            }

            if (_actualLayers >= _layers.Length)
            {
                Projections.Log(LogType.Error, "Cannot add more layers maximum layers have already been reached!");
                return this;
            }

            if (diffuse == null && emission == null)
            {
                _layers[_actualLayers++].Initialize(diffuse, emission);
                return this;
            }

            int widthD = diffuse?.Width ?? 0;
            int heightD = diffuse?.Height ?? 0;

            int widthE = emission?.Width ?? 0;
            int heightE = emission?.Height ?? 0;

            int maxW = Math.Max(widthD, widthE);
            int minW = Math.Min(widthD, widthE);

            int maxH = Math.Max(heightD, heightE);
            int minH = Math.Min(heightD, heightE);

            if (minW != 0 && maxW != minW || minH != 0 && maxH != minH)
            {
                Projections.Log(LogType.Error, $"Textures passed into 'ProjectorTexture.AddLayer' don't have the same resolutions! ({widthD}x{heightD} =/= {widthE}x{heightE})");
                return this;
            }

            _layers[_actualLayers++].Initialize(diffuse, emission);
            return this;
        }

        /// <summary>
        /// Advances the layer count.
        /// </summary>
        public ProjectorTexture AddLayer()
        {
            if (_device == null)
            {
                Projections.Log(LogType.Error, "You have to call Setup before AddLayer!");
                return this;
            }

            if (_actualLayers >= _layers.Length)
            {
                Projections.Log(LogType.Error, "Cannot add more layers maximum layers have already been reached!");
                return this;
            }

            _actualLayers++;
            return this;
        }

        /// <summary>
        /// Adds a Layer from given raw pixel pointers, pixels are assumed to be 8 bit RGBA.
        /// </summary>
        public ProjectorTexture AddLayer(IntPtr diffuse, IntPtr emission, int width, int height)
        {
            if (_device == null)
            {
                Projections.Log(LogType.Error, "You have to call Setup before AddLayer!");
                return this;
            }

            if (_actualLayers >= _layers.Length)
            {
                Projections.Log(LogType.Error, "Cannot add more layers maximum layers have already been reached!");
                return this;
            }

            if (diffuse == IntPtr.Zero && emission == IntPtr.Zero)
            {
                _layers[_actualLayers++].Initialize(_device, diffuse, emission, width, height);
                return this;
            }

            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
            {
                Projections.Log(LogType.Error, $"Given width/height ({width}x{height}) are invalid for ProjectorTexture! (Both have to more than 0 and at MAX 4096)");
                return this;
            }

            _layers[_actualLayers++].Initialize(_device, diffuse, emission, width, height);
            return this;
        }

        /// <summary>
        /// Sets current a Layer's pixel data from given raw pixel pointer, pixels are assumed to be 8 bit RGBA.
        /// </summary>
        public ProjectorTexture SetLayer(IntPtr data, bool isEmission, int width, int height)
        {
            if (_device == null)
            {
                Projections.Log(LogType.Error, "You have to call Setup before AddLayer!");
                return this;
            }

            if (_actualLayers >= _layers.Length)
            {
                Projections.Log(LogType.Error, "Cannot add more layers maximum layers have already been reached!");
                return this;
            }

            if (data == IntPtr.Zero)
            {
                _layers[_actualLayers].Initialize(_device, data, isEmission, width, height);
                return this;
            }

            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
            {
                Projections.Log(LogType.Error, $"Given width/height ({width}x{height}) are invalid for ProjectorTexture! (Both have to more than 0 and at MAX 4096)");
                return this;
            }

            _layers[_actualLayers].Initialize(_device, data, isEmission, width, height);
            return this;
        }



        public bool DrawLayer(SpriteBatch batch, int layer, ProjectorTexFlags drawFlags, ref Rectangle src, ref Rectangle dst, Color diffColor, Color emissColor, SpriteEffects effects, Vector2 origin, float rotation)
        {
            if (layer < 0 || layer >= _actualLayers) { return false; }
            _layers[layer].Draw(batch, drawFlags, ref src, ref dst, diffColor, emissColor, effects, origin, rotation);
            return true;
        }

        public void Reset(bool fullClear)
        {
            _device = null;
            _actualLayers = 0;
            if (fullClear)
            {
                Free();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Reset(true);
        }

        private void Free()
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i].Clear();
            }
            Array.Resize(ref _layers, 0);
        }

        private enum TextureFlags
        {
            NoFree = 0x1,
            IsValidD = 0x2,
            IsValidE = 0x4,

            IsValid = IsValidD | IsValidE
        }

        private struct TextureLayer
        {
            public int Width => diffuse?.Width ?? emission?.Width ?? 0;
            public int Height => diffuse?.Height ?? emission?.Height ?? 0;
            public Texture2D diffuse, emission;
            public TextureFlags flags;

            public void Initialize(Texture2D diffuse, Texture2D emission)
            {
                if (!flags.HasFlag(TextureFlags.NoFree))
                {
                    Clear();
                }

                this.diffuse = diffuse;
                this.emission = emission;
                flags = diffuse != null ? flags | TextureFlags.IsValidD : flags & ~TextureFlags.IsValidD;
                flags = emission != null ? flags | TextureFlags.IsValidE : flags & ~TextureFlags.IsValidE;
            }

            public void Initialize(GraphicsDevice device, IntPtr diffuse, IntPtr emission, int width, int height)
            {
                if (flags.HasFlag(TextureFlags.NoFree))
                {
                    Clear();
                }
                TextureExtensions.SetTextureDataFastSafe(device, ref this.diffuse, width, height, diffuse);
                TextureExtensions.SetTextureDataFastSafe(device, ref this.emission, width, height, emission);
                flags = diffuse != IntPtr.Zero ? flags | TextureFlags.IsValidD : flags & ~TextureFlags.IsValidD;
                flags = emission != IntPtr.Zero ? flags | TextureFlags.IsValidE : flags & ~TextureFlags.IsValidE;
            }


            public void Initialize(GraphicsDevice device, IntPtr data, bool isEmission, int width, int height)
            {
                if (flags.HasFlag(TextureFlags.NoFree))
                {
                    Clear();
                }

                if (isEmission)
                {
                    TextureExtensions.SetTextureDataFastSafe(device, ref emission, width, height, data);
                    flags = data != IntPtr.Zero ? flags | TextureFlags.IsValidE : flags & ~TextureFlags.IsValidE;
                }
                else
                {
                    TextureExtensions.SetTextureDataFastSafe(device, ref diffuse, width, height, data);
                    flags = data != IntPtr.Zero ? flags | TextureFlags.IsValidD : flags & ~TextureFlags.IsValidD;
                }
            }

            public void Draw(SpriteBatch batch, ProjectorTexFlags drawFlags, ref Rectangle src, ref Rectangle dst, Color diffColor, Color emissColor, SpriteEffects effects, Vector2 origin, float rotation)
            {
                if ((flags & TextureFlags.IsValid) != 0)
                {
                    if (diffColor.A > 0 && drawFlags.HasFlag(ProjectorTexFlags.DrawDiffuse) && flags.HasFlag(TextureFlags.IsValidD))
                    {
                        batch.Draw(diffuse, dst, src, diffColor, rotation, origin, effects, 0.0f);
                    }

                    if (emissColor.A > 0 && drawFlags.HasFlag(ProjectorTexFlags.DrawEmission) && flags.HasFlag(TextureFlags.IsValidE))
                    {
                        batch.Draw(emission, dst, src, emissColor, rotation, origin, effects, 0.0f);
                    }
                }
            }

            public void Clear()
            {
                if (!flags.HasFlag(TextureFlags.NoFree))
                {
                    diffuse?.Dispose();
                    emission?.Dispose();
                }

                diffuse = null;
                emission = null;

                flags &= ~TextureFlags.IsValid;
            }
        }
    }

    public enum ProjectorTexFlags : byte
    {
        None = 0x00,
        DrawDiffuse = 0x1,
        DrawEmission = 0x2,
    }
}
