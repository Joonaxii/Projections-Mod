using Microsoft.CodeAnalysis.CSharp.Syntax;
using Projections.Common.Configs;
using Projections.Common.Items;
using Projections.Common.Netcode;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Content.Items;
using Projections.Content.Items.Consumables;
using Projections.Content.Items.Projectors;
using Projections.Content.NPCs;
using Projections.Core.Collections;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using static Projections.Common.Players.ProjectionsPlayer;

namespace Projections.Common.Players
{
    public class ProjectionsPlayer : ModPlayer
    {
        public const int PLAYER_PROJECTORS = 5;

        public bool CanProject => _flags.HasFlag(PPlayerFlags.CanProject);

        public bool HasBookOfProjections
        {
            get
            {
                int id = ModContent.ItemType<BookOfProjections>();
                return
                    Player.inventory.HasItem(id) ||
                    Player.bank.HasItem(id) ||
                    Player.bank2.HasItem(id) ||
                    Player.bank3.HasItem(id) ||
                    Player.bank4.HasItem(id);
            }
        }

        public int ProjectorCount => _projectors.Length;

        public HashSet<ProjectionIndex> CollectedProjections => _seenProjections;
        public TimeSpan TimeProjected => TimeSpan.FromSeconds(_timeSpentProjecting);
        public ref double TimeProjectedSec => ref _timeSpentProjecting;

        private PPlayerFlags _flags;
        private HashSet<ProjectionIndex> _seenProjections = new HashSet<ProjectionIndex>();

        private Projector[] _projectors;
        private PProjectorFlags[] _projectorFlags;

        private ProjectionIndex[] _tProjPool = new ProjectionIndex[10];
        private ProjectionIndex[] _tMatPool = new ProjectionIndex[10];
        private ProjectionIndex[] _tBunPool = new ProjectionIndex[10];

        private RefList<ProjectionIndex> _indexQueue = new RefList<ProjectionIndex>();
        private bool _isDay = false;
        private bool _shoudRegenPool = false;
        private double _timeSpentProjecting;

        public ProjectionsPlayer()
        {
            _projectors = new Projector[PLAYER_PROJECTORS];
            _projectorFlags = new PProjectorFlags[PLAYER_PROJECTORS];
            for (int i = 0; i < _projectorFlags.Length; i++)
            {
                _projectorFlags[i] = PProjectorFlags.IsVisible;
            }
        }

        public bool HasSeenProjection(ReadOnlySpan<char> name)
         => HasSeenProjection(name.ParseProjection());

        public static int MoveToProjector(Projector projector, Item item, bool activeSlotOnly = false, bool allowEmpty = false)
        {
            if (!item.IsAir && item.stack > 0 && item.ModItem is ProjectionItem pItem && !pItem.IsEmpty)
            {
                if (activeSlotOnly)
                {
                    return projector.TryPushToSlot(pItem, projector.ActiveSlotIndex, allowEmpty);
                }
                return projector.TryPushProjection(pItem, allowEmpty);
            }
            return 0;
        }

        public bool HasProjector(Projector projector)
        {
            if (projector == null) { return false; }
            for (int i = 0; i < _projectors.Length; i++)
            {
                if (_projectors[i] == projector) { return true; }
            }
            return false;
        }

