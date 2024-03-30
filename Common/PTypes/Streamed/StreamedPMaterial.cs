using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Projections.Core.Data;
using Projections.Core.Utilities;
using Projections.Core.Data.Structures;
using Projections.Core.Textures;
using Projections.Common.PTypes.OnDisk;

namespace Projections.Common.PTypes.Streamed
{
    /// <summary>
    /// P-Material for my own Streamed Projection format.
    /// </summary>
    public sealed class StreamedPMaterial : OnDiskPMaterial
    {
        public override Texture2D Icon => _icon;
        private Texture2D _icon;

        public StreamedPMaterial(string path, uint id) : base(path, id) { }

        ~StreamedPMaterial()
        {
            Unload();
        }

        public override void Unload()
        {
            base.Unload();
            UnloadIcon();
        }

        public override bool Deserialize(BinaryReader br, Stream stream)
        {
            UnloadIcon();
            int version = br.ReadInt32();
            if (version != Projections.MOD_IO_VERSION)
            {
                Projections.Log(LogType.Warning, $"Could not load Material/Projection! (Projection IO Version mismatch, {version} =/= {Projections.MOD_IO_VERSION})");
                return false;
            }

            _id = br.ReadShortString();
            _index = _id.ParseProjectionID();

            _name = br.ReadShortString();
            _description = br.ReadShortString();

            _rarity = br.Read<PRarity>();
            _priority = br.ReadInt32();
            _flags = br.Read<PMaterialFlags>();
            _value = br.ReadInt32();
            int count = br.ReadUInt16();

            _sources.Resize(count);
            for (int i = 0; i < count; i++)
            {
                _sources[i].Deserialize(br);
            }

            int recipes = br.ReadUInt16();
            _recipes.Resize(recipes);

            for (int i = 0; i < recipes; i++)
            {
                int alts = br.ReadByte();
                var recipe = _recipes[i] = PRecipe.Create(alts);

                int recipeC = br.ReadInt32();
                for (int k = 0; k < recipeC; k++)
                {
                    recipe.AddIngredient(ReadRecipeItem(br));
                    for (int j = 0; j < alts; j++)
                    {
                        recipe.AddAlt(ReadRecipeItem(br));
                    }
                }
                recipe.Finish();
            }

            TexFormat icon = (TexFormat)br.ReadByte();

            long size = br.ReadUInt32();
            long curPos = stream.Position;
            switch (icon)
            {
                case TexFormat.PNG:
                    _icon = Texture2D.FromStream(Main.graphics.GraphicsDevice, stream);
                    break;
                case TexFormat.DDS:
                    _icon = Texture2DUtils.LoadDDS(Main.graphics.GraphicsDevice, stream);
                    break;
                case TexFormat.JTEX:
                    _icon = Texture2DUtils.LoadJTEX(Main.graphics.GraphicsDevice, stream);
                    break;
                case TexFormat.RLE:
                    _icon = Texture2DUtils.LoadRLE(Main.graphics.GraphicsDevice, stream);
                    break;
            }
            stream.Seek(curPos + size, SeekOrigin.Begin);
            return _index.IsValidID();
        }

        private void UnloadIcon()
        {
            _icon?.Dispose();
            _icon = null;
        }
        private static RecipeItem ReadRecipeItem(BinaryReader br)
        {
            RecipeType type = br.Read<RecipeType>();
            int stack = Math.Max(br.ReadInt32(), 1);
            switch (type)
            {
                default:
                    return RecipeItem.FromNone();
                case RecipeType.Modded:
                    {
                        Span<char> temp = stackalloc char[256];
                        temp = br.ReadShortString(temp);
                        return RecipeItem.FromModded(temp, stack);
                    }
                case RecipeType.Vanilla:
                    return RecipeItem.FromID(br.ReadInt32(), stack);
                case RecipeType.Projection:
                case RecipeType.ProjectionMaterial:
                case RecipeType.ProjectionBundle:
                    return RecipeItem.FromProjection(br.Read<ProjectionIndex>(), (PType)(type - RecipeType.Projection), stack);
            }
        }
    }
}