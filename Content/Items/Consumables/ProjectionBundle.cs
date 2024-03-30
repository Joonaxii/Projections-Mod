using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.PTypes;
using Projections.Core.Data;
using Projections.Core.Maths;
using Projections.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace Projections.Content.Items.Consumables
{
    public class ProjectionBundle : ProjectionBase<ProjectionBundle>
    {
        public override PType PType => PType.TPMaterial;
        public override bool IsValid => _bundle != null;

        public override string Texture => "Projections/Content/Items/Consumables/ProjectionBundle";
        public PBundle BundleSrc => _bundle;

        [CloneByReference] private PBundle _bundle;
        private static Asset<Texture2D> _glow, _main;

        public override void SetDefaults()
        {
            _main ??= ModContent.Request<Texture2D>(Texture);
            _glow ??= ModContent.Request<Texture2D>("Projections/Content/Items/Consumables/ProjectionBundle_Glow");
            base.SetDefaults();
            Item.maxStack = Item.CommonMaxStack;
            Item.width = 24;
            Item.height = 32;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (_bundle != null)
            {
                tooltips.RemoveExpertMaster(_bundle.Rarity);
                tooltips.Add(new TooltipLine(Mod, "P-Bundle-Info", "Right click to open..."));
                tooltips.Add(new TooltipLine(Mod, "P-Group", $"Group: {_bundle.Material.Group}"));
                tooltips.Add(new TooltipLine(Mod, "P-Bundle", _bundle.Description));
            }
            else
            {
                if (_index.IsValidID())
                {
                    tooltips.Add(new TooltipLine(Mod, "P-Bundle (Unknown)", "You don't have this bundle locally!") { OverrideColor = Color.Gray });
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "P-Bundle (Empty)", "Uh oh, this shouldn't be possible under normal circumstances! D:") { OverrideColor = Color.Gray });
                }
            }
        }
        public override bool CanRightClick() => _bundle?.IsValid ?? false;

        public override void RightClick(Player player)
        {
            base.RightClick(player);
            _bundle?.SpawnAsItems(player.Center);
        }

        protected override void DoValidate()
        {
            _bundle = Projections.GetBundle(_index);
            if(_bundle != null)
            {
                Item.value = _bundle.Material.Value;
                Item.rare = _bundle.Rarity.ToTerrariaRarity();
                Item.expert = Item.rare == ItemRarityID.Expert;
                Item.master = Item.rare == ItemRarityID.Master;
                Item.SetNameOverride(_bundle.Name);
            }
            else
            {
                Item.value = 150;
                Item.rare = ItemRarityID.Gray;
                Item.expert = false;
                Item.master = false;
                Item.SetNameOverride("P-Bundle (Unknown)");
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            var bTex = _bundle?.Material.Icon ?? Terraria.GameContent.TextureAssets.Sun.Value;
            if (bTex != null)
            {
                ProjectionUtils.DrawInGUI(spriteBatch, position, 56 * GetEssModifier(0.85f, 1.25f), bTex, new Color(0xFF, 0xFF, 0xFF, 96), scale);
            }

            PRarity rarity = _bundle?.Rarity ?? PRarity.Basic;
            ProjectionUtils.DrawInGUI(spriteBatch, position, 36, _main.Value, drawColor, scale);
            ProjectionUtils.DrawInGUI(spriteBatch, position, 36, _glow.Value, rarity.ToColor(), scale);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            var bTex = _bundle?.Material.Icon ?? Terraria.GameContent.TextureAssets.Sun.Value;
            if (bTex != null)
            {
                byte alpha = PMath.MultUI8LUT(GetShimmerAlpha(alphaColor), 164);
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, 56 * GetEssModifier(0.85f, 1.25f), bTex, Color.White.MultiplyRGBA(new Color(alpha, alpha, alpha, alpha)), scale);
            }

            PRarity rarity = _bundle?.Rarity ?? PRarity.Basic;
            ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, 36, _main.Value, lightColor.MultiplyRGBA(new Color(0xFF, 0xFF, 0xFF, GetShimmerAlpha(alphaColor))), scale);
            ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, 36, _glow.Value, rarity.ToColor().MultiplyRGBA(new Color(0xFF, 0xFF, 0xFF, GetShimmerAlpha(alphaColor))), scale);
            return false;
        }

        public override void PostUpdate()
        {
            Color colorG = (_bundle?.Rarity ?? PRarity.Basic).ToColor();
            float power = IsValid ? 0.65f : 0.25f;
            Lighting.AddLight(Item.Center, colorG.ToVector3() * power * Main.essScale);
            base.PostUpdate();
        }
    }
}
