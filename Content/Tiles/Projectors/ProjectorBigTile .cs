using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Projections.Content.Items.Projectors;

namespace Projections.Content.Tiles.Projectors
{
    public class ProjectorBigTile : BaseProjectorTile
    {
        public override string Texture => "Projections/Content/Tiles/Projectors/ProjectorTile_Big";
        public override string HighlightTexture => "Projections/Content/Tiles/Projectors/ProjectorTile_Big_Highlight";
        public override string NameExtension => "Large";

        public override int SlotCount => 48;
        public override Vector2 Hotspot => new Vector2(24, 48);

        private static Asset<Texture2D> _glow;

        public override void Load()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>("Projections/Content/Tiles/Projectors/ProjectorTile_Big_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.AnchorWall = true;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new int[3] 
            { 
                16,16,16 
            };
            TileObjectData.newTile.WaterPlacement = Terraria.Enums.LiquidPlacement.Allowed;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.addTile(Type);

            LocalizedText name = this.GetLocalization("MapEntry", () => "Projector Big");
            AddMapEntry(new Color(200, 170, 130), name);
        }

        public override Point GetTileRegion() => new Point(3, 3);
        public override Asset<Texture2D> GetGlowTexture() => _glow;
        public override int HoverItemID() => ModContent.ItemType<ProjectorBigItem>();

        public override Rectangle? GetUVs(bool isMain, bool isActive)
        {
            return new Rectangle(isActive ? 50 : 0, 0, 16, 16);
        }
    }
}
