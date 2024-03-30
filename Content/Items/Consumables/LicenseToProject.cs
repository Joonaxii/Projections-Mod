using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Content.NPCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Projections.Core.Utilities;

namespace Projections.Content.Items.Consumables
{
    public class LicenseToProject : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<BookOfProjections>();
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 1;
            Item.value = 25000;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            return !(player.PPlayer()?.CanProject ?? false);
        }

        public override bool? UseItem(Player player)
        {
            var pPlayer = player.PPlayer();
            if (pPlayer == null)
            {
                return false;
            }

            var traderI = NPC.FindFirstNPC(ModContent.NPCType<ProjectionTrader>());
            SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Projections/Content/Audio/NPC/tim"), player.position);

            if (pPlayer.CanProject)
            {
                pPlayer.RevokeProjecting();
                Main.NewText("You've revoked your ability to project!", 196, 32, 32);
            }
            else
            {
                pPlayer.UnlockProjecting();
                Main.NewText("You've just achieved the ability to project!", 32, 196, 196);         
            }
            return true;
        }

    }
}
