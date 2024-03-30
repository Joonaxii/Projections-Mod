using Microsoft.Xna.Framework;
using Projections.Common.Netcode;
using Projections.Common.PTypes;
using Projections.Content.Items;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Utilities;
using rail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ProjectionLUT = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Projections.Core.Systems.PInfoPtr>>;

namespace Projections.Core.Systems
{
    internal struct PInfoPtr
    {
        public ProjectionIndex index;
        public DropSource source;
    }

    internal struct DropIndex
    {
        public int netID;
        public int ID;

        public DropIndex(int netID, int iD)
        {
            this.netID = netID;
            ID = iD;
        }
    }

    public static class ItemDropManager
    {
        public delegate int OnItemFromPool(Vector2 position, ProjectionIndex index, PType type, int stackSize, int client);

        // All instanced projection items' slots will be locked for 6 minutes
        public const int ITEM_RESERVE_TIME = 360 * 60;

        private static PlayerContext[] _players;

        private class PoolInstance
        {
            public ProjectionLUT poolProjections = new ProjectionLUT();
            public ProjectionLUT poolMaterials = new ProjectionLUT();
            public ProjectionLUT poolBundles = new ProjectionLUT();
            public HashSet<ProjectionIndex> ignoreList = new HashSet<ProjectionIndex>();

            public void Clear()
            {
                poolProjections.Clear();
                poolMaterials.Clear();
                poolBundles.Clear();
                ignoreList.Clear();
            }

            internal void GetDrops(ProjectionLUT dropPools, int id, List<PoolDrop> drops, float randomVal)
            {
                if (dropPools.TryGetValue(id, out var poolNet))
                {
                    foreach (var proj in poolNet)
                    {
                        if (!proj.index.IsValidID()) { continue; }

                        var info = proj.source;
                        if (randomVal < info.Chance && !ignoreList.Contains(proj.index) && info.ConditionsMet)
                        {
                            drops.Add(new PoolDrop()
                            {
                                type = proj.index,
                                Weight = info.Weight,
                                stackSize = info.Stack
                            });
                        }
                    }
                }
            }

            internal bool TryGetPool(PType type, bool allowAny, bool allowNetID, DropIndex drop, List<PoolDrop> drops, float randomVal)
            {
                ProjectionLUT dropPools = null;
                switch (type)
                {
                    case PType.TProjection: dropPools = poolProjections; break;
                    case PType.TPMaterial: dropPools = poolMaterials; break;
                    case PType.TPBundle: dropPools = poolBundles; break;
                };

                drops.Clear();
                if (allowAny)
                {
                    //Any
                    GetDrops(dropPools, 0, drops, randomVal);
                }

                if (allowNetID)
                {
                    //NET-ID
                    GetDrops(dropPools, drop.netID, drops, randomVal);
                }

                //ID
                GetDrops(dropPools, drop.ID, drops, randomVal);
                return drops.Count > 0;
            }
        }

        private struct PoolDrop : IWeighted
        {
            public ProjectionIndex type;
            public float Weight { get; set; }
            public int stackSize;
        }

        private class PlayerContext
        {
            public bool IsLoaded => player != null;

            public Player player;
            public PoolInstance[] instances;

            public List<PoolDrop> tempPool = new List<PoolDrop>();

