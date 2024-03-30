using Terraria;
using Terraria.ID;

namespace Projections.Common.Items
{
    public struct ItemRef
    {
        public Item Item
        {
            get
            {
                if (inventory >= 0)
                {
                    return Main.chest[inventory].item[index];
                }

                if (player >= Main.maxPlayers)
                {
                    return null;
                }

                Player plr = Main.player[player];
                switch (inventory)
                {
                    default: return null;
                    case -1:
                        return plr.inventory[index];
                    case -2:
                        return plr.bank.item[index];
                    case -3:
                        return plr.bank2.item[index];
                    case -4:
                        return plr.bank3.item[index];
                    case -5:
                        return plr.bank4.item[index];
                }
            }
        }

        public byte player;
        public byte index;
        public int inventory;

        public ItemRef(byte player, byte index, int inventory)
        {
            this.player = player;
            this.index = index;
            this.inventory = inventory;
        }

        public ItemRef UpdateStack(int newStack)
        {
            var item = Item;
            if (item == null || newStack == item.stack) { return this; }

            item.stack = newStack;
            if (newStack <= 0)
            {
                item.TurnToAir();
            }

            if(Main.netMode != NetmodeID.SinglePlayer)
            {
                if (inventory >= 0)
                {
                    NetMessage.SendData(MessageID.SyncChestItem, ignoreClient: player, number: inventory, number2: index, number3: 0);
                }
                else
                {
                    int start = 0;
                    switch (inventory)
                    {
                        case -2:
                            start = 99;
                            break;
                        case -3:
                            start = 139;
                            break;
                        case -4:
                            start = 180;
                            break;
                        case -5:
                            start = 220;
                            break;
                    }
                    NetMessage.SendData(MessageID.SyncEquipment, ignoreClient: player, number: player, number2: start + index);
                }
            }       
            return this;
        }
    }
}