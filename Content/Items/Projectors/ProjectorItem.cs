using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Data;
using Terraria;
using Terraria.ID;
using ReLogic.Content;
using Terraria.ModLoader;
using Projections.Core.Utilities;

namespace Projections.Content.Items.Projectors
{
    public class ProjectorItem : OverrideShimmerFX, IProjectorItem
    {
        public override string Texture => "Projections/Content/Items/Projectors/ProjectorItem";

        public int PlacementTileID => ModContent.TileType<Tiles.Projectors.ProjectorTile>();

        private static Asset<Texture2D> _main;
        private static Asset<Texture2D> _glow;

        public override void Load()
        {
            _glow ??= ModContent.RequestIfExists<Texture2D>("Projections/Content/Items/Projectors/ProjectorItem_Glow", out var tex, AssetRequestMode.AsyncLoad) ? tex : null;
            _main ??= ModContent.RequestIfExists<Texture2D>(Texture, out tex, AssetRequestMode.AsyncLoad) ? tex : null;
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 16;
            Item.maxStack = Item.CommonMaxStack;

            Item.value = 10000;

            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;

            Item.rare = ItemRarityID.Green;
            Item.createTile = PlacementTileID;
        }

        private const float UI_SCALE = 1.25f;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            scale *= UI_SCALE;
            ProjectionUtils.DrawInGUI(spriteBatch, position, 48, _main.Value,drawColor, scale);
            if (_glow != null)
            {
                ProjectionUtils.DrawInGUI(spriteBatch, position, 48, _glow.Value, PRarity.Basic.ToColor() * Main.essScale, scale);
            }
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            byte alpha = GetShimmerAlpha(alphaColor);
            var alphaC = new Color(alpha, alpha, alpha, alpha);
            ProjectionUtils.DrawInWorld(spriteBatch, Item.position, 48, 16, 48, _main.Value, lightColor.MultiplyRGBA(alphaC), scale);

            if (_glow != null)
            {
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position,  48, 16, 48, _glow.Value, PRarity.Basic.ToColor().MultiplyRGBA(alphaC) * Main.essScale, scale);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
           .AddIngredient(ItemID.IronBar, 10)
           .AddIngredient(ItemID.Wire, 25)
           .AddIngredient(ItemID.PixelBox, 16)
           .AddIngredient(ItemID.Ruby, 1)
           .AddIngredient(ItemID.Emerald, 1)
           .AddIngredient(ItemID.Sapphire, 1)
           .AddTile(TileID.Anvils)
           .Register();


            CreateRecipe(1)
            .AddIngredient(ItemID.LeadBar, 10)
            .AddIngredient(ItemID.Wire, 25)
            .AddIngredient(ItemID.PixelBox, 16)
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