        public override bool OnPickup(Item item)
        {
            MarkFromItem(item);

            if (CanProject)
            {
                int result = 0;
                if (CanProject)
                {
                    for (int i = 0; i < _projectors.Length; i++)
                    {
                        if (_projectors[i] != null)
                        {
                            int res = MoveToProjector(_projectors[i], item, true);
                            if (res != 0)
                            {
                                result = res;
                                if (result == 2)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (result != 0)
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    return result != 2;
                }
            }
            return base.OnPickup(item);
        }
        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            if (Main.npcShop > 0 ||
                Player.chest < -1 && context != ItemSlot.Context.BankItem ||
                Player.chest >= 0 && context != ItemSlot.Context.InventoryItem)
            {
                return base.ShiftClickSlot(inventory, context, slot);
            }

            switch (context)
            {
                case ItemSlot.Context.BankItem:
                case ItemSlot.Context.ChestItem:
                case ItemSlot.Context.InventoryItem:
                    bool audioPlayed = false;
                    int result = 0;
                    for (int i = 0; i < _projectors.Length; i++)
                    {
                        var proj = _projectors[i];
                        if (proj != null)
                        {
                            int res = MoveToProjector(proj, inventory[slot], true);
                            result = res > 0 ? res : result;
                            if (res == 2)
                            {
                                break;
                            }
                        }
                    }
                    if (result > 0)
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                        audioPlayed = true;
                    }

                    if (result == 2)
                    {
                        return true;
                    }

                    result = 0;
                    var openProj = UISystem.Instance.CurrentProjector;
                    if (!HasProjector(openProj))
                    {
                        result = MoveToProjector(openProj, inventory[slot], true, true);
                        if (result > 0 && !audioPlayed)
                        {
                            SoundEngine.PlaySound(SoundID.Grab);
                        }
                    }
                    return result == 2 || base.ShiftClickSlot(inventory, context, slot);
            }
            return base.ShiftClickSlot(inventory, context, slot);
        }

        public bool HasSeenProjection(ProjectionIndex index)
        {
            if (!index.IsValidID()) { return false; }
            return _seenProjections.Contains(index);
        }
        public bool MarkAsSeen(ProjectionIndex index)
        {
            if (index.IsValidID())
            {
                if (_seenProjections.Add(index))
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer == Player.whoAmI)
                    {
                        _indexQueue.Add(index);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool TryGetProjector(int index, out Projector proj)
        {
            if (index < 0 || index >= ProjectorCount)
            {
                proj = null;
                return false;
            }
            proj = _projectors[index];
            return proj != null;
        }
        public bool IsProjectorVisible(int index) => index >= 0 && index < _projectorFlags.Length && (_projectorFlags[index] & PProjectorFlags.IsVisible) != 0;
        public void SetProjectorVisible(int index, bool isVisible)
        {
            if(index >= 0 && index < _projectorFlags.Length)
            {
                _projectorFlags[index] = (isVisible ? (_projectorFlags[index] | PProjectorFlags.IsVisible) : (_projectorFlags[index] & ~PProjectorFlags.IsVisible));

                if(_projectors[index] == null) { return; }
                _projectors[index].Clear();
            }
        }

        public bool MoveToEmptySlotInventory(Item item)
        {
            if(item == null || item.IsAir) { return false; }
            for (int i = 0; i < 50; i++)
            {
                var itm = Player.inventory[i];
                if(itm == null || itm.IsAir)
                {
                    int toMove = Math.Min(itm.maxStack - itm.stack, item.stack);
                    Player.inventory[i] = item;
                    if(Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncEquipment, -1, Player.whoAmI, number: Player.whoAmI, number2: i);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool UnlockProjecting()
        {
            if (CanProject) { return false; }
            _flags |= PPlayerFlags.CanProject;
            return true;
        }
        public bool RevokeProjecting()
        {
            if (!CanProject) { return false; }
            _flags &= ~PPlayerFlags.CanProject;
            return true;
        }

        public bool TryFindProjectorItem(int index, out PlayerProjectorItem item, out int iSlot)
        {
            iSlot = 0;
            item = null;
            var projector = index < 0 || index >= _projectors.Length ? null : _projectors[index];
            if(projector == null) { return false; }

            if(FindProjectorItem(Player.inventory, projector, out item, out iSlot)) 
            {
                return true; 
            }

            if(IsProjectorItem(Player.trashItem, projector)) 
            {
                iSlot = 179;
                item = Player.trashItem.ModItem as PlayerProjectorItem; 
                return true; }

            if(IsProjectorItem(Main.mouseItem, projector))
            {
                iSlot = -1;
                item = Main.mouseItem.ModItem as PlayerProjectorItem; 
                return true; 
            }
            if(Player.useVoidBag() && FindProjectorItem(Player.bank4.item, projector, out item, out iSlot))
            {
                iSlot += 220;
                return true; 
            }
            return false;
        }

        private static bool FindProjectorItem(IList<Item> items, Projector projector, out PlayerProjectorItem item, out int ind)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (IsProjectorItem(items[i], projector))
                {
                    ind = i;
                    item = items[i].ModItem as PlayerProjectorItem; 
                    return true; 
                }
            }
            item = null;
            ind = -1;
            return false;
        }

        private static bool IsProjectorItem(Item item, Projector projector)
        {
            return item?.ModItem is PlayerProjectorItem plr && plr.Projector == projector;
        }

        public void CheckInventory()
        {
            if (Main.netMode == NetmodeID.Server) { return; }
            MarkFromItems(Player.inventory);
            MarkFromItems(Player.bank.item);
            MarkFromItems(Player.bank2.item);
            MarkFromItems(Player.bank3.item);
            MarkFromItems(Player.bank4.item);
        }

        private void MarkFromItems(IList<Item> items)
        {
            foreach (var item in items)
            {
                MarkFromItem(item);
            }
        }

        internal void ReceiveMessage(BinaryReader reader, MessageType type)
        {
            switch (type)
            {
                case MessageType.PlayerProjectorFlags:
                    {
                        int index = reader.ReadInt32();
                        if(index < 0)
                        {
                            index = ~index;
                            var span = MemoryMarshal.AsBytes(_projectorFlags.AsSpan());
                            int len = Math.Min(index, span.Length);
                            reader.Read(span.Slice(0, len));
                            for (int i = len; i < span.Length; i++)
                            {
                                _projectorFlags[i] = PProjectorFlags.IsVisible;
                            }
                        }
                        else if(index < _projectorFlags.Length)
                        {
                            _projectorFlags[index] = reader.Read<PProjectorFlags>();
                        }
                        break;
                    }
                case MessageType.PlayerTime:
                    _timeSpentProjecting = reader.ReadDouble();
                    break;
                case MessageType.PlayerObtainedProjection:
                    {
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            MarkAsSeen(reader.Read<ProjectionIndex>());
                        }
                        break;
                    }
            }
        }

        public void SendProjectorFlags(int index)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) { return; }

            var packet = Mod.GetPacket(48);
            packet.Write(MessageType.PlayerTime);
            packet.Write((byte)Player.whoAmI);

            if(index < 0)
            {
                packet.Write(~_projectorFlags.Length);
                packet.Write(MemoryMarshal.AsBytes(_projectorFlags.AsSpan()));
            }
            else
            {
                packet.Write(index);
                packet.Write(index < _projectorFlags.Length ? _projectorFlags[index] : PProjectorFlags.None);
            }
            packet.Send(-1, Player.whoAmI);
        }

        public void SendTime()
        {
            if(Main.netMode != NetmodeID.MultiplayerClient) { return; }

            var packet = Mod.GetPacket(64);
            packet.Write(MessageType.PlayerTime);
            packet.Write((byte)Player.whoAmI);
            packet.Write(_timeSpentProjecting);
            packet.Send(-1, Player.whoAmI);
        }

        private const int MAX_QUEUE_PER_PACKET = 128;
        public void SendObtainQueue()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || _indexQueue.Count < 1) { return; }
            int count = 0;

