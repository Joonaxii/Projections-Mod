using Projections.Content.Items;
using Projections.Core.Systems;
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
    public class ProjectorSmallTile : BaseProjectorTile
    {
        public override string Texture => "Projections/Content/Tiles/Projectors/ProjectorTile_Small";
        public override string HighlightTexture => "Projections/Content/Tiles/Projectors/ProjectorTile_Small_Highlight";
        public override string NameExtension => "Small";

        public override int SlotCount => 8;
        public override Vector2 Hotspot => new Vector2(8, 16);

        private static Asset<Texture2D> _glow;

        public override void Load()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>("Projections/Content/Tiles/Projectors/ProjectorTile_Small_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
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

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorWall = true;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.WaterPlacement = Terraria.Enums.LiquidPlacement.Allowed;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.addTile(Type);

            LocalizedText name = this.GetLocalization("MapEntry", () => "Projector Small");
            AddMapEntry(new Color(200, 170, 130), name);
        }

        public override Point GetTileRegion() => new Point(1, 1);
        public override Asset<Texture2D> GetGlowTexture() => _glow;
        public override int HoverItemID() => ModContent.ItemType<ProjectorSmallItem>();

        public override Rectangle? GetUVs(bool isMain, bool isActive)
        {
            return new Rectangle(isActive ? 18 : 0, 0, 16, 16);
        }
    }
}
