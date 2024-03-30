using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Content.NPCs;
using Projections.Core.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Content.Items.Consumables
{
    public class BookOfProjections : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<LicenseToProject>();
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.width = 16;
            Item.height = 20;
            Item.maxStack = 1;
            Item.value = 25000;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            // TODO: Open crafting interface
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
             .AddIngredient(ItemID.Book)
             .AddIngredient(ItemID.Sapphire, 2)
             .AddIngredient(ItemID.Paintbrush)
             .AddIngredient(ItemID.PaintRoller)
             .AddIngredient(ItemID.PaintScraper)
             .AddTile(TileID.Anvils).DisableDecraft()
             .Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            scale *= 1.25f;
            ProjectionUtils.DrawInGUI(spriteBatch, position, 40, ModContent.Request<Texture2D>("Projections/Content/Items/Consumables/BookOfProjections").Value, drawColor, scale);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            ProjectionUtils.DrawInWorld(spriteBatch, Item.Center, 0, 0, 40, ModContent.Request<Texture2D>("Projections/Content/Items/Consumables/BookOfProjections").Value, lightColor.MultiplyRGBA(new Color(0xFF, 0xFF, 0xFF, alphaColor.A)), scale);
            return false;
        }
    }
}
