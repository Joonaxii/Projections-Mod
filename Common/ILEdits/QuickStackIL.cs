using MonoMod.Cil;
using System.Collections.Generic;
using Terraria;
using Mono.Cecil.Cil;
using Projections.Content.Items;
using Projections.Common.Netcode;
using Projections.Core.Utilities;
using Projections.Common.ProjectorTypes;
using Terraria.UI;
using Projections.Content.Items.Projectors;

namespace Projections.Common.ILEdits
{
    internal class QuickStackIL : ILEdit
    {
        public void Init()
        {
            IL_ItemSlot.SelectEquipPage += PatchSelectEquipPage;
            IL_Player.QuickStackAllChests += PatchQuickStack;
            IL_Chest.ServerPlaceItem += PatchServerPlaceItem;
        }

        public void Deinit()
        {
            IL_ItemSlot.SelectEquipPage -= PatchSelectEquipPage;
            IL_Player.QuickStackAllChests -= PatchQuickStack;
            IL_Chest.ServerPlaceItem -= PatchServerPlaceItem;
        }

        private static void PatchSelectEquipPage(ILContext ctx)
        {
            ILCursor cursor = new ILCursor(ctx);
            var label = cursor.DefineLabel();
            cursor.EmitLdarg0();
            cursor.EmitDelegate(SetToEquipPageIfPlrProjector);
            cursor.Emit(OpCodes.Brfalse_S, label);
            cursor.EmitRet();
            cursor.MarkLabel(label);
        }

        private static void PatchQuickStack(ILContext ctx)
        {
            ILCursor cursor = new ILCursor(ctx);
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchRet()))
            {
                cursor.Index += 4;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(TryProjectorQuickStack);
            }
        }

        private static bool SetToEquipPageIfPlrProjector(Item item)
        {
            if(item?.IsAir ?? true) { return false; }

            if(item.ModItem is PlayerProjectorItem)
            {
                Main.EquipPage = 2;
                return true;
            }
            return false;
        }

        private static void PatchServerPlaceItem(ILContext ctx)
        {
            ILCursor cursor = new ILCursor(ctx);
            while (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdelemRef(), i => i.MatchLdsfld(CommonIL.Main_player)))
            {
                cursor.Index--;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ServerPlaceItemProjectorCheck);
            }
        }

        private static void TryProjectorQuickStack(Player player)
        {
            List<Projector> projectors = new List<Projector>();
            if (player.Center.GetNearByProjectors(projectors, requireActive: false))
            {
                for (int i = 10; i < 50; i++)
                {
                    Item item = player.inventory[i];

                    if (!item.IsAir &&
                        !item.favorited &&
                        item.ModItem is ProjectionItem pItm)
                    {
                        ProjectionNetUtils.QuickStackProjections(player.Center, projectors, pItm);
                    }
                }

                if (player.useVoidBag())
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Item item = player.bank4.item[i];
                        if (!item.IsAir &&
                            !item.favorited &&
                            item.ModItem is ProjectionItem pItm)
                        {
                            ProjectionNetUtils.QuickStackProjections(player.Center, projectors, pItm);
                        }
                    }
                }
            }
        }

        private static Item ServerPlaceItemProjectorCheck(Item item, byte plr)
        {
            Player player = Main.player[plr];
            List<Projector> projectors = new List<Projector>();
            if (item.ModItem is ProjectionItem pItem && player.Center.GetNearByProjectors(projectors, requireActive: false))
            {
                ProjectionNetUtils.QuickStackProjections(player.Center, projectors, pItem);
            }
            return item;
        }

    }
}