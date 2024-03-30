using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Data;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;
using Terraria;
using Projections.Core.Utilities;
using Projections.Common.Configs;
using Projections.Common.PTypes;

namespace Projections.Content.Items
{
    public class ProjectionMaterial : ProjectionBase<ProjectionMaterial>
    {
        public override PType PType => PType.TPMaterial;
        public override bool IsValid => _material != null;
        [CloneByReference] private PMaterial _material;
        private static Asset<Texture2D> _main;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 0;
        }

        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.width = 26;
            Item.height = 26;
            Item.scale = 1.0f;
            Item.useStyle = ItemUseStyleID.None;
            Item.useTime = 10;
            Item.useAnimation = 10;

            Clear();
            Validate();
        }

        public override void Load()
        {
            base.Load();
            if(_main == null)
            {
                ModContent.RequestIfExists("Projections/Content/Items/ProjectionMaterial", out _main);
            }
        }

        public override void Clear()
        {
            base.Clear();
            _material = null;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if(_material != null)
            {
                tooltips.RemoveExpertMaster(_material.Rarity);
                tooltips.Add(new TooltipLine(Mod, "P-Group", $"Group: {_material.Group}"));
                tooltips.Add(new TooltipLine(Mod, "P-Material", _material.Description));
            }
            else
            {
                if (_index.IsValidID())
                {
                    tooltips.Add(new TooltipLine(Mod, "P-Material (Unknown)", "You don't have this material locally!") { OverrideColor = Color.Gray });
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "P-Material (Empty)", "Hmm you really shouldn't have this item under normal circumstances.") { OverrideColor = Color.Gray });
                }
            } 
        }

        protected override void DoValidate()
        {
            _material = Projections.GetMaterial(_index, PType.TPMaterial);
            var config = ProjectionsServerConfig.Instance;
            bool noPrice = config.DisablePrices && _source.Source != ProjectionSourceType.Shop;
            if (_material != null)
            {
                Item.rare = _material.Rarity.ToTerrariaRarity();
                Item.SetNameOverride(_material.Name);
                Item.value = noPrice ? 0 : _material.Value;
            }
            else
            {
                if (_index.IsValidID())
                {
                    Item.rare = ItemRarityID.Gray;
                    Item.SetNameOverride("P-Material (Unknown)");
                }
                else
                {
                    Item.rare = ItemRarityID.Gray;
                    Item.SetNameOverride("P-Material (Empty)");
                }
                Item.value = 0;
            }

            Item.master = Item.rare == ItemRarityID.Master;
            Item.expert = Item.rare == ItemRarityID.Expert;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            var icon = _material?.Icon;
            if (icon != null)
            {
                ProjectionUtils.DrawInGUI(spriteBatch, position, 32 * GetEssModifier(), icon, drawColor, scale, new Rectangle(0, 0, icon.Width, icon.Height));
                return false;
            }
            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {     
            var icon = _material?.Icon;
            if (icon != null)
            {
                byte alpha = GetShimmerAlpha(alphaColor);
                var alphaC = new Color(alpha, alpha, alpha, alpha);
                Color colorG = Color.White.MultiplyRGBA(alphaC);
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, 32 * GetEssModifier(), icon, colorG, scale, new Rectangle(0, 0, icon.Width, icon.Height));
                return false;
            }
            return true;
        }

        public override void PostUpdate()
        {
            Color colorG = (_material?.Rarity ?? PRarity.Basic).ToColor();
            float power = IsValid ? 0.65f : 0.25f;
            Lighting.AddLight(Item.Center, colorG.ToVector3() * power * Main.essScale);
            base.PostUpdate();
        }

    }
}