            var span = _indexQueue.Span;
            while (count < _indexQueue.Count)
            {
                int toSend = Math.Min(_indexQueue.Count - count, MAX_QUEUE_PER_PACKET);
                SendObtainQueue(span.Slice(count, toSend));
                count += toSend;
            }
            _indexQueue.Clear();
        }

        private void SendObtainQueue(Span<ProjectionIndex> queue)
        {
            var packet = Mod.GetPacket((MAX_QUEUE_PER_PACKET * 8) + 4);
            packet.Write(MessageType.PlayerObtainedProjection);
            packet.Write((byte)Player.whoAmI);
            packet.Write(queue.Length);
            for (int i = 0; i < queue.Length; i++)
            {
                packet.Write(queue[i]);
            }
            packet.Send(-1, Player.whoAmI);
        }

        private void MarkFromItem(Item item)
        {
            if (item == null) { return; }
            if (item.ModItem is ProjectionItem pItem)
            {
                MarkAsSeen(pItem.Index);
            }
            else if (item.ModItem is PlayerProjectorItem prItem)
            {
                prItem.IterateSlots((int index, in ProjectorSlot slot, bool isActive) =>
                {
                    MarkAsSeen(slot.Index);
                    return 0;
                }, out _);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag.AddEnum("P-Flags", _flags);
            tag.Assign("TimeProjected", _timeSpentProjecting);

            List<TagCompound> pRojs = new List<TagCompound>();
            foreach (var ob in _seenProjections)
            {
                pRojs.Add(NBTExtensions.New(ob));
            }
            tag.Assign("ObtainedProjections", pRojs);

            List<TagCompound> projectors = new List<TagCompound>();
            for (int i = 0; i < _projectors.Length; i++)
            {
                TagCompound tagPI = new TagCompound();
                tagPI.AddEnum("P-Slot-Flags", _projectorFlags[i]);
                if (_projectors[i] != null)
                {
                    _projectors[i].Data.Save(tagPI);
                    TagCompound tagP = new TagCompound();
                    _projectors[i].Save(tagP);
                    tagPI.Assign("Data", tagP);
                }
                else
                {
                    ProjectorData.NewNull().Save(tagPI);
                }
                projectors.Add(tagPI);
            }
            tag.Set("Projectors", projectors, true);
        }
        public override void LoadData(TagCompound tag)
        {
            _flags = tag.GetEnum<PPlayerFlags>("P-Flags");
            _timeSpentProjecting = tag.GetSafe("TimeProjected", 0.0d);

            _seenProjections.Clear();
            if (tag.TryGetSafe<IList<TagCompound>>("ObtainedProjections", out var obtained))
            {
                for (int i = 0; i < obtained.Count; i++)
                {
                    MarkAsSeen(obtained[i].GetPIndex());
                }
            }

            if (tag.TryGetSafe<IList<TagCompound>>("Projectors", out var projectors))
            {
                ProjectorData data = default;
                int len = Math.Min(projectors.Count, _projectors.Length);
                for (int i = 0; i < _projectors.Length; i++)
                {
                    _projectors[i]?.Deactivate();
                    if(i < len)
                    {
                        _projectorFlags[i] = projectors[i].GetSafe<PProjectorFlags>("P-Slot-Flags");
                    }
                    else
                    {
                        _projectorFlags[i] = PProjectorFlags.IsVisible;
                    }

                    if (i < len && data.Load(projectors[i]) && data.Type < ProjectorType.__Count)
                    {
                        _projectorFlags[i] = projectors[i].GetSafe<PProjectorFlags>("P-Slot-Flags");
                        _projectors[i] = ProjectorSystem.GetNewProjector(in data);
                        if (_projectors[i] != null && projectors[i].TryGetSafe<TagCompound>("Data", out var projT))
                        {
                            _projectors[i].Load(projT);
                        }
                    }
                    else
                    {
                        _projectors[i] = null;
                    }
                }
            }
            CheckInventory();
        }

        public override void OnEnterWorld()
        {
            base.OnEnterWorld();
            for (int i = 0; i < _projectors.Length; i++)
            {
                if(_projectors[i] != null)
                {
                    _projectors[i].OwningPlayer = Player.whoAmI;
                }
            }
        }

        internal void UpdateProjectors(Action<Projector> onUpdate)
        {
            bool isPlaying = false;
            for (int i = 0; i < _projectors.Length; i++)
            {
                var proj = _projectors[i];
                ref var projI = ref _projectorFlags[i];
                if (proj != null && (projI & PProjectorFlags.IsVisible) != 0)
                {
                    onUpdate.Invoke(proj);
                    isPlaying |= proj.IsPlaying;
                }
            }
            if (isPlaying)
            {
                _timeSpentProjecting += ProjectorSystem.Instance?.FrameDelta ?? 0.0f;
            }
        }

        internal void GetItems(List<ItemRef> stacks)
        {
            int curChest = Player.chest;
            for (int i = 10; i < 50; i++)
            {
                var item = Player.inventory[i];
                if (!item.IsAir && item.stack > 0)
                {
                    stacks.Add(new ItemRef((byte)Player.whoAmI, (byte)i, -1));
                }
            }

            Chest chest = null;
            switch (curChest)
            {
                default:
                    if (curChest >= 0)
                    {
                        chest = Main.chest[curChest];
                    }
                    break;
                case -2:
                    chest = Player.bank;
                    break;
                case -3:
                    chest = Player.bank2;
                    break;
                case -4:
                    chest = Player.bank3;
                    break;
                case -5:
                    chest = Player.bank4;
                    break;
            }

            if (chest != null)
            {
                for (int i = 0; i < chest.item.Length; i++)
                {
                    var item = chest.item[i];
                    if (!item.IsAir && item.stack > 0)
                    {
                        stacks.Add(new ItemRef((byte)Player.whoAmI, (byte)i, curChest));
                    }
                }
            }

            if (Player.useVoidBag() && curChest != -5)
            {
                for (int i = 0; i < Player.bank4.item.Length; i++)
                {
                    var item = Player.bank4.item[i];
                    if (!item.IsAir && item.stack > 0)
                    {
                        stacks.Add(new ItemRef((byte)Player.whoAmI, (byte)i, -5));
                    }
                }
            }
        }

        internal bool CheckRecipe(PRecipe recipe, List<ItemRef> items, Span<int> current)
        {
            if (!recipe.IsValid || items.Count < 1)
            {
                return false;
            }
            return recipe.HasAllItems(items, current);
        }
        internal bool ConsumeRecipe(PRecipe recipe, List<ItemRef> items, Span<int> current)
        {
            if (!recipe.IsValid || items.Count < 1)
            {
                return false;
            }
            return recipe.ConsumeAllItems(items, current);
        }

        public override void AnglerQuestReward(float rareMultiplier, List<Item> rewardItems)
        {
            ItemDropManager.DoDrops(PoolType.FishingQuest, default, rewardItems, ProjectionSource.Drop, rareMultiplier, 0, true);
        }

        public void ClearTraderPools()
        {
            for (int i = 0; i < 10; i++)
            {
                _tProjPool[i] = ProjectionIndex.Zero;
                _tMatPool[i] = ProjectionIndex.Zero;
            }
        }

        internal void FetchPools(
            Span<ProjectionIndex> projections, Span<ProjectionIndex> materials,
            out Span<ProjectionIndex> poolP, out Span<ProjectionIndex> poolM)
        {
            ProjectionUtils.GetTraderPoolSizes(out int pCount, out int mCount);
            pCount = Math.Min(pCount, _tProjPool.Length);
            mCount = Math.Min(mCount, _tMatPool.Length);

            poolP = projections.Slice(0, Math.Min(pCount, projections.Length));
            poolM = materials.Slice(0, Math.Min(mCount, materials.Length));

            for (int i = 0; i < poolP.Length; i++)
            {
                poolP[i] = _tProjPool[i];
            }

            for (int i = 0; i < poolM.Length; i++)
            {
                poolM[i] = _tMatPool[i];
            }
        }

        public bool GenerateTraderPools()
        {
            ClearTraderPools();
            bool didSomething = ItemDropManager.DoDrops(PoolType.Trader, default, (_, index, type, stackSize, client) =>
            {
                int mI = 0;
                int pI = 0;
                int bI = 0;

                if (!index.IsValidID()) { return 0; }

                switch (type)
                {
                    case PType.TProjection:
                        if (pI < 10)
                        {
                            _tProjPool[pI++] = index;
                        }
                        break;
                    case PType.TPMaterial:
                        if (mI < 10)
                        {
                            _tProjPool[mI++] = index;
                        }
                        break;
                    case PType.TPBundle:
                        if (bI < 10)
                        {
                            _tProjPool[bI++] = index;
                        }
                        break;
                }

                return mI + pI+ bI;
            },
            default, 0.0f, 1.1f, 10, 10, 10, true);
            _shoudRegenPool = false;
            return didSomething;
        }

        private int _packetTimer = 0;
        public override void PostUpdateMiscEffects()
        {
            if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
            {
                if (Main.dayTime != _isDay)
                {
                    _isDay = Main.dayTime;
                    _shoudRegenPool |= _isDay;
                }

                var trader = NPC.FindFirstNPC(ModContent.NPCType<ProjectionTrader>());
                if (_shoudRegenPool && trader > -1)
                {
                    if (GenerateTraderPools() && (ProjectionsClientConfig.Instance?.ShowTraderWareRefreshMessage ?? true))
                    {
                        Main.NewText($"The Projection Trader '{Main.npc[trader].FullName}' has refreshed their wares!", Colors.RarityBlue);
                    }
                }

                if (++_packetTimer >= 30)
                {
                    _packetTimer = 0;
                    SendTime();
                    SendObtainQueue();
                }
            }
        }

        public void ValidateProjectors()
        {
            for (int i = 0; i < _projectors.Length; i++)
            {
                _projectors[i]?.Validate();
            }
        }

        internal void SetProjector(Projector projector, int index)
        {
            if(projector != _projectors[index])
            {
                _projectors[index]?.Deactivate();
                _projectors[index] = projector;
            }
        }

        internal void BuildIgnoreList(HashSet<ProjectionIndex> ignoreList, PType type)
        {
            Player.inventory.BuildIgnoreList(ignoreList, type, true);
            if (ProjectionsServerConfig.Instance?.BuildIgnoreListFromOpenVoidBag ?? false && Player.useVoidBag())
            {
                Player.bank4.item.BuildIgnoreList(ignoreList, type, false);
            }
        }

        internal bool TryPushPlayerProjector(PlayerProjectorItem proj)
        {
            if (!CanProject) { return false; }
            for (int i = 0; i < _projectors.Length; i++)
            {
                if (_projectors[i] == null)
                {
                    if(proj.Unpack(Player.whoAmI, i, ref _projectors[i]))
                    {
                        ProjectionNetUtils.SendCreateProjector(_projectors[i], Player.whoAmI);
                        return true;
                    }
                }
            }
            return false;
        }

        public enum PProjectorFlags : byte
        {
            None = 0x00,
            IsVisible = 0x01,
        }
    }
}
