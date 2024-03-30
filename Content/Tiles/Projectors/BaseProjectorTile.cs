using Microsoft.Xna.Framework;
using Projections.Core.Systems;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ObjectData;
using Projections.Core.Collections;
using Projections.Common.Netcode;
using Projections.Core.Data;
using Projections.Core.Utilities;
using Projections.Common.ProjectorTypes;
using Terraria.GameContent.ObjectInteractions;

namespace Projections.Content.Tiles.Projectors
{
    public abstract class BaseProjectorTile : ModTile, IProjector
    {
        internal readonly static OrderedList<int> ProjectorTileLUT = new OrderedList<int>(4, false);

        public abstract string NameExtension { get; }

        public ProjectorType ProjectorType => ProjectorType.Tile;
        public virtual bool CanBeUsedInRecipe => true;

        public virtual int SlotCount => 8;
        public uint CreatorTag => Projections.DEFAULT_PROJECTOR_ID;
        public abstract Vector2 Hotspot { get; }

        public abstract Rectangle? GetUVs(bool isMain, bool isActive);

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;
            ProjectorTileLUT.Add(Type);
            Main.tileLighted[Type] = true;
        }

        public abstract Point GetTileRegion();
        public abstract int HoverItemID();

        public override bool RightClick(int i, int j)
        {
            ProjectorID data = ProjectorID.FromTile(i, j);
            if (ProjectorSystem.TryGetProjector(in data, out var projector))
            {
                Player player = Main.LocalPlayer;
                Main.mouseRightRelease = false;

                player.CloseSign();
                player.SetTalkNPC(-1);

                UISystem.OpenProjectorUI(projector);
                return true;
            }
            return false;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            const float NORMALIZE_BYTE = 1.0f / 255.0f;
            float power = 0.15f;
            PRarity rarity = PRarity.Basic;

            if(ProjectorSystem.TryGetProjector(ProjectorID.FromTile(i, j), out var proj))
            {
                power = proj.IsPlaying ? 1.0f : proj.IsActive ? 0.35f : power;
                rarity = proj.Rarity;
            }

            Color color = rarity.ToColor() * power * Main.essScale;
            r += color.R * NORMALIZE_BYTE;
            g += color.G * NORMALIZE_BYTE;
            b += color.B * NORMALIZE_BYTE;
        }

        public override void PlaceInWorld(int i, int j, Item item)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            int style = TileObjectData.GetTileStyle(tile);

            ProjectorData pdata = ProjectorData.NewTile(CreatorTag, Hotspot, SlotCount, new Point(i, j), GetTileRegion(), style);

            var proj = ProjectorSystem.AddProjector(in pdata);
            if (proj != null && Main.netMode == NetmodeID.MultiplayerClient)
            {
                ProjectionNetUtils.SendCreateProjector(proj, Main.myPlayer);
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            var tile = Main.tile[i, j];
            if (tile.IsTileInvisible && !Main.ShouldShowInvisibleWalls()) { return; }
            ProjectorID data = ProjectorID.FromTile(i, j);
            bool found = ProjectorSystem.TryGetProjector(in data, out var projector);
            Color color = (found ? projector.Rarity : PRarity.Basic).ToColor();

            Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
            if (Main.drawToScreen)
            {
                zero = Vector2.Zero;
            }

            int x = ((i << 4) - (int)Main.screenPosition.X) + (int)zero.X;
            int y = ((j << 4) - (int)Main.screenPosition.Y) + (int)zero.Y;

            var rect = GetUVs(false, projector.IsPlaying).GetValueOrDefault();
            var rectDst = new Rectangle(x, y, 16, 16);

            var rectSrc = new Rectangle(rect.X + tile.TileFrameX, rect.Y + tile.TileFrameY, rect.Width, rect.Height);
            spriteBatch.Draw(GetGlowTexture().Value, rectDst, rectSrc, color * Main.essScale);
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            if (player.mouseInterface || !player.InInteractionRange(i, j, TileReachCheckSettings.Simple)) { return; }

            ProjectorID data = ProjectorID.FromTile(i, j);
            if (!ProjectorSystem.TryGetProjector(in data, out var _)) { return; }

            player.cursorItemIconID = HoverItemID();
            player.cursorItemIconEnabled = true;
        }

        public override void MouseOverFar(int i, int j)
        {
            MouseOver(i, j);
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (!fail && !effectOnly)
            {
                ProjectorID data = ProjectorID.FromTile(i, j);
                if (ProjectorSystem.KillProjector(in data, true, out var projector) && Main.netMode != NetmodeID.SinglePlayer)
                {
                    ProjectionNetUtils.SendKillProjector(projector, !ProjectorSystem.EjectSuppressed(ProjectorType.Tile), Main.myPlayer);
                }
            }
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override void HitWire(int i, int j)
        {
            ProjectorID data = ProjectorID.FromTile(i, j);
            if (ProjectorSystem.TryGetProjector(in data, out var projector))
            {
                if (projector.IsActive)
                {
                    projector.Deactivate();
                }
                else
                {
                    projector.Play();
                }
                ProjectionNetUtils.SendProjectorUpdate(projector, SerializeType.Partial, Main.myPlayer);
            }
        }

        public Asset<Texture2D> GetMainTexture() => null;
        public abstract Asset<Texture2D> GetGlowTexture();
    }
}
