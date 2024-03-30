using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Projections.Core.Systems;
using Projections.Content.Items.RarityStones;
using Microsoft.Xna.Framework;
using Projections.Content.Items;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Collections;
using Projections.Core.Utilities;
using Projections.Common.Netcode;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Common.PTypes.OnDisk;
using Projections.Common.PTypes.Streamed;

namespace Projections
{
    public delegate OnDiskProjection NewDiskProjection(string path);
    public delegate OnDiskPMaterial NewDiskMaterial(string path);
    public delegate OnDiskPBundle NewDiskBundle(string path);

    public class Projections : Mod
    {
        public const string STREAMED_IDENTIFIER_STR = "PDAT";
        public static uint STREAMED_IDENTIFIER { get; } = STREAMED_IDENTIFIER_STR.ToHeaderIdentifier();
        public const uint DEFAULT_PROJECTOR_ID = 0;

        public const int MOD_IO_VERSION = 2;

        public static Projections Instance { get; private set; }
        public static bool IsLoaded => Instance?._loaded ?? false;

        public static string DefaultProjectionPath => Instance?._projectionPath ?? "";

        internal static uint DropVersion
        {
            get => Instance?._currentDropVersion ?? 0;
            set
            {
                if(Instance == null) { return; }
                Instance._currentDropVersion = value;
            }
        }

        internal OrderedList<Projection, ProjectionIndex> AllProjections => _projections;
        internal OrderedList<PMaterial, ProjectionIndex> AllPMaterials => _materials;
        internal OrderedList<PBundle, ProjectionIndex> AllPBundles => _bundles;

        private OrderedList<Projection, ProjectionIndex> _projections = new OrderedList<Projection, ProjectionIndex>(8, false, CompareHelpers.CompareByProjectionID, CompareHelpers.CompareByProjectionID);
        private OrderedList<PMaterial, ProjectionIndex> _materials = new OrderedList<PMaterial, ProjectionIndex>(8, false, CompareHelpers.CompareByProjectionID, CompareHelpers.CompareByProjectionID);
        private OrderedList<PBundle, ProjectionIndex> _bundles = new OrderedList<PBundle, ProjectionIndex>(8, false, CompareHelpers.CompareByProjectionID, CompareHelpers.CompareByProjectionID);

        private OrderedList<string> _tagPool = new OrderedList<string>();
        private OrderedList<PFlags, ProjectionIndex> _pMatFlags = new OrderedList<PFlags, ProjectionIndex>(8, CompareByProjectionID, CompareByProjectionID);
        private Bitset _itemMatSet = new Bitset();

        private Dictionary<string, List<(string, int)>> _modToNPC = new Dictionary<string, List<(string, int)>>();
        private Dictionary<string, Dictionary<string, int>> _modToItem = new Dictionary<string, Dictionary<string, int>>();

        private HashSet<DiskSource> _projectionSources = new HashSet<DiskSource>();

        private HashSet<Projection> _externalProjections = new HashSet<Projection>();
        private HashSet<PMaterial> _externalMaterials = new HashSet<PMaterial>();
        private HashSet<PBundle> _externalBundles = new HashSet<PBundle>();

        private OrderedList<DiskCreator<NewDiskProjection>, uint> _projectionCreators = new OrderedList<DiskCreator<NewDiskProjection>, uint>(8, CompareByDiskID, CompareByDiskID);
        private OrderedList<DiskCreator<NewDiskMaterial>, uint> _materialCreators = new OrderedList<DiskCreator<NewDiskMaterial>, uint>(8, CompareByDiskID, CompareByDiskID);
        private OrderedList<DiskCreator<NewDiskBundle>, uint> _bundleCreators = new OrderedList<DiskCreator<NewDiskBundle>, uint>(8, CompareByDiskID, CompareByDiskID);

        private string _projectionPath;
        private bool _loaded = false;

        private bool _shouldValidate = false;
        private bool _shouldLoad = false;
        private bool _shouldUnload = false;
        private bool _shouldSendPoolData = false;
        private uint _currentDropVersion;

        private class PlayerDropData
        {
            public bool IsInProgress => sources != null;

            public DropSource[] sources;
            public IndexPacket[] projections;
            public IndexPacket[] materials;
            public IndexPacket[] bundles;
            public int totalRead;
            public uint version;

            public bool ReadPacket(BinaryReader br)
            {
                version = br.ReadUInt32();

                Array.Resize(ref sources, br.ReadInt32());
                Array.Resize(ref projections, br.ReadInt32());
                for (int i = 0; i < projections.Length; i++)
                {
                    projections[i].ReadPacket(br);
                }

                Array.Resize(ref materials, br.ReadInt32());
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].ReadPacket(br);
                }

