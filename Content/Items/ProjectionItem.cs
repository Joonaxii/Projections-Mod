
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.ID;
using ReLogic.Content;
using Projections.Core.Data;
using Projections.Core.Utilities;
using Projections.Common.Configs;
using Projections.Common.PTypes;

namespace Projections.Content.Items
{
    public class ProjectionItem : ProjectionBase<ProjectionItem>
    {
        public const int DEFAULT_VALUE = 12500;

        public override string Texture => "Projections/Content/Items/ProjectionItemSingle";
        public Projection ProjectionSrc => _projection;

        public override PType PType => PType.TProjection;
        public override bool IsValid => _projection != null;

        [CloneByReference] private Projection _projection;
        private static Asset<Texture2D> _glow, _main;

        public override void Load()
        {
            base.Load();
            CheckTextures();
        }

        private void CheckTextures()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>($"{Mod.Name}/Content/Items/ProjectionItem_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
            _main ??= ModContent.RequestIfExists<Texture2D>($"{Mod.Name}/Content/Items/ProjectionItem", out tex, AssetRequestMode.AsyncLoad) ? tex : null;
        }

        public override void Clear()
        {
            base.Clear();
            _projection = null;
        }

        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.None;

            Clear();
            Validate();
        }

        protected override void DoValidate()
        {
            _projection = Projections.GetProjection(_index);

            var config = ProjectionsServerConfig.Instance;
            bool noPrice = config.DisablePrices && _source.Source != ProjectionSourceType.Shop;
            if (_projection != null)
            {
                Item.rare = _projection.Rarity.ToTerrariaRarity();
                Item.SetNameOverride(_projection.Material.Name);
                Item.value = noPrice ? 0 : _projection.Value;
            }
            else
            {
                if (_index.IsValidID())
                {
                    Item.rare = ItemRarityID.Gray;
                    Item.SetNameOverride("Projection (Unknown)");
                    Item.value = noPrice ? 0 : DEFAULT_VALUE;
                }
                else
                {
                    Item.rare = ItemRarityID.White;
                    Item.SetNameOverride("Projection (Empty)");
                    Item.value = DEFAULT_VALUE;
                }
            }
            Item.master = Item.rare == ItemRarityID.Master;
            Item.expert = Item.rare == ItemRarityID.Expert;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (_projection != null)
            {
                tooltips.RemoveExpertMaster(_projection.Rarity);
                var material = _projection.Material;
                tooltips.Add(new TooltipLine(Mod, "P-Group", $"Group: {material.Group}"));
                tooltips.Add(new TooltipLine(Mod, "Projection", material.Description));
            }
            else
            {
                if(_index.IsValidID())
                {
                    tooltips.Add(new TooltipLine(Mod, "Projection (Unknown)", "You don't have this projection locally!") { OverrideColor = Color.Gray });
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "Projection (Empty)", "Used for crafting projections.") { OverrideColor = Color.White });
                }
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            var uv = new Rectangle(0, 0, 48, 48);
            PRarity rarity = _projection?.Rarity ?? PRarity.Basic;
            Color colorG = rarity.ToColor();

            ProjectionUtils.DrawInGUI(spriteBatch, position, 40, _main.Value, drawColor, scale, uv);
            ProjectionUtils.DrawInGUI(spriteBatch, position, 40, _glow.Value, colorG * Main.essScale, scale, uv);

            var icon = _projection?.Icon;
            if (icon != null)
            {
                ProjectionUtils.DrawInGUI(spriteBatch, position, 32 * GetEssModifier(), icon, drawColor, 0.7f * scale, new Rectangle(0, 0, _projection.Icon.Width, _projection.Icon.Height));
            }
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            var uv = new Rectangle(0, 0, 48, 48);
            byte alpha = GetShimmerAlpha(alphaColor);
            var alphaC = new Color(alpha, alpha, alpha, alpha);
            PRarity rarity = _projection?.Rarity ?? PRarity.Basic;
            Color colorG = rarity.ToColor();
            Vector2 center = Item.position;

            ProjectionUtils.DrawInWorld(spriteBatch, center, Item.width, Item.height, 40, _main.Value, lightColor.MultiplyRGBA(new Color(0xFF, 0xFF, 0xFF, alpha)), scale, uv);
            ProjectionUtils.DrawInWorld(spriteBatch, center, Item.width, Item.height, 40, _glow.Value, colorG.MultiplyRGBA(alphaC) * Main.essScale, scale, uv);

            var icon = _projection?.Icon;
            if (icon != null)
            {
                Color color = Color.White.MultiplyRGBA(alphaC);
                ProjectionUtils.DrawInWorld(spriteBatch, center, Item.width, Item.height, 32 * GetEssModifier(), _projection.Icon, color, scale * 0.65f);
            }
            return false;
        }

        public override void PostUpdate()
        {
            Color colorG = (_projection?.Rarity ?? PRarity.Basic).ToColor();
            float power = IsValid ? 0.65f : 0.25f;
            Lighting.AddLight(Item.Center, colorG.ToVector3() * power * Main.essScale);
            base.PostUpdate();
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
            .AddIngredient(ItemID.IronBar, 3)
            .AddIngredient(ItemID.ItemFrame, 1)
            .AddIngredient(ItemID.Glass, 1)
            .AddTile(TileID.Anvils).DisableDecraft()
            .Register();

            CreateRecipe(1)
           .AddIngredient(ItemID.LeadBar, 3)
           .AddIngredient(ItemID.ItemFrame, 1)
           .AddIngredient(ItemID.Glass, 1)
           .AddTile(TileID.Anvils).DisableDecraft()
           .Register();
        }

        protected override bool AllowDecraft() => _projection != null;
    }
}
