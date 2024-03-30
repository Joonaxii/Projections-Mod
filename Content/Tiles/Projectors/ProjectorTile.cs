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
using Projections.Core.Data;
using Terraria.Enums;
using Projections.Common.Netcode;
using Projections.Common.ProjectorTypes;

namespace Projections.Content.Tiles.Projectors
{
    public class ProjectorTile : BaseProjectorTile
    {
        public override string Texture => "Projections/Content/Tiles/Projectors/ProjectorTile";
        public override string HighlightTexture => "Projections/Content/Tiles/Projectors/ProjectorTile_Highlight";
        public override string NameExtension => "Medium";

        public override int SlotCount => 24;
        public override Vector2 Hotspot => new Vector2(24, 16);

        private static Asset<Texture2D> _glow;

        public override void Load()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>("Projections/Content/Tiles/Projectors/ProjectorTile_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileSolid[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.AnchorWall = false;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidWithTop| AnchorType.SolidTile | AnchorType.PlatformNonHammered | AnchorType.PlanterBox, 3, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.Origin = new Point16(0, 0);

            TileObjectData.addTile(Type);

            LocalizedText name = this.GetLocalization("MapEntry", () => "Projector");
            AddMapEntry(new Color(200, 170, 130), name);
        }

        public override void HitWire(int i, int j)
        {
            ProjectorID id = ProjectorID.FromTile(i, j);
            if (ProjectorSystem.TryGetProjector(in id, out var projector))
            {
                int localX = i - projector.TilePosition.X;
                switch (localX)
                {
                    case 0:
                        break;
                    case 1:
                        if (projector.IsActive)
                        {
                            projector.Deactivate();
                        }
                        else
                        {
                            projector.Play();
                        }
                        ProjectionNetUtils.SendProjectorUpdate(projector, SerializeType.Partial, Main.myPlayer);
                        break;
                    case 2:
                        break;
                }
            }
        }

        public override Point GetTileRegion() => new Point(3, 1);
        public override Asset<Texture2D> GetGlowTexture() => _glow;
        public override int HoverItemID() => ModContent.ItemType<ProjectorItem>();

        public override Rectangle? GetUVs(bool isMain, bool isActive)
        {
            return new Rectangle(isActive ? 50 : 0, 0, 16, 16);
        }
    }
}