                Array.Resize(ref bundles, br.ReadInt32());
                for (int i = 0; i < bundles.Length; i++)
                {
                    bundles[i].ReadPacket(br);
                }
                return ReadChunk(br);
            }

            public bool ReadChunk(BinaryReader br)
            {
                uint version = br.ReadUInt32();
                if(version != this.version || !IsInProgress)
                {
                    return false;
                }

                totalRead += ProjectionNetUtils.ReadDropInfoChunk(br, sources);
                return totalRead >= sources.Length;
            }

            public void Apply(int client)
            {
                if(totalRead < sources.Length) { return; }

                var span = sources.AsSpan();
                int pos = 0;
                for (int i = 0; i < projections.Length; i++)
                {
                    ref var proj = ref projections[i];
                    ItemDropManager.ReceivePlayerSources(client, PType.TProjection, proj.index, span.Slice(pos, proj.length));
                    pos += proj.length;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    ref var mat = ref materials[i];
                    ItemDropManager.ReceivePlayerSources(client, PType.TPMaterial, mat.index, span.Slice(pos, mat.length));
                    pos += mat.length;
                }

                for (int i = 0; i < bundles.Length; i++)
                {
                    ref var bun = ref bundles[i];
                    ItemDropManager.ReceivePlayerSources(client, PType.TPBundle, bun.index, span.Slice(pos, bun.length));
                    pos += bun.length;
                }
            }

