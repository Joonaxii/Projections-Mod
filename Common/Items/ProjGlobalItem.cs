using Projections.Content.Items;
using Projections.Content.Items.Projectors;
using Projections.Core.Data;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Common.Items
{
    public class ProjGlobalItem : GlobalItem
    {
        public static TooltipLine PMatUsageTip
        {
            get => _matUsageTip ??= new TooltipLine(Projections.Instance, "PMaterial", "Used in P-Recipe");
        }
        private static TooltipLine _matUsageTip;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var proj = item.ModItem as IProjectionItemBase;
            if (proj != null && Projections.IsUsedInRecipe(proj.Index, proj.PType) || proj == null && Projections.IsUsedInRecipe(item.type))
            {
                tooltips.Add(PMatUsageTip);
            }
        }

        public override void RightClick(Item item, Player player)
        {
            ItemDropManager.DoDrops(PoolType.Treasure, new DropIndex(item.netID, item.type), null, player.position, 1, 0, null, null, null, true, forClient: player.whoAmI);
        }
    }
}
