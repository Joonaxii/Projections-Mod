using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Data;
using Projections.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Content.Items.RarityStones
{
    public abstract class RarityStone : ModItem
    {
        public abstract PRarity Rarity { get; }
        public override string Texture => $"{Mod.Name}/Content/Items/RarityStones/RarityStones_S{(int)Rarity+1}";
        private static Asset<Texture2D> _main;

        private void CheckTextures()
        {
            if (_main == null)
            {
                _main = ModContent.RequestIfExists<Texture2D>($"{Mod.Name}/Content/Items/RarityStones/RarityStones", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
            }
        }

        public override void Load()
        {
            CheckTextures();
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.maxStack = Terraria.Item.CommonMaxStack;
            Item.width = 16;
            Item.height = 16;
            Item.scale = 1.0f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 10;

            Item.value = Rarity.ToCopperValue();
            Item.rare = Rarity.ToTerrariaRarity();

            Item.master = Item.rare == ItemRarityID.Master;
            Item.expert = Item.rare == ItemRarityID.Expert;

            Item.SetNameOverride($"Rarity Stone ({Rarity})");
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            switch (Rarity)
            {
                case PRarity.Expert:
                case PRarity.Master:
                    tooltips.RemoveAll((TooltipLine tip) => tip.Name == "Master" || tip.Name == "Expert");
                    break;
            }
            Color clr = Rarity.ToColor();
            tooltips.Add(new TooltipLine(Mod, "Rarity Stone", "Material used for crafting Projections"));
        }

        public Rectangle GetUVRect()
        {
            int index = Rarity.ToIndex();
            return new Rectangle(0, index * 16, 16, 16);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Color colorG = Rarity.ToColor() * Main.essScale;
            ProjectionUtils.DrawInGUI(spriteBatch, position, 14, _main.Value, colorG, scale, GetUVRect());
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            var alphaC = new Color(0xFF, 0xFF, 0xFF, alphaColor.A);

            Color colorG = Rarity.ToColor() * Main.essScale;
            ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, 14, _main.Value, colorG.MultiplyRGBA(alphaC), scale, GetUVRect());
            return false;
        }

        public override void PostUpdate()
        {
            Color colorG = Rarity.ToColor();
            Lighting.AddLight(Item.Center, colorG.ToVector3() * 0.55f * Main.essScale);
        }
    }

    public class RarityStone0 : RarityStone 
    { 
        public override PRarity Rarity => 0;
    }
    public class RarityStone1 : RarityStone 
    { 
        public override PRarity Rarity => PRarity.Intermediate;
    }
    public class RarityStone2 : RarityStone 
    { 
        public override PRarity Rarity => PRarity.Advanced;
    }
    public class RarityStone3 : RarityStone 
    { 
        public override PRarity Rarity => PRarity.Expert;
    }
    public class RarityStone4 : RarityStone 
    { 
        public override PRarity Rarity => PRarity.Master;
    }
}