            public bool Load(Player player)
            {
                if (IsLoaded) { return false; }
                this.player = player;
                instances = new PoolInstance[(int)PoolType.__Count];
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i] = new PoolInstance();
                }
                return true;
            }
            public bool Unload()
            {
                if (!IsLoaded) { return false; }
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i].Clear();
                }
                tempPool.Clear();
                instances = null;
                player = null;
                return true;
            }

            public HashSet<ProjectionIndex> BuildIgnoreList(PoolType pool, PType type)
            {
                if (pool < 0 || pool >= PoolType.__Count) { return null; }
                if (IsLoaded)
                {
                    var instance = instances[(int)pool];
                    instance.ignoreList.Clear();
                    player.PPlayer().BuildIgnoreList(instance.ignoreList, type);
                    return instance.ignoreList;
                }
                return null;
            }

            public void AddToPools(PType type, ProjectionIndex index, ReadOnlySpan<DropSource> sources)
            {
                if (!index.IsValidID() || sources.Length < 1) { return; }

                for (int i = 0; i < sources.Length; i++)
                {
                    ref readonly var source = ref sources[i];
                    if (!source.IsValid) { continue; }
                    var inst = instances[(int)source.Type];

                    ProjectionLUT poolOut = null;
                    switch (type)
                    {
                        case PType.TProjection: poolOut = inst.poolProjections; break;
                        case PType.TPMaterial: poolOut = inst.poolMaterials; break;
                        case PType.TPBundle: poolOut = inst.poolBundles; break;
                    };


                    if (!poolOut.TryGetValue(source.ID, out var infoPtrs))
                    {
                        infoPtrs = new List<PInfoPtr>();
                        poolOut.Add(source.ID, infoPtrs);
                    }
                    infoPtrs.Add(
                    new PInfoPtr()
                    {
                        index = index,
                        source = source
                    });
                }
            }

            internal bool TryGetPool(PoolType pool, PType type, bool allowAny, bool allowNetID, DropIndex index, float rng)
            {
                if (pool < 0 || pool >= PoolType.__Count) { return false; }
                return instances[(int)pool].TryGetPool(type, allowAny, allowNetID, index, tempPool, rng);
            }
        }

        static ItemDropManager()
        {
            _players = new PlayerContext[Main.netMode == NetmodeID.Server ? Main.maxPlayers : 1];
            for (int i = 0; i < _players.Length; i++)
            {
                _players[i] = new PlayerContext();
            }
        }

        internal static DropIndex GetNPCDropIndex(NPC npc) => new DropIndex(npc.netID, npc.type);

        internal static bool ClearPlayerSources(int sender)
        {
            sender = Main.netMode == NetmodeID.Server ? sender : 0;
            if (sender < 0 || sender >= _players.Length) { return false; }
            return _players[sender].Unload();
        }

        internal static void ReceivePlayerSources(int sender, PType type, ProjectionIndex index, ReadOnlySpan<DropSource> sources)
        {
            sender = Main.netMode == NetmodeID.Server ? sender : 0;
            if (sender < 0 || sender >= _players.Length) { return; }
            var context = _players[sender];
            context.Load(Main.player[sender]);
            context.AddToPools(type, index, sources);
        }

        internal static void ClearAllPools()
        {
            foreach (var plr in _players)
            {
                plr.Unload();
            }
        }

        public static int DefaultSpawn(Vector2 position, ProjectionIndex index, PType type, int stackSize, int client)
        {
            ProjectionNetUtils.SpawnProjectionItem(ProjectionUtils.GetProjectionItemID(type),
                index, position, stackSize, null, client >= 0 && client < Main.maxPlayers ? ITEM_RESERVE_TIME : 0, client);
            return 1;
        }

        public static bool DoDrops(NPC npc, float rngMod = 1.0f, float minRNG = 0.0f, int forClient = -1)
        {
            if (npc == null || npc.type == NPCID.None) { return false; }
            return DoDrops(PoolType.NPC, GetNPCDropIndex(npc), null, npc.Center, rngMod, minRNG, forClient: forClient);
        }

        internal static bool DoDrops(PoolType type, DropIndex index, List<Item> itemsOut, ProjectionSource? source = null, float rngMod = 1.0f, float minRNG = 0.0f, bool allowClient = false, int forClient = -1)
        {
            if (itemsOut == null) { return false; }
            source ??= ProjectionSource.Drop;
            return DoDrops(type, index, (Vector2 _, ProjectionIndex index, PType type, int stackSize, int _) =>
            {
                itemsOut.Add(ProjectionUtils.NewProjectionItem(ProjectionUtils.GetProjectionItemID(type), index, source.Value, stackSize)?.Item);
                return 1;
            }, default, rngMod, minRNG, null, null, null, allowClient, forClient);
        }

        internal static bool DoDrops(PoolType pool, DropIndex index, OnItemFromPool itemOut = null, Vector2 position = default, float rngMod = 1.0f, float minRNG = 0.0f, int? projDrops = null, int? matDrops = null, int? bunDrops = null, bool allowClient = false, int forClient = -1)
        {
            if (pool < 0 || pool >= PoolType.__Count || (!allowClient && Main.netMode == NetmodeID.MultiplayerClient)) { return false; }

            itemOut ??= DefaultSpawn;

            int spawned = 0;
            bool allowAny = pool == PoolType.NPC;
            bool allowNetID = pool == PoolType.NPC;
            int projChances = projDrops ?? Main.rand.Next(1, 4);
            int matChances = matDrops ?? Main.rand.Next(2, 8);
            int bunChances = bunDrops ?? Main.rand.Next(1, 4);
            float baseProModifier = 1.0f;
            float baseMatModifier = 1.0f;
            float matModifier;
            float proModifier;

            forClient = Main.netMode == NetmodeID.SinglePlayer ? 0 : forClient;
            bool instanced = forClient > -1;
            int len = forClient < 0 ? _players.Length : forClient + 1;
            forClient = forClient < 0 ? 0 : forClient;
            for (int i = forClient; i < len; i++)
            {
                var plr = _players[i];
                if (plr.IsLoaded)
                {
                    matModifier = baseMatModifier;
                    proModifier = baseProModifier;
                    var ignore = plr.BuildIgnoreList(pool, PType.TPBundle);
                    {
                        ignore = plr.BuildIgnoreList(pool, PType.TPBundle);
                        plr.tempPool.Clear();
                        for (int j = 0; j < bunChances; j++)
                        {
                            float rng = 1.0f - MathF.Max(minRNG, Main.rand.NextFloat() * rngMod);
                            if (plr.TryGetPool(pool, PType.TPBundle, allowAny, allowNetID, index, rng))
                            {
                                var select = plr.tempPool.SelectRandom();
                                ignore.Add(select.type);
                                spawned += itemOut.Invoke(position, select.type, PType.TPBundle, select.stackSize, i);

                                proModifier *= 0.5f;
                                matModifier *= 0.66f;
                            }
                        }
                    }

                    ignore = plr.BuildIgnoreList(pool, PType.TProjection);
                    plr.tempPool.Clear();

                    int pProjChances = (projDrops != null ? projChances : (int)(projChances * proModifier));
                    for (int j = 0; j < pProjChances; j++)
                    {
                        float rng = 1.0f - MathF.Max(minRNG, Main.rand.NextFloat() * rngMod);
                        if (plr.TryGetPool(pool, PType.TProjection, allowAny, allowNetID, index, rng))
                        {
                            var select = plr.tempPool.SelectRandom();
                            ignore.Add(select.type);

                            spawned += itemOut.Invoke(position, select.type, PType.TProjection, select.stackSize, i);
                            matModifier *= 0.66f;
                        }
                    }

                    int pMatChances = (matDrops != null ? matChances : (int)(matChances * matModifier));
                    if (pMatChances > 0)
                    {
                        ignore = plr.BuildIgnoreList(pool, PType.TPMaterial);
                        plr.tempPool.Clear();
                        for (int j = 0; j < pMatChances; j++)
                        {
                            float rng = 1.0f - MathF.Max(minRNG, Main.rand.NextFloat() * rngMod);
                            if (plr.TryGetPool(pool, PType.TPMaterial, allowAny, allowNetID, index, rng))
                            {
                                var select = plr.tempPool.SelectRandom();
                                ignore.Add(select.type);

                                spawned += itemOut.Invoke(position, select.type, PType.TPMaterial, select.stackSize, i);
                            }
                        }
                    }
                }
            }

            return spawned > 0;
        }
    }
}
