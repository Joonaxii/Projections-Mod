using Humanizer.Bytes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Content.Items;
using Projections.Content.Items.Projectors;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Common.Netcode
{
    public static class ProjectionNetUtils
    {
        internal static int TargetClient() => Main.netMode == NetmodeID.Server ? -1 : Main.maxPlayers;

        public static void SpawnItem<T>(int player, int stackSize = 1, Action<Item> doAction = null) where T : ModItem
        {
            SpawnItem(ModContent.ItemType<T>(), player, stackSize, doAction);
        }
        public static void SpawnItem<T>(Vector2 position, int stackSize = 1, Action<Item> doAction = null) where T : ModItem
        {
            SpawnItem(ModContent.ItemType<T>(), position, stackSize, doAction);
        }

        public static int SpawnItem(int id, int player, int stackSize = 1, Action<Item> doAction = null)
        {
            Player plr = Main.player[player];
            return SpawnItem(id, plr.position, stackSize, doAction);
        }
        public static int SpawnItem(int id, Vector2 position, int stackSize = 1, Action<Item> doAction = null)
        {
            int itmIndex = Item.NewItem(Entity.GetSource_NaturalSpawn(), position, id, stackSize, noBroadcast: true);
            ref var item = ref Main.item[itmIndex];
            item.stack = stackSize;

            doAction?.Invoke(item);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itmIndex, 1f);
            }
            return itmIndex;
        }

        public static int IndexOfItem(this Item item)
        {
            for (int i = 0; i < Main.maxItems; i++)
            {
                if (Main.item[i] == item) { return i; }
            }
            return -1;
        }

        public static void SpawnProjectionItem<T>(ProjectionIndex index, int player, int stackSize = 1, Action<Item> doAction = null, int timeOut = 0, bool onlyForPlayer = false) where T : ModItem, IProjectionItemBase
        {
            Player plr = Main.player[player];
            SpawnProjectionItem(ModContent.ItemType<T>(), index, plr.Center, stackSize, doAction, timeOut, onlyForPlayer ? player : -1);
        }
        public static void SpawnProjectionItem<T>(ProjectionIndex index, Vector2 position, int stackSize = 1, Action<Item> doAction = null, int timeOut = 0, int client = -1) where T : ModItem, IProjectionItemBase
        {
            SpawnProjectionItem(ModContent.ItemType<T>(), index, position, stackSize, doAction, timeOut, client);
        }

        public static int SpawnProjectionItem(int type, ProjectionIndex index, Vector2 position, int stackSize = 1, Action<Item> doAction = null, int timeOut = 0, int client = -1)
        {
            if (type <= 0 || type >= ItemLoader.ItemCount || stackSize < 0) { return -1; }

            int itmIndex = Item.NewItem(Entity.GetSource_NaturalSpawn(), position, type);
            ref var item = ref Main.item[itmIndex];
            Main.timeItemSlotCannotBeReusedFor[itmIndex] = Main.netMode != NetmodeID.Server ? 0 : Math.Max(timeOut, 0);
            item.stack = stackSize;
            if (item.ModItem is IProjectionItemBase prj)
            {
                prj.SetIndex(index);
            }
            doAction?.Invoke(item);

            if (Main.netMode == NetmodeID.Server)
            {
                if (timeOut > 0)
                {
                    if (client < 0 || client >= Main.maxPlayers)
                    {
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active)
                            {
                                NetMessage.SendData(MessageID.InstancedItem, client, -1, null, itmIndex);
                            }
                        }
                    }
                    else
                    {
                        if (Main.player[client].active)
                        {
                            NetMessage.SendData(MessageID.InstancedItem, client, -1, null, itmIndex);
                        }
                    }
                    item.active = false;
                }
                else
                {
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itmIndex, 1f);
                }
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, client, -1, null, itmIndex, 1f);
            }
            return itmIndex;
        }

        public static bool IsHost(this Player player)
        {
            switch (Main.netMode)
            {
                default: return false;
                case NetmodeID.SinglePlayer: 
                    return true;
                case NetmodeID.MultiplayerClient: 
                    return Netplay.Connection.Socket.GetRemoteAddress().IsLocalHost();
                case NetmodeID.Server:
                    {
                        var client = Netplay.Clients[player.whoAmI];
                        return client.IsConnected() && client.Socket.GetRemoteAddress().IsLocalHost();
                    }
            }
        }

        public static void SendCreateProjector(Projector projector, int sender)
        {
            if(projector == null) { return; }
            SendCreateProjector(in projector.Data, sender);
        }
        public static void SendCreateProjector(in ProjectorData data, int sender)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }
            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.CreateProjector);
            packet.Write((byte)sender);
            data.Serialize(packet);
            packet.Send(TargetClient(), sender);
        }
        public static void SendProjectorUpdate(this Projector projector, SerializeType type, int sender)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }
            if (projector == null) { return; }

            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.UpdateProjector);
            packet.Write((byte)sender);
            projector.Data.id.Serialize(packet);
            projector.Serialize(packet, type);
            packet.Send(TargetClient(), sender);
        }
        public static void SendKillProjector(Projector projector, bool eject, int sender, int? target = null)
        {
            if(projector == null) { return; }
            SendKillProjector(in projector.Data.id, eject, sender, target);
        }
        public static void SendKillProjector(in ProjectorID id, bool eject, int sender, int? target = null)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }
            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.KillProjector);
            packet.Write((byte)sender);
            packet.Write(eject);
            id.Serialize(packet);
            packet.Send(target != null ? target.Value : TargetClient(), sender);
        }

        public static void SendCustomProjectorUpdate(uint oldId, uint newId, int target)
        {
            if (Main.netMode != NetmodeID.Server) { return; }
            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.ClientUpdateCustom);
            packet.Write((byte)target);
            packet.Write(oldId);
            packet.Write(newId);
            packet.Send(target);
        }

            public static void SendProjectorSlotUpdate(this Projector projector, int slotIndex, int sender)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }
            if (projector == null || slotIndex >= projector.SlotCount) { return; }
            slotIndex = slotIndex < 0 ? ~projector.SlotCount : slotIndex;

            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.UpdateProjector);
            packet.Write((byte)sender);
            projector.Data.id.Serialize(packet);
            packet.Write(slotIndex);

            if (slotIndex < 0)
            {
                for (int i = 0; i < projector.SlotCount; i++)
                {
                    ref var slot = ref projector.GetSlot(i);
                    slot.Serialize(packet);
                }
            }
            else
            {
                ref var slot = ref projector.GetSlot(slotIndex);
                slot.Serialize(packet);
            }
            packet.Send(TargetClient(), -1);
        }

        public static void ReadProjectorSlotUpdate(BinaryReader br, Projector projector)
        {
            if (projector == null) { return; }

            int slotVal = br.ReadInt32();
            if (slotVal < 0)
            {
                slotVal = Math.Min(~slotVal, projector.SlotCount);
                for (int i = 0; i < slotVal; i++)
                {
                    ref var slot = ref projector.GetSlot(i);
                    slot.Deserialize(br);
                }
            }
            else if (slotVal < projector.SlotCount)
            {
                ref var slot = ref projector.GetSlot(slotVal);
                slot.Deserialize(br);
            }
            // TODO: Possibly Call some sort of refresh method in Projector
        }

        public static void SendEraseProjectors(int sender, bool eject)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }
            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write(MessageType.EraseAllProjectors);
            packet.Write((byte)sender);
            packet.Write(eject);
            packet.Send(-1, sender);
        }

        public static bool QuickStackProjections(Vector2 position, IList<Projector> projectors, ProjectionItem item)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || item == null || projectors.Count < 1)
            {
                return false;
            }

            int projItemType = ModContent.ItemType<ProjectionItem>();
            foreach (Projector proj in projectors)
            {
                int size = item.Item.stack;
                int pushResult = proj.TryPushProjection(item);
                if (pushResult != 0)
                {
                    Chest.VisualizeChestTransfer(position, proj.ProjectorCenter, ContentSamples.ItemsByType[projItemType], size - item.Item.stack);
                    if (pushResult == 2) { return true; }
                }
            }
            return false;
        }
        public static void SendResetDropInfo(int client)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) { return; }

            ModPacket packet = Projections.Instance.GetPacket();
            packet.Write((byte)MessageType.ClientResetDropInfo);
            packet.Write((byte)client);
            packet.Send(TargetClient(), client);
        }

        internal static int ReadDropInfoChunk(BinaryReader br, Span<DropSource> drops)
        {
            int total = 0;
            int position = br.ReadInt32();
            int count = br.ReadInt32();
            for (int i = 0, j = position; i < count; i++, j++)
            {
                drops[j].ReadPacket(br);
            }
            total += count;

            position = br.ReadInt32();
            count = br.ReadInt32();
            for (int i = 0, j = position; i < count; i++, j++)
            {
                drops[j].ReadPacket(br);
            }
            total += count;

            position = br.ReadInt32();
            count = br.ReadInt32();
            for (int i = 0, j = position; i < count; i++, j++)
            {
                drops[j].ReadPacket(br);
            }
            total += count;
            return total;
        }

        internal const int MAX_DROPS_PER_PACKET = 100;
        internal const int DROP_ITEM_SIZE = DropSource.DROP_SOURCE_SIZE;
        internal const int DROP_INFO_CHUNK_SIZE = MAX_DROPS_PER_PACKET * DROP_ITEM_SIZE;

        private static bool PreCalcDropInfoHeader(
            Span<ushort> lensProj, Span<ushort> lensMat, Span<ushort> lensBun, out int totalProj, out int totalMat, out int totalBun, out int validProj, out int validMat, out int validBun,
            IList<Projection> projections, IList<PMaterial> materials, IList<PBundle> bundles)
        {
            totalProj = 0;
            validProj = 0;
            totalMat = 0;
            totalBun = 0;
            validMat = 0;
            validBun = 0;

            for (int i = 0; i < projections.Count; i++)
            {
                var mat = projections[i].Material;
                if (mat.Sources.Length < 1)
                {
                    lensProj[i] = 0;
                    continue;
                }
                lensProj[i] = (ushort)Math.Min(mat.Sources.Length, ushort.MaxValue);
                totalProj += lensProj[i];
                validProj++;
            }

            for (int i = 0; i < materials.Count; i++)
            {
                var mat = materials[i];
                if (mat.Sources.Length < 1)
                {
                    lensMat[i] = 0;
                    continue;
                }
                lensMat[i] = (ushort)Math.Min(mat.Sources.Length, ushort.MaxValue);
                totalMat += lensMat[i];
                validMat++;
            }

            for (int i = 0; i < bundles.Count; i++)
            {
                var bundle = bundles[i].Material;
                if (bundle.Sources.Length < 1)
                {
                    lensBun[i] = 0;
                    continue;
                }
                lensBun[i] = (ushort)Math.Min(bundle.Sources.Length, ushort.MaxValue);
                totalBun += lensBun[i];
                validBun++;
            }
            return totalMat > 0 || totalProj > 0 || totalBun > 0;
        }

        internal static void WriteDrops(BinaryWriter bw, int start, Span<DropSource> clients)
        {
            int value = clients.Length;
            bw.Write(start);
            bw.Write(value);
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].WritePacket(bw);
            }
        }

        internal static void SendDropInfo(int client, IList<Projection> projections, IList<PMaterial> materials, IList<PBundle> bundles)
        {
            if (client < 0 || client >= Main.maxPlayers || Main.netMode != NetmodeID.MultiplayerClient) { return; }

            Span<ushort> projLens = projections.Count > 256 ? new ushort[projections.Count] : stackalloc ushort[projections.Count];
            Span<ushort> matLens = materials.Count > 256 ? new ushort[materials.Count] : stackalloc ushort[materials.Count];
            Span<ushort> bunLens = bundles.Count > 256 ? new ushort[bundles.Count] : stackalloc ushort[bundles.Count];

            if (PreCalcDropInfoHeader(projLens, matLens, bunLens,
                out int totalProj, out int totalMat, out int totalBun, out int validProj, out int validMat, out int validBun,
                projections, materials, bundles))
            {
                uint version = Projections.DropVersion++;
                int pr = 0, ma = 0, bu = 0;
                int written = 18 + validProj * 10 + validMat * 10 + validBun * 10;

                int left = DROP_INFO_CHUNK_SIZE - written;
                if (left < 0)
                {
                    Projections.Log(LogType.Error, $"Client '{Main.player[client].name} [{client}]' tried to send {validProj} projections, {validMat} materials and {validBun} bundles, but the header was too large by {-left} bytes!");
                    return;
                }
                written += 32;

                DropSource[] clientData = new DropSource[totalProj + totalMat + totalBun];
                var cDataSpan = clientData.AsSpan();
                ModPacket packet = Projections.Instance.GetPacket(DROP_INFO_CHUNK_SIZE);

                packet.Write((byte)MessageType.ClientSendDropInfo);
                packet.Write((byte)client);
                packet.Write(version);
                packet.Write(clientData.Length);

                packet.Write(validProj);
                for (int i = 0; i < projLens.Length; i++)
                {
                    ushort value = projLens[i];
                    if (value < 1) { continue; }
                    packet.Write(projections[i].Index);
                    packet.Write(projLens[i]);
                    pr++;
                }

                packet.Write(validMat);
                for (int i = 0; i < matLens.Length; i++)
                {
                    ushort value = matLens[i];
                    if (value < 1) { continue; }
                    packet.Write(materials[i].Index);
                    packet.Write(matLens[i]);
                    ma++;
                }

                packet.Write(validBun);
                for (int i = 0; i < bunLens.Length; i++)
                {
                    ushort value = bunLens[i];
                    if (value < 1) { continue; }
                    packet.Write(bundles[i].Index);
                    packet.Write(bunLens[i]);
                    bu++;
                }

                var span = clientData.AsSpan();
                int toWrite = Math.Min(left / DROP_ITEM_SIZE, clientData.Length);
                int totalPos = 0;

                for (int i = 0; i < projections.Count; i++)
                {
                    if (projLens[i] < 1) { continue; }
                    for (int j = 0; j < projLens[i]; j++)
                    {
                        clientData[totalPos++] = projections[i].Sources[j];
                    }
                }

                for (int i = 0; i < materials.Count; i++)
                {
                    if (matLens[i] < 1) { continue; }
                    for (int j = 0; j < matLens[i]; j++)
                    {
                        clientData[totalPos++] = materials[i].Sources[j];
                    }
                }

                for (int i = 0; i < bundles.Count; i++)
                {
                    if (bunLens[i] < 1) { continue; }
                    for (int j = 0; j < bunLens[i]; j++)
                    {
                        clientData[totalPos++] = bundles[i].Material.Sources[j];
                    }
                }

                int pCount = totalProj;
                int mCount = totalMat;
                int bCount = totalBun;
                int totalPosProj = 0;
                int totalPosMat = totalProj;
                int totalPosBun = totalMat + totalBun;

                totalPos = 0;
                packet.Write(toWrite);
                int toHeader = Math.Min(toWrite, totalProj);

                packet.Write(version);
                WriteDrops(packet, totalPosProj, cDataSpan.Slice(totalPosProj, toHeader));
                totalPos += toHeader;
                totalPosProj += toHeader;
                toWrite -= toHeader;
                pCount -= toHeader;

                toHeader = Math.Min(toWrite, totalMat);
                WriteDrops(packet, totalPosMat, cDataSpan.Slice(totalPosMat, toHeader));
                totalPos += toHeader;
                totalPosMat += toHeader;
                mCount -= toHeader;

                toHeader = Math.Min(toWrite, totalBun);
                WriteDrops(packet, totalPosBun, cDataSpan.Slice(totalPosBun, toHeader));
                totalPos += toHeader;
                totalPosBun += toHeader;
                bCount -= toHeader;

                packet.Send(0xFF, client);

                int packetsSent = 1;
                while (totalPos < clientData.Length)
                {
                    int dropCount = MAX_DROPS_PER_PACKET;
                    packet = Projections.Instance.GetPacket(DROP_INFO_CHUNK_SIZE + 16);
                    packet.Write((byte)MessageType.ClientSendDropInfoChunk);
                    packet.Write((byte)client);

                    toWrite = Math.Min(pCount, dropCount);

                    packet.Write(version);
                    WriteDrops(packet, totalPosProj, cDataSpan.Slice(totalPosProj, toWrite));
                    totalPos += toWrite;
                    totalPosProj += toWrite;
                    dropCount -= toWrite;
                    pCount -= toWrite;

                    toWrite = Math.Min(mCount, dropCount);
                    WriteDrops(packet, totalPosMat, cDataSpan.Slice(totalPosMat, toWrite));
                    totalPos += toWrite;
                    totalPosMat += toWrite;
                    dropCount -= toWrite;
                    mCount -= toWrite;

                    toWrite = Math.Min(bCount, dropCount);
                    WriteDrops(packet, totalPosBun, cDataSpan.Slice(totalPosBun, toWrite));
                    totalPos += toWrite;
                    totalPosBun += toWrite;
                    dropCount -= toWrite;
                    bCount -= toWrite;

                    packet.Send(0xFF, client);
                    packetsSent++;
                }
                Projections.Log(LogType.Info, $"Client '{Main.player[client].name} [{client}]' sent a total of {packetsSent} drop info packets!");
            }
        }
    }
}
