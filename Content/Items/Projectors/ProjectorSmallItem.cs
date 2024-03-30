using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Data;
using Projections.Core.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Content.Items.Projectors
{
    public class ProjectorSmallItem : OverrideShimmerFX, IProjectorItem
    {
        public static Texture2D Glow => _glow?.Value;
        public override string Texture => "Projections/Content/Items/Projectors/ProjectorItem_Small";
        public int PlacementTileID => ModContent.TileType<Tiles.Projectors.ProjectorSmallTile>();

        private static Asset<Texture2D> _main;
        private static Asset<Texture2D> _glow;

        public override void Load()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>("Projections/Content/Items/Projectors/ProjectorItem_Small_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
            _main ??= ModContent.RequestIfExists<Texture2D>(Texture, out tex, AssetRequestMode.AsyncLoad) ? tex : null;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = Item.CommonMaxStack;

            Item.value = 5000;

            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;

            Item.rare = ItemRarityID.Green;
            Item.createTile = PlacementTileID;
        }

        private static Rectangle GetTextureRect()
        {
            return new Rectangle(0, 0, 16, 16);
        }
        private const float UI_SCALE = 1.25f;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            var rect = GetTextureRect();
            scale *= UI_SCALE;
            ProjectionUtils.DrawInGUI(spriteBatch, position, 16, _main.Value, drawColor, scale, rect);
            if (_glow != null)
            {
                ProjectionUtils.DrawInGUI(spriteBatch, position, 16, _glow.Value, PRarity.Basic.ToColor() * Main.essScale, scale, rect);
            }
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            var rect = GetTextureRect();
            byte alpha = GetShimmerAlpha(alphaColor);
            var alphaC = new Color(alpha, alpha, alpha, alpha);
            ProjectionUtils.DrawInWorld(spriteBatch, Item.position, 16, 16, 16, _main.Value, lightColor.MultiplyRGBA(alphaC), scale, rect);

            if (_glow != null)
            {
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position, 16, 16, 16, _glow.Value, PRarity.Basic.ToColor().MultiplyRGBA(alphaC) * Main.essScale, scale, rect);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
           .AddIngredient(ItemID.IronBar, 5)
           .AddIngredient(ItemID.Lens, 2)
           .AddIngredient(ItemID.Ruby, 1)
           .AddIngredient(ItemID.Emerald, 1)
           .AddIngredient(ItemID.Sapphire, 1)
           .AddTile(TileID.Anvils)
           .Register();

            CreateRecipe(1)
            .AddIngredient(ItemID.LeadBar, 5)
            .AddIngredient(ItemID.Lens, 2)
            .AddIngredient(ItemID.Ruby, 1)
            .AddIngredient(ItemID.Emerald, 1)
            .AddIngredient(ItemID.Sapphire, 1)
            .AddTile(TileID.Anvils)
            .Register();
        }

        public override void PostUpdate()
        {
            const float POWER = 0.5f;
            Lighting.AddLight(Item.Center, (PRarity.Basic).ToColor().ToVector3() * POWER * Main.essScale);
            base.PostUpdate();
        }
    }
}