            public void Reset()
            {
                totalRead = 0;
                sources = null;
                materials = null;
                bundles = null;
                projections = null;
            }
        }
        private struct IndexPacket
        {
            public ushort length;
            public ProjectionIndex index;

            public void ReadPacket(BinaryReader br)
            {
                length = br.ReadUInt16();
                br.Read(ref index);
            }
        }

        private struct DiskCreator<T>
        {
            public uint identifier;
            public T func;
        }

        private struct DiskSource : IEquatable<DiskSource>
        {
            public uint identifier;
            public string path;

            public override bool Equals(object obj)
            {
                return obj is DiskSource source && Equals(source);
            }

            public bool Equals(DiskSource other)
            {
                return path == other.path && identifier == other.identifier;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(path, identifier);
            }

            public static bool operator ==(DiskSource left, DiskSource right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(DiskSource left, DiskSource right)
            {
                return !(left == right);
            }
        }

        private PlayerDropData[] _playerDrops;

        public Projections() 
        {
            Instance = this;
        }

        internal static bool AddSource(uint identifier, string source)
        {
            if (Instance == null || identifier == 0 || string.IsNullOrEmpty(source)) { return false; }
            var src = new DiskSource()
            {
                identifier = identifier,
                path = source
            };
            if (Instance._projectionSources.Add(src))
            {
                if (IsLoaded)
                {
                    Instance._shouldUnload = true;
                    Instance._shouldLoad = true;
                }
                return true;
            }
            return false;
        }

        internal static bool RegisterDiskCreator(uint identifier, NewDiskMaterial create)
        {
            if(identifier == 0 || Instance == null || create == null)
            {
                return false;
            }

            return Instance._materialCreators.Add(new DiskCreator<NewDiskMaterial>()
            {
                func = create,
                identifier = identifier,
            });
        }
        internal static bool RegisterDiskCreator(uint identifier, NewDiskProjection create)
        {
            if (identifier == 0 || Instance == null || create == null)
            {
                return false;
            }

            return Instance._projectionCreators.Add(new DiskCreator<NewDiskProjection>()
            {
                func = create,
                identifier = identifier,
            });
        }
        internal static bool RegisterDiskCreator(uint identifier, NewDiskBundle create)
        {
            if (identifier == 0 || Instance == null || create == null)
            {
                return false;
            }

            return Instance._bundleCreators.Add(new DiskCreator<NewDiskBundle>()
            {
                func = create,
                identifier = identifier,
            });
        }

        internal static bool RegisterExternal(Projection projection)
        {
            if(Instance == null || projection == null || Main.netMode == NetmodeID.Server || !projection.Index.IsValidID()) { return false; }
            if (Instance._externalProjections.Add(projection))
            {
                if (IsLoaded)
                {
                    if (Instance._projections.Add(projection))
                    {
                        Instance._shouldValidate = true;
                        Instance._shouldSendPoolData = true;
                    }
                }
                return true;
            }
            return false;
        }
        internal static bool RegisterExternal(PMaterial material)
        {
            if (Instance == null || material == null || Main.netMode == NetmodeID.Server || !material.Index.IsValidID()) { return false; }
            if (Instance._externalMaterials.Add(material))
            {
                if (IsLoaded)
                {
                    if (Instance._materials.Add(material))
                    {
                        Instance._shouldValidate = true;
                        Instance._shouldSendPoolData = true;
                    }
                }
                return true;
            }
            return false;
        } 
        internal static bool RegisterExternal(PBundle bundle)
        {
            if (Instance == null || bundle == null || Main.netMode == NetmodeID.Server || !bundle.Index.IsValidID()) { return false; }
            if (Instance._externalBundles.Add(bundle))
            {
                if (IsLoaded)
                {
                   Instance._bundles.Add(bundle);
                }
                return true;
            }
            return false;
        }

        internal static bool UnregisterExternal(Projection projection)
        {
            if (Instance == null || projection == null || Main.netMode == NetmodeID.Server) { return false; }
            if (Instance._externalProjections.Remove(projection))
            {
                if (IsLoaded)
                {
                    if (Instance._projections.Remove(projection))
                    {
                        Instance._shouldValidate = true;
                        Instance._shouldSendPoolData = true;
                    }
                }
                return true;
            }
            return false;
        }
        internal static bool UnregisterExternal(PMaterial material)
        {
            if (Instance == null || material == null || Main.netMode == NetmodeID.Server) { return false; }
            if (Instance._externalMaterials.Remove(material))
            {
                if (IsLoaded)
                {
                    if (Instance._materials.Remove(material))
                    {
                        Instance._shouldValidate = true;
                        Instance._shouldSendPoolData = true;
                    }
                }
                return true;
            }
            return false;
        }    
        internal static bool UnregisterExternal(PBundle bundle)
        {
            if (Instance == null || bundle == null || Main.netMode == NetmodeID.Server) { return false; }
            if (Instance._externalBundles.Remove(bundle))
            {
                if (IsLoaded)
                {
                    Instance._bundles.Remove(bundle);
                }
                return true;
            }
            return false;
        }

        internal static int IndexOfTag(ReadOnlySpan<char> tag)
        {
            if (Instance == null) { return -1; }
            for (int i = 0; i < Instance._tagPool.Count; i++)
            {
                if (tag.Equals(Instance._tagPool[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
        internal static string GetTag(int index)
        {
            return Instance != null && index >= 0 && index < Instance._tagPool.Count ? Instance._tagPool[index] : "";
        }

        internal static int AddTag(ReadOnlySpan<char> tag)
        {
            if (Instance == null || tag.IsWhiteSpace()) { return -1; }
            int ind = IndexOfTag(tag);
            if (ind > -1) { return ind; }

            Instance._tagPool.Add(tag.ToString());
            return Instance._tagPool.Count - 1;
        }     

        public static bool IsValidIndex(ProjectionIndex index, PType type)
        {
            if (Instance == null) { return false; }

            switch (type)
            {
                default: return false;
                case PType.TProjection:
                    return Instance._projections.Contains(index);

                case PType.TPMaterial:
                    return Instance._materials.Contains(index);

                case PType.TPBundle:
                    return Instance._materials.Contains(index);
            }
        }

        public static bool TryGetMaterial(ProjectionIndex index, out PMaterial materialOut)
        {
            materialOut = null;
            if (Instance == null || !index.IsValidID()) { return false; }
            materialOut = Instance._materials.SelectBy(index);
            return materialOut != null;
        }
        public static bool TryGetProjection(ProjectionIndex index, out Projection projectionOut)
        {
            projectionOut = null;
            if (Instance == null || !index.IsValidID()) { return false; }
            projectionOut = Instance._projections.SelectBy(index);
            return projectionOut != null;
        }
        public static bool TryGetBundle(ProjectionIndex index, out PBundle bundleOut)
        {
            bundleOut = null;
            if (Instance == null || !index.IsValidID()) { return false; }
            bundleOut = Instance._bundles.SelectBy(index);
            return bundleOut != null;
        }
        
        public static Projection GetProjection(ProjectionIndex index)
        {
            if (Instance == null) { return null; }
            return Instance._projections.SelectBy(index);
        }
        public static PMaterial GetMaterial(ProjectionIndex index, PType type)
        {
            if(Instance == null) { return null; }
            return type switch
            {
                PType.TProjection => Instance._projections.SelectBy(index)?.Material,
                PType.TPMaterial => Instance._materials.SelectBy(index),
                PType.TPBundle => Instance._bundles.SelectBy(index)?.Material,
                _ => null,
            };
        }
        public static PBundle GetBundle(ProjectionIndex index)
        {
            if (Instance == null) { return null; }
            return Instance._bundles.SelectBy(index);
        }

        public static bool IsUsedInRecipe(int id) => Instance?._itemMatSet[id] ?? false; 
        public static bool IsUsedInRecipe(ProjectionIndex index, PType type)
        {
            if(Instance == null || !index.IsValidID())
            {
                return false;
            }
            int ind = Instance._pMatFlags.IndexOf(index);
            return ind >= 0 && Instance._pMatFlags[ind].IsOK(type);
        }

        public static bool ReloadAllProjections()
        {
            if (Instance == null || Main.gameMenu) { return false; }
            Instance._shouldUnload = IsLoaded;
            Instance._shouldLoad = true;
            return true;
        }
        public static bool UnloadAllProjections()
        {
            if (Instance == null || Main.gameMenu || !IsLoaded) { return false; }
            Instance._shouldUnload = true;
            return true;
        }
        public static bool LoadAllProjections()
        {
            if (Instance == null || Main.gameMenu || IsLoaded) { return false; }
            Instance._shouldLoad = true;
            return true;
        }

        internal static void Log(LogType type, string message)
        {
            var logger = Instance?.Logger;
            if (logger == null) { return; }
            switch (type)
            {
                case LogType.Info:
                    logger.Info(message);
                    break;
                case LogType.Warning:
                    logger.Warn(message);
                    break;
                case LogType.Error:
                    logger.Error(message);
                    break;
            }
        }

        public static bool CanShimmer(ProjectionIndex index, PType type)
        {
            if(type == PType.TPBundle) { return false; }

            var mat = GetMaterial(index, type);
            if(mat == null) { return false; }
            return mat.Flags.HasFlag(PMaterialFlags.AllowShimmer) && mat.HasValidRecipe();
        }
        internal static bool SpawnRecipeAsItems(ProjectionSource source, ProjectionIndex index, Vector2 position, int stackSize, PType type, Action<Item> onSpawn = null)
        {
            PMaterial material = GetMaterial(index, type);
            if (material == null || material.Recipes.Length < 1) { return false; }
            if (material.TryGetValidRecipe(out var rec, source.Recipe))
            {
                if (type == PType.TProjection)
                {
                    ProjectionNetUtils.SpawnProjectionItem<ProjectionItem>(ProjectionIndex.Zero, position, stackSize, onSpawn);
                    switch (material.Rarity)
                    {
                        default:
                            ProjectionNetUtils.SpawnItem<RarityStone0>(position, stackSize, onSpawn);
                            break;
                        case PRarity.Intermediate:
                            ProjectionNetUtils.SpawnItem<RarityStone1>(position, stackSize, onSpawn);
                            break;
                        case PRarity.Advanced:
                            ProjectionNetUtils.SpawnItem<RarityStone2>(position, stackSize, onSpawn);
                            break;
                        case PRarity.Expert:
                            ProjectionNetUtils.SpawnItem<RarityStone3>(position, stackSize, onSpawn);
                            break;
                        case PRarity.Master:
                            ProjectionNetUtils.SpawnItem<RarityStone4>(position, stackSize, onSpawn);
                            break;
                    }
                }
                rec.SpawnAsItems(position, onSpawn);
                return true;
            }
            return false;
        }
       
        public override void Load()
        {
            _projectionPath = $"{ModLoader.ModPath}/{Instance.Name}/Projection Data/";
            _projectionSources.Add(new DiskSource()
            {
                identifier = STREAMED_IDENTIFIER,
                path = _projectionPath,
            });

            RegisterDiskCreator(STREAMED_IDENTIFIER, (string path) =>
            {
                return new StreamedPMaterial(path, STREAMED_IDENTIFIER);
            });
            RegisterDiskCreator(STREAMED_IDENTIFIER, (string path) =>
            {
                return new StreamedProjection(path, STREAMED_IDENTIFIER);
            });

            _playerDrops = new PlayerDropData[Main.maxPlayers];
            if (Main.netMode == NetmodeID.Server)
            {
                for (int i = 0; i < _playerDrops.Length; i++)
                {
                    _playerDrops[i] = new PlayerDropData();
                }
            }
        }
        public override void PostSetupContent()
        {
            LoadNPCList();
            LoadItemList();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if(whoAmI == Main.myPlayer)
            {
                return;
            }

            MessageType type = reader.Read<MessageType>();
            byte sender = reader.ReadByte();

            ProjectorData pData = new ProjectorData();
            switch (type)
            {
                case MessageType.CreateProjector:
                    {
                        pData.Deserialize(reader);
                        var proj = ProjectorSystem.AddProjector(in pData);

                        if(Main.netMode == NetmodeID.Server)
                        {
                            if (pData.Type == ProjectorType.Custom && proj == null)
                            {
                                uint old = pData.id.uniqueID;
                                pData.id.uniqueID = ++ProjectorSystem.CurrentID;
                                proj = ProjectorSystem.AddProjector(in pData);

                                if(proj != null)
                                {
                                    ProjectionNetUtils.SendCustomProjectorUpdate(old, pData.id.uniqueID, sender);
                                }
                                else
                                {
                                    ProjectionNetUtils.SendKillProjector(ProjectorID.FromCustom(old), true, 0xFF, sender);
                                    break;
                                }
                            }
                            ProjectionNetUtils.SendCreateProjector(in pData, sender);
                        }
                    }
                    break;

                case MessageType.UpdateProjector:
                    {
                        pData.Deserialize(reader);
                        if(ProjectorSystem.TryGetProjector(in pData.id, out var projector))
                        {
                            projector.Deserialize(reader, out var sType);
                            if(Main.netMode == NetmodeID.Server)
                            {
                                ProjectionNetUtils.SendProjectorUpdate(projector, sType, sender);
                            }
                        }
                    }
                    break;
                case MessageType.ClientUpdateCustom:
                    {
                        if(Main.myPlayer == sender)
                        {
                            ProjectorSystem.UpdateCustom(reader.ReadUInt32(), reader.ReadUInt32());
                        }
                    }
                    break;

                case MessageType.KillProjector:
                    {
                        bool eject = reader.ReadBoolean();
                        pData.id.Deserialize(reader);             
                        if (Main.netMode == NetmodeID.Server)
                        {
                            ProjectionNetUtils.SendKillProjector(in pData.id, eject, sender);
                        }
                    }
                    break;
                case MessageType.PlayerTime:
                case MessageType.PlayerObtainedProjection:
                case MessageType.PlayerProjectorFlags:
                    Main.player[sender].PPlayer().ReceiveMessage(reader, type);
                    break;
                case MessageType.ClientResetDropInfo:
                    if (Main.netMode == NetmodeID.Server && _playerDrops[sender].IsInProgress)
                    {
                        _playerDrops[sender].Reset();
                    }
                    ItemDropManager.ClearPlayerSources(sender);
                    break;
                case MessageType.ClientSendDropInfo:
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            var dropProg = _playerDrops[sender];
                            if (dropProg.ReadPacket(reader))
                            {
                                dropProg.Apply(sender);
                                dropProg.Reset();
                            }
                        }
                    }
                    break;
                case MessageType.ClientSendDropInfoChunk:
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            var dropProg = _playerDrops[sender];
                            if (dropProg.ReadChunk(reader))
                            {
                                dropProg.Apply(sender);
                                dropProg.Reset();
                            }
                        }
                    }
                    break;
                case MessageType.EraseAllProjectors:
                    {
                        bool eject = reader.ReadBoolean();
                        ProjectorSystem.Instance.EraseAllProjectors(eject, true);
                    }
                    break;
            }
        }

        internal static int GetItemIndex(ReadOnlySpan<char> path)
        {
            int ind = path.IndexOf(':');
            if (ind > -1)
            {
                string modName = path.Slice(0, ind).ToString();
                return Instance._modToItem.TryGetValue(modName, out var dict) && dict.TryGetValue(path.Slice(ind + 1).ToString(), out int val) ? val : 0;
            }
            return 0;
        }
        internal static int GetNPCIndex(ReadOnlySpan<char> path)
        {
            int ind = path.IndexOf(':');

            if (ind > -1)
            {
                string modName = path.Slice(0, ind).ToString();
                if (Instance._modToNPC.TryGetValue(modName, out var list))
                {
                    var npcName = path.Slice(ind + 1);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (npcName.Equals(list[i].Item1, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return list[i].Item2;
                        }
                    }
                }
            }
            return 0;
        }

        internal static void MarkItemAsMaterial(int id)
        {
            if (id <= 0 || Instance == null) { return; }
            Instance._itemMatSet[id] = true;
        }
        internal static void MarkItemAsMaterial(ProjectionIndex index, PType type)
        {
            if (Instance == null || !index.IsValidID()) { return; }

            int ind = Instance._pMatFlags.IndexOf(index);
            if(ind > -1)
            {
                switch (type)
                {
                    case PType.TProjection:
                        Instance._pMatFlags[ind].flags |= PFlags.HAS_PROJECTION;
                        break;
                    case PType.TPMaterial:
                        Instance._pMatFlags[ind].flags |= PFlags.HAS_MATERIAL;
                        break;
                    case PType.TPBundle:
                        Instance._pMatFlags[ind].flags |= PFlags.HAS_BUNDLE;
                        break;
                }
            }
        }

        internal void Update()
        {
            if (Main.gameMenu) { return; }
            if(Main.netMode != NetmodeID.Server)
            {
                if (_shouldUnload)
                {
                    UnloadProjections();
                }

                if (_shouldLoad)
                {
                    LoadProjections();
                }

                if (_shouldValidate)
                {       
                    ValidateAll();
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    if (_shouldSendPoolData)
                    {
                        SendDropInfo();
                    }
                }

                if (_loaded)
                {
                    for (int i = 0; i < _projections.Count; i++)
                    {
                        _projections[i].Update();
                    }
                }
            }
        }

        internal void SendDropInfo()
        {
            ProjectionNetUtils.SendDropInfo(Main.myPlayer, _projections, _materials, _bundles);
            _shouldSendPoolData = false;
        }

        internal void ValidateAll()
        {
            ProjectorSystem.ValidateProjectors();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Main.player[i].ValidateProjections();
            }

            for (int i = 0; i < Main.maxChests; i++)
            {
                Main.chest[i].ValidateProjections();
            }

            for (int i = 0; i < Main.maxItems; i++)
            {
                Main.item[i].ValidateProjection();
            }
            _shouldValidate = false;
        }

        internal void ReloadMaterialUsage()
        {
            _itemMatSet.SetAll(false);
            for (int i = 0; i < _pMatFlags.Count; i++)
            {
                _pMatFlags[i].flags = (byte)(_pMatFlags[i].flags & ~(PFlags.HAS_MATERIAL | PFlags.HAS_PROJECTION));
            }

            foreach (var mat in _materials)
            {
                foreach(var recipe in mat.Recipes)
                {
                    recipe.MarkItemsAsUsed();
                }
            }

            foreach (var proj in _projections)
            {
                foreach (var recipe in proj.Material.Recipes)
                {
                    recipe.MarkItemsAsUsed();
                }
            }
        }

        private bool AddProjection(Projection projection, string message)
        {
            if (!projection.Load())
            {
                Log(LogType.Error, $"Failed to load Projection '{message}'! (Projection.Load returned false)");
                projection.Unload();
                return false;
            }

            if (!projection.Index.IsValidID()) { return false; }

            int ind = _projections.IndexOf(projection.Index);
            if (ind < 0)
            {
                _projections.Add(projection);
                return true;
            }
            ref var aProj = ref _projections[ind];
            if (aProj.Priority < projection.Priority)
            {
                aProj.Unload();
                aProj = projection;
                return true;
            }

            Log(LogType.Error, $"Failed to add '{projection.Name}'! (Projection Index was Invalid or a Projection with a higher priority was already added)");
            projection.Unload();
            return false;
        }
        private bool AddMaterial(PMaterial material, string message)
        {
            if (!material.Load())
            {
                Log(LogType.Error, $"Failed to load P-Material '{message}'! (PMaterial.Load returned false)");
                material.Unload();
                return false;
            }

            if (!material.Index.IsValidID()) { return false; }

            int ind = _materials.IndexOf(material.Index);
            if (ind < 0)
            {
                _materials.Add(material);
                return true;
            }
            ref var aMat = ref _materials[ind];
            if (aMat.Priority < material.Priority)
            {
                aMat.Unload();
                aMat = material;
                return true;
            }

            Log(LogType.Error, $"Failed to add '{material.Name}'! (Projection Index was Invalid or a Material with a higher priority was already added)");
            material.Unload();
            return false;
        }
        private bool AddBundle(PBundle bundle, string message)
        {
            if (!bundle.Load())
            {
                Log(LogType.Error, $"Failed to load P-Bundle '{message}'! (PBundle.Load returned false)");
                bundle.Unload();
                return false;
            }

            if (!bundle.Index.IsValidID()) { return false; }

            int ind = _bundles.IndexOf(bundle.Index);
            if (ind < 0)
            {
                _bundles.Add(bundle);
                return true;
            }
            ref var aBun = ref _bundles[ind];
            if (aBun.Priority < bundle.Priority)
            {
                aBun.Unload();
                aBun = bundle;
                return true;
            }

            Log(LogType.Error, $"Failed to add '{bundle.Name}'! (Projection Index was Invalid or a Bundle with a higher priority was already added)");
            bundle.Unload();
            return false;
        }

        internal void LoadProjections()
        {
            if (_loaded)
            {
                _shouldLoad = false;
                return;
            }

            if(Main.netMode == NetmodeID.Server)
            {
                _shouldLoad = false;
                _loaded = true;
                return;
            }

            _itemMatSet.SetAll(false);
            _pMatFlags.Clear();

            ProjectionIndex rIndex = ProjectionIndex.Zero;
            List<string> projDirs = new List<string>();
            List<string> matDirs = new List<string>();
            List<string> bunDirs = new List<string>();
            foreach (var pDir in _projectionSources)
            {
                matDirs.Clear();
                projDirs.Clear();
                bunDirs.Clear();

                var pType = GetPaths(pDir.path, projDirs, matDirs, bunDirs);
                if (pType == ProjectionSrc.Unknown) { continue; }

                switch (pType)
                {
                    case ProjectionSrc.Disk:
                        Directory.CreateDirectory(pDir.path);
                        break;
                }

                foreach (var projDir in projDirs)
                {
                    var func = _projectionCreators.SelectBy(pDir.identifier).func;
                    if (func == null)
                    {
                        Log(LogType.Error, $"Failed to load '{projDir}'! (Could not find Creator for ID '{pDir.identifier}')");
                        continue;
                    }

                    OnDiskProjection proj = func.Invoke(projDir);
                    if (proj == null)
                    {
                        Log(LogType.Error, $"Failed to load '{projDir}'! (Projection Creator func of ID '{pDir.identifier}' returned null)");
                        continue;
                    }
                    AddProjection(proj, projDir);
                }

                foreach (var matDir in matDirs)
                {
                    var func = _materialCreators.SelectBy(pDir.identifier).func;
                    if(func == null)
                    {
                        Log(LogType.Error, $"Failed to load '{matDir}'! (Could not find Creator for ID '{pDir.identifier}')");
                        continue;
                    }

                    OnDiskPMaterial mat = func.Invoke(matDir);
                    if(mat == null)
                    {
                        Log(LogType.Error, $"Failed to load '{matDir}'! (Material Creator func of ID '{pDir.identifier}' returned null)");
                        continue;
                    }
                    AddMaterial(mat, matDir);
                }

                foreach (var bunDir in bunDirs)
                {
                    var func = _bundleCreators.SelectBy(pDir.identifier).func;
                    if(func == null)
                    {
                        Log(LogType.Error, $"Failed to load '{bunDir}'! (Could not find Creator for ID '{pDir.identifier}')");
                        continue;
                    }

                    OnDiskPBundle bun = func.Invoke(bunDir);
                    if(bun == null)
                    {
                        Log(LogType.Error, $"Failed to load '{bunDir}'! (Bundle Creator func of ID '{pDir.identifier}' returned null)");
                        continue;
                    }
                    AddBundle(bun, bunDir);
                }
            }

            foreach (var proj in _externalProjections)
            {
                AddProjection(proj, proj.Name);
            }

            foreach (var mat in _externalMaterials)
            {
                AddMaterial(mat, mat.Name);
            }

            foreach (var bundle in _externalBundles)
            {
                AddBundle(bundle, bundle.Name);
            }
            ReloadMaterialUsage();

            ItemDropManager.ClearPlayerSources(Main.myPlayer);
            for (int i = 0; i < _projections.Count; i++)
            {
                var projection = _projections[i];
                ItemDropManager.ReceivePlayerSources(Main.myPlayer, PType.TProjection, projection.Index, projection.Sources);
            }

            for (int i = 0; i < _materials.Count; i++)
            {
                var material = _materials[i];
                ItemDropManager.ReceivePlayerSources(Main.myPlayer, PType.TPMaterial, material.Index, material.Sources);
            }

            for (int i = 0; i < _bundles.Count; i++)
            {
                var bundle = _bundles[i];
                ItemDropManager.ReceivePlayerSources(Main.myPlayer, PType.TPBundle, bundle.Index, bundle.Material.Sources);
            }

            _loaded = true;
            _shouldLoad = false;
            _shouldSendPoolData = true;
            _shouldValidate = true;
        }
        internal void UnloadProjections()
        {
            if(Main.netMode == NetmodeID.Server || !_loaded) { return; }
            _shouldUnload = false;

            _tagPool.Clear();
            _pMatFlags.Clear();
            _itemMatSet.SetAll(false);

            foreach (var proj in _projections)
            {
                proj.Unload();
            }
            _projections.Clear();

            foreach (var pMat in _materials)
            {
                pMat.Unload();
            }
            _materials.Clear();

            foreach (var pBun in _bundles)
            {
                pBun.Unload();
            }
            _bundles.Clear();

            ItemDropManager.ClearPlayerSources(Main.myPlayer);

            _shouldValidate = true;
            _shouldSendPoolData = true;
            _loaded = false;
        }

        private enum ProjectionSrc
        {
            Disk,
            Resources,
            Unknown
        }

        private static void ParsePaths(string root, string mod, IList<string> paths, List<string> projections, List<string> materials, List<string> bundles)
        {
            if (root.Contains("/~")) { return; }
            foreach(var file in paths)
            {
                if (file.Contains("/~"))
                {
                    continue;
                }
                if(string.IsNullOrWhiteSpace(root) ||
                    file.StartsWith(root, StringComparison.InvariantCultureIgnoreCase)) 
                {
                    if (file.EndsWith(".pdat"))
                    {
                        projections.Add(string.IsNullOrWhiteSpace(mod) ? file : $"tmod:{mod}:{file}");
                    }
                    else if (file.EndsWith(".pmat"))
                    {
                        materials.Add(string.IsNullOrWhiteSpace(mod) ? file : $"tmod:{mod}:{file}");
                    }
                    else if (file.EndsWith(".pbun"))
                    {
                        bundles.Add(string.IsNullOrWhiteSpace(mod) ? file : $"tmod:{mod}:{file}");
                    }
                } 
            }
        }
        private static ProjectionSrc GetPaths(string path, List<string> projections, List<string> materials, List<string> bundles)
        {
            path = path.Replace('\\', '/');
            if (path.IsContentPath())
            {
                ModContent.SplitName(path, out string mod, out string subName);
                if(ModLoader.TryGetMod(mod, out Mod modV))
                { 
                    var filesM = modV.GetFileNames();
                    ParsePaths(path, mod, filesM, projections, materials, bundles);
                }
                return ProjectionSrc.Unknown;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return ProjectionSrc.Unknown;
            }

            if (Directory.Exists(path))
            {
                var filesD = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                ParsePaths("", "", filesD, projections, materials, bundles);
            }
            return ProjectionSrc.Disk;
        }
       
        private void LoadItemList()
        {
            _modToItem.Clear();
            int count = ItemLoader.ItemCount;
            for (int i = ItemID.Count; i < count; i++)
            {
                var item = ItemLoader.GetItem(i);
                if (!_modToItem.TryGetValue(item.Mod.Name, out var dict))
                {
                    dict = new Dictionary<string, int>();
                    _modToItem.Add(item.Mod.Name, dict);
                }
                dict.TryAdd(item.Name, i);
            }
            _itemMatSet.Resize(Main.maxItems);
        }
        private void LoadNPCList()
        {
            _modToNPC.Clear();
            int count = NPCLoader.NPCCount;
            for (int i = NPCID.Count; i < count; i++)
            {
                var npc = NPCLoader.GetNPC(i);
                if (!_modToNPC.TryGetValue(npc.Mod.Name, out var list))
                {
                    list = new List<(string, int)>();
                    _modToNPC.Add(npc.Mod.Name, list);
                }
                list.Add((npc.Name, i));
            }
        }

        private static int CompareByProjectionID(in PFlags lhs, in PFlags rhs)
        {
            return lhs.index.CompareTo(rhs.index);
        }
        private static int CompareByProjectionID(in PFlags lhs, in ProjectionIndex rhs)
        {
            return lhs.index.CompareTo(rhs);
        }

        private static int CompareByDiskID<T>(in DiskCreator<T> lhs, in DiskCreator<T> rhs)
        {
            return lhs.identifier.CompareTo(rhs.identifier);
        }
        private static int CompareByDiskID<T>(in DiskCreator<T> lhs, in uint rhs)
        {
            return lhs.identifier.CompareTo(rhs);
        }

        private struct PFlags
        {
            public const byte HAS_PROJECTION = 0x1;
            public const byte HAS_MATERIAL = 0x2;
            public const byte HAS_BUNDLE = 0x4;

            public bool IsProjection => (flags & HAS_PROJECTION) != 0;
            public bool IsMaterial => (flags & HAS_MATERIAL) != 0;
            public bool IsBundle => (flags & HAS_BUNDLE) != 0;

            public ProjectionIndex index;
            public byte flags;

            public bool IsOK(PType type)
            {
                byte mask = 0;
                switch (type)
                {
                    case PType.TProjection:
                        mask = HAS_PROJECTION;
                        break;
                    case PType.TPMaterial:
                        mask = HAS_MATERIAL;
                        break;
                    case PType.TPBundle:
                        mask = HAS_BUNDLE;
                        break;
                }
                return (flags & mask) != 0;
            }
        }
    }
}