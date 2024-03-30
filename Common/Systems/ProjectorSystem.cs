using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using System.Diagnostics;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Projections.Core.Collections;
using Projections.Core.Audio;
using Projections.Core.Utilities;
using Projections.Common.ILEdits;
using Projections.Core.Maths;
using Projections.Core.Textures;
using Projections.Common.Configs;
using Projections.Common.Netcode;
using Projections.Content.Tiles.Projectors;
using Projections.Common.ProjectorTypes;
using ReLogic.Content;
using Projections.Content.Items.Projectors;
using Terraria.Localization;
using Projections.Content;
using Projections.Common.PTypes.Streamed;

namespace Projections.Core.Systems
{
    public delegate Projector CreateProjector(in ProjectorData data);
    public class ProjectorSystem : ModSystem
    {
        public static ProjectorSystem Instance
        {
            get; private set;
        }

        public static bool IsActive => Instance?._isActive ?? false;
        public static uint CurrentID { get; internal set; }

        public const int FRAME_BUFFER_SIZE = 1024 * 1024;
        public static Color MasterColor { get; private set; }

        public static float EssScaleNorm => Instance?._essNorm ?? 0.0f;
        public static float EssScaleSquared => EssScaleNorm * EssScaleNorm;
        public static float EssScaleCubed => EssScaleNorm * EssScaleNorm * EssScaleNorm;
        public float FrameDelta => _curFrame - _prevFrame;

        public Span<Color32> TempBuffer => _frameBuffer.AsSpan().Slice(0, FRAME_BUFFER_SIZE);
        public Span<Color32> FrameBuffer => _frameBuffer.AsSpan().Slice(FRAME_BUFFER_SIZE, FRAME_BUFFER_SIZE);
        public Span<byte> AlphaBuffer => _alphaBuffer.AsSpan().Slice(0, FRAME_BUFFER_SIZE * StreamedProjection.MAX_LAYERS);
        public Span<byte> IndexBuffer => _alphaBuffer.AsSpan().Slice(FRAME_BUFFER_SIZE * StreamedProjection.MAX_LAYERS, FRAME_BUFFER_SIZE * 2);

        private UnmanagedBuffer<byte> _alphaBuffer = new UnmanagedBuffer<byte>();
        private UnmanagedBuffer<Color32> _frameBuffer = new UnmanagedBuffer<Color32>();

        private AudioBufferPool _audioBufferPool = new AudioBufferPool(8);
        internal OrderedList<Projector, ulong> CustomProjectors => _customProjectors;
        private OrderedList<Projector, ulong> _customProjectors = new OrderedList<Projector, ulong>(8, false, CompareHelpers.CompareByProjectorID, CompareHelpers.CompareByProjectorID);

        internal OrderedList<Projector, ulong> TileProjectors => _tileProjectors;

        public static int TotalProjectorCount
        {
            get
            {
                int count = 0;
                if(Instance != null)
                {
                    count += Instance._tileProjectors.Count;
                    count += Instance._customProjectors.Count;

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (!Main.player[i].active) { continue; }
                        var pplr = Main.player[i].PPlayer();
                        count += pplr.CanProject ? pplr.ProjectorCount : 0;
                    }
                }
                return count;
            }
        }

        private OrderedList<Projector, ulong> _tileProjectors = new OrderedList<Projector, ulong>(8, false, CompareHelpers.CompareByProjectorID, CompareHelpers.CompareByProjectorID);
        private OrderedList<Redirect, Point> _tileLUT = new OrderedList<Redirect, Point>(64, false, SortRedirect, SortRedirect);

        private List<Projector>[] _layers;
        private float _prevFrame = 0;
        private float _curFrame = 0;
        private bool[] _suppressKill = new bool[(int)ProjectorType.__Count];
        private bool[] _suppressEject = new bool[(int)ProjectorType.__Count];
        private bool _isActive;
        private float _essNorm;
        private Stopwatch _frameDelta = new Stopwatch();
        private SpriteBatch _spriteBatch;
        private Visualier _fxVisualizer = new Visualier();

        public static RecipeGroup ProjectorRecipeGroup => _projectorGroup;
        private static RecipeGroup _projectorGroup;

        private struct CustomProjGetter
        {
            public uint tag;
            public string tagName;
            public CreateProjector func;
        }
        private OrderedList<CustomProjGetter, uint> _projectorCreators = new OrderedList<CustomProjGetter, uint>();

        public ProjectorSystem()
        {
            Instance = this;
            _layers = new List<Projector>[(int)DrawLayer.__Count];
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i] = new List<Projector>(8);
            }
        }

        internal static AudioBuffer RequestAudioBuffer()
            => Instance?._audioBufferPool.GetBuffer();
        internal static AudioBuffer ReturnAudioBuffer(AudioBuffer buffer)
            => Instance?._audioBufferPool.Return(buffer);

        public override void AddRecipeGroups()
        {
            List<int> items = new List<int>()
            {
                ModContent.ItemType<ProjectorSmallItem>(),
                ModContent.ItemType<ProjectorItem>(),
                ModContent.ItemType<ProjectorBigItem>(),
            };

            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                var item = ItemLoader.GetItem(i);
                if(item == null) { continue; }
                IProjector iProjector = item as IProjector;
                if(iProjector == null)
                {
                    if (item is IProjectorItem projItem)
                    {
                        iProjector = ModContent.GetModTile(projItem.PlacementTileID) as IProjector;
                    }
                }
                
                if(iProjector?.CanBeUsedInRecipe ?? false && !items.Contains(i))
                {
                    items.Add(i);
                }
            }
            Projections.Log(LogType.Info, $"Registered '{items.Count}' Projector Items for use in Recipe Group 'Any Projector'!");

            _projectorGroup ??= new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} Projector [Gains Slots]",
            items.ToArray());
            RecipeGroup.RegisterGroup("Projections:Projector", _projectorGroup);
        }

        internal static bool GetFrameBuffers(int width, int height, int layers, out Span<byte> indexBuffer, out Span<Color32> temp, out Span<Color32> color, out Span<byte> alpha)
        {
            temp = default;
            color = default;
            alpha = default;
            indexBuffer = default;
            if (width <= 0 || height <= 0 || Instance == null)
            {
                return false;
            }
            int reso = width * height;

            temp = Instance.TempBuffer.Slice(0, reso);
            color = Instance.FrameBuffer.Slice(0, reso);
            alpha = Instance.AlphaBuffer.Slice(0, reso * layers);
            indexBuffer = Instance.IndexBuffer.Slice(0, reso * 2);
            return true;
        }

        private static Projector DefaultProjector(in ProjectorData data) => new Projector(in data);

        private CreateProjector GetCreateFunc(uint id)
        {
            if(id == 0) { return DefaultProjector;  }
            int index = _projectorCreators.IndexOf(id);
            if (index < 0)
            {
                Projections.Log(LogType.Error, $"Could not create new projector! (Type with tag ID {id:X8} was not found!)");
                return null;
            }
            return _projectorCreators[index].func;
        }

        internal static Projector GetNewProjector(in ProjectorData data)
        {
            if (Instance == null)
            {
                Projections.Log(LogType.Error, $"Could not create new projector! ({nameof(ProjectorSystem)} is not Initialized!)");
                return null;
            }

            Projector proj;
            var creator = Instance.GetCreateFunc(data.creatorTag);
            if(creator == null)
            {
                return null;
            }
            switch (data.id.type)
            {
                case ProjectorType.Custom:
                        proj = creator.Invoke(in data);
                        if (proj == null)
                        {
                            Projections.Log(LogType.Error, $"Could not create new projector! (Creation method returned null!)");
                            return null;
                        }
                        return proj;
                case ProjectorType.Player:
                    if(data.Owner < 0 || data.Owner >= Main.maxPlayers)
                    {
                        Projections.Log(LogType.Error, $"Could not create new projector! (Player index {data.Owner} is out of range!)");
                        return null;
                    }

                    if (!Main.gameMenu && !Main.player[data.Owner].active)
                    {
                        Projections.Log(LogType.Error, $"Could not create new projector! (Player at index {data.Owner} is not active!)");
                        return null;
                    }

                    proj = creator.Invoke(in data);
                    if (proj == null)
                    {
                        Projections.Log(LogType.Error, $"Could not create new projector! (Creation method returned null!)");
                        return null;
                    }
                    return proj;
                case ProjectorType.Tile:
                    if (Instance._tileLUT.Contains(data.TilePosition))
                    {
                        Projections.Log(LogType.Error, $"Could create new projector! (A tile projector at tile {data.TilePosition} already exists!)");
                        return null;
                    }

                    proj = creator.Invoke(in data);
                    if (proj == null)
                    {
                        Projections.Log(LogType.Error, $"Could not create new projector! (Creation method returned null!)");
                        return null;
                    }
                    return proj;
            }
            
            Projections.Log(LogType.Error, $"Could create new projector! (Type {data.id.type} is not supported for creation!)");
            return null;
        }
        internal static Projector AddProjector(in ProjectorData data)
        {
            Projector projector = GetNewProjector(in data);
            if(projector == null) { return null; }

            switch (data.id.type)
            {
               default:
                    return null;
                case ProjectorType.Player:
                    Main.player[data.Owner].PPlayer().SetProjector(projector, data.ProjectorIndex);
                    return projector;
                case ProjectorType.Tile:
                    if (Instance._tileProjectors.Add(projector))
                    {
                        Instance.AddAllInRegion(data.TilePosition, data.tileRegion);
                        return projector;
                    }
                    break;
                case ProjectorType.Custom:
                    if(projector.UniqueID == 0) { projector.UniqueID = ++CurrentID; }
                    if (Instance._customProjectors.Add(projector))
                    {
                        return projector;
                    }
                    break;
            }
            Projections.Log(LogType.Error, $"Could not create new projector! (Either type {data.id.type} is not supported, or there was an error when trying to add it)");
            return null;
        }
        internal static bool KillProjector(in ProjectorID id, bool eject, out Projector proj)
        {
            proj = null;
            if (Instance == null) { return false; }

            if(id.type < 0 || id.type >= ProjectorType.__Count || Instance._suppressKill[(int)id.type])
            {
                return false;
            }

            switch (id.type)
            {
                case ProjectorType.Tile:
                    {
                        int ind = Instance._tileLUT.IndexOf(id.tilePosition);
                        if (ind < 0)
                        {
                            Projections.Log(LogType.Error, $"Could not kill projector! (A tile projector at tile {id.tilePosition} doesn't exist!)");
                            return false;
                        }

                        proj = Instance._tileProjectors.SelectBy(Instance._tileLUT[ind].target.Reinterpret<Point, ulong>());
                        if (proj == null)
                        {
                            Projections.Log(LogType.Error, $"Could not kill projector! (A tile projector at tile {id.tilePosition} doesn't exist!)");
                            return false;
                        }

                        var tgt = Instance._tileLUT[ind].target;
                        Instance.RemoveByTarget(tgt);
                        Instance._tileProjectors.RemoveBy(tgt.Reinterpret<Point, ulong>());
                        break;
                    }

                case ProjectorType.Player:
                    {
                        if (id.owner < 0 || id.owner >= Main.maxPlayers)
                        {
                            Projections.Log(LogType.Error, $"Could not kill projector! (Player index {id.owner} is out of range!)");
                            return false;
                        }

                        if (!Main.player[id.owner].active)
                        {
                            Projections.Log(LogType.Error, $"Could not kill projector! (Player at index {id.owner} is not active!)");
                            return false;
                        }
                        var pPlayer = Main.player[id.owner].PPlayer();
                        if (pPlayer.TryGetProjector(id.projectorIndex, out proj))
                        {
                            pPlayer.SetProjector(null, id.owner);
                            break;
                        }
                        return false;
                    }
                case ProjectorType.Custom:
                    {
                        proj = Instance._customProjectors.SelectBy(id.uniqueID);
                        if (proj == null)
                        {
                            Projections.Log(LogType.Error, $"Could not kill projector! (A custom projector with ID {id.uniqueID} doesn't exist!)");
                            return false;
                        }

                        Instance._tileProjectors.RemoveBy(id.uniqueID);
                        break;
                    }
            }
            if(proj != null)
            {
                if (UISystem.Instance.CurrentProjector == proj)
                {
                    UISystem.CloseProjectorUI(false);
                }

                proj.Deactivate();
                if (eject && !Instance._suppressEject[(int)id.type])
                {
                    proj.EjectProjections();
                }
            }
            return proj != null;
        }
        internal static bool TryGetProjector(in ProjectorID id, out Projector proj)
        {
            proj = null;
            if (Instance == null) { return false; }

            switch (id.type)
            {
                case ProjectorType.Player:
                    {
                        if (id.owner < 0 || id.owner >= Main.maxPlayers)
                        {
                            Projections.Log(LogType.Error, $"Could not find projector! (Player index {id.owner} is out of range!)");
                            return false;
                        }

                        if (!Main.player[id.owner].active)
                        {
                            Projections.Log(LogType.Error, $"Could not find projector! (Player at index {id.owner} is not active!)");
                            return false;
                        }
                        Main.player[id.owner].PPlayer().TryGetProjector(id.projectorIndex, out proj);
                        break;
                    }
                case ProjectorType.Tile:
                    {
                        var tgt = Instance._tileLUT.IndexOf(id.tilePosition);
                        if (tgt < 0)
                        {
                            Projections.Log(LogType.Error, $"Could not find projector! (Tile {id.tilePosition} is not used for a Projector!)");
                            return false;
                        }
                        proj = Instance._tileProjectors.SelectBy(Instance._tileLUT[tgt].target.Reinterpret<Point, ulong>());
                        break;
                    }
                case ProjectorType.Custom:
                    {
                        proj = Instance._customProjectors.SelectBy(id.uniqueID);
                        if (proj == null)
                        {
                            Projections.Log(LogType.Error, $"Could not find projector! (Projector with ID {id.uniqueID} is not used!)");
                            return false;
                        }
                        break;
                    }
            }

            if(proj == null)
            {
                Projections.Log(LogType.Error, $"Could not find projector! (Something went wrong when trying to find one!)");
                return false;
            }
            return true;
        }

        internal static bool EjectSuppressed(ProjectorType type) => 
            Instance != null && type >= 0 && type < ProjectorType.__Count && Instance._suppressEject[(int)type];

        internal static bool KillSuppressed(ProjectorType type) =>
            Instance != null && type >= 0 && type < ProjectorType.__Count && Instance._suppressKill[(int)type];

        internal static void UpdateCustom(uint oldId, uint newId)
        {
            if (Instance == null) { return; }

            int ind = Instance._customProjectors.IndexOf(oldId);
            if(ind > -1)
            {
                Instance._customProjectors[ind].UniqueID = newId;
                Instance._customProjectors.Update(ind);
            }
        }

        internal static void DrawProjectors<T>(IList<T> instances, SpriteBatch batch = null) where T : Projector
        {
            foreach (var inst in instances)
            {
                DrawProjector(inst, batch);
            }
        }
        internal static void DrawProjector(Projector projector, SpriteBatch batch = null)
        {
            if (projector == null || Instance == null || !projector.CanRender) { return; }
            batch = projector.OverrideBatch;
            batch ??=  Instance._spriteBatch;

            var tex = projector.Texture;
            if (tex?.CanDraw ?? false) { return; }

            ref var settings = ref projector.Settings;
            if (settings.scale <= 0.001f) { return; }

            Color32 tintColor = projector.ProjectorTint;
            if (tintColor.a < 1) { return; }
            Color32 lightColor = Color32.Multiply(projector.ProjectorColor, tintColor);

            SpriteEffects effects = SpriteEffects.None;
            if (settings.FlipX) { effects |= SpriteEffects.FlipHorizontally; }
            if (settings.FlipY) { effects |= SpriteEffects.FlipVertically; }

            ProjectorTexFlags flags = (ProjectorTexFlags)(~(settings.tileHideType));
          
            MathLow.Premultiply(ref lightColor);
            MathLow.Premultiply(ref tintColor);

            Rectangle dst = projector.ProjectionArea;
            Rectangle src = default;
            for (int i = 0; i < tex.LayerCount; i++)
            {
                if(projector.GetLayerTextureUV(i, ref src))
                {
                    tex.DrawLayer(batch, i, flags, ref src, ref dst, lightColor, tintColor, effects, Vector2.Zero, 0.0f);
                }
            }
        }

        internal static bool TryGetProjectorTypeName(uint id, out ReadOnlySpan<char> name)
        {
            if(id == 0) { name = "Default Projector"; return true; }
            int index = Instance != null ? Instance._projectorCreators.IndexOf(id) : -1;


            if(index < 0)
            {
                name = "<Unknown Projector>";
                return false;
            }
            name = Instance._projectorCreators[index].tagName;
            return true;
        }

        internal static bool RegisterProjectorType(ReadOnlySpan<char> tagName, CreateProjector method)
        {
            if (Instance == null)
            {
                Projections.Log(LogType.Error, $"Could not register projector type! ({nameof(ProjectorSystem)} is not Initialized!)");
                return false;
            }

            if (method == null)
            {
                Projections.Log(LogType.Error, $"Could not register projector type! (Given method is null!)");
                return false;
            }

            uint tagID = CRC32.Calculate(tagName);
            if (tagID == 0)
            {
                Projections.Log(LogType.Error, $"Given projector tag ID was 0! (Given tag was {tagName})");
                return true;
            }

            if (Instance._projectorCreators.Add(
                      new CustomProjGetter()
                      {
                          tag = tagID,
                          tagName = tagName.ToString(),
                          func = method,
                      }))
            {
                return true;
            }
            Projections.Log(LogType.Error, $"Could not register a Projector Type with Tag '{tagName}' [{tagID:X8}], because it already exists!");
            return false;
        }
        internal static bool UnregisterProjectorType(ReadOnlySpan<char> tagName)
        {
            if (Instance == null)
            {
                Projections.Log(LogType.Error, $"Could not unregister projector type! ({nameof(ProjectorSystem)} is not Initialized!)");
                return false;
            }

            uint tagID = CRC32.Calculate(tagName);
            if(tagID == 0)
            {
                Projections.Log(LogType.Error, $"Cannot unregister a projector type with ID 0! (Tag was: {tagName})");
                return false;
            }

            if (Instance._projectorCreators.RemoveBy(tagID))
            {
                return true;
            }
            Projections.Log(LogType.Error, $"Could not unregister a Projector Type with Tag '{tagName}' [{tagID:X8}], because it wasn't found!");
            return false;
        }

        internal static bool RegisterProjector(Projector projector)
        {
            if (Instance == null)
            {
                Projections.Log(LogType.Error, $"Could not register projector! ({nameof(ProjectorSystem)} is not Initialized!)");
                return false;
            }

            if (projector == null)
            {
                Projections.Log(LogType.Error, $"Could not register projector! (Given project is null!)");
                return false;
            }
            
            if (projector.Type != ProjectorType.Custom)
            {
                Projections.Log(LogType.Error, $"Could not register projector! (Given project is not an additional projector! [{projector.Type}])");
                return false;
            }

            if (Instance._customProjectors.Add(projector))
            {
                return true;
            }
            Projections.Log(LogType.Warning, $"Could not register projector! (Given project is already registered!)");
            return false;
        }
        internal static bool UnregisterProjector(Projector projector)
        {
            if (Instance == null)
            {
                Projections.Log(LogType.Error, $"Could not unregister projector! ({nameof(ProjectorSystem)} is not Initialized!)");
                return false;
            }

            if (projector == null)
            {
                Projections.Log(LogType.Error, $"Could not unregister projector! (Given project is null!)");
                return false;
            }

            if (projector.Type != ProjectorType.Custom)
            {
                Projections.Log(LogType.Error, $"Could not register projector! (Given project is not an additional projector! [{projector.Type}])");
                return false;
            }

            if (Instance._customProjectors.Remove(projector))
            {
                return true;
            }
            Projections.Log(LogType.Warning, $"Could not unregister projector! (Given project was not registered!)");
            return false;
        }

        internal static void ValidateProjectors()
        {
            if (!IsActive) { return; }

            foreach (var proj in Instance._tileProjectors)
            {
                proj.Validate();
            }

            foreach (var plr in Main.player)
            {
                if (plr.active)
                {
                    plr.PPlayer().ValidateProjectors();
                }
            }

            foreach (var proj in Instance._customProjectors)
            {
                proj.Validate();
            }
        }

        public override void Load()
        {
            _fxVisualizer.Load();
            _projectorCreators.Clear();
            _frameBuffer.Resize(FRAME_BUFFER_SIZE * 2);
            _alphaBuffer.Resize((FRAME_BUFFER_SIZE * StreamedProjection.MAX_LAYERS) + FRAME_BUFFER_SIZE);
            _audioBufferPool.Init(true);
            CommonIL.Init();
        }

        public override void Unload()
        {
            _frameBuffer.Release();
            _alphaBuffer.Release();
            _audioBufferPool.Clear();   
            _projectorCreators.Clear();
            _isActive = false;
            CommonIL.Deinit();
        }

        internal static void RefreshAudioRange(Projector projector)
        {
            Instance?._fxVisualizer.SetTarget(projector);
        }

        public override void PreUpdateTime()
        {
            _prevFrame = _curFrame;
            _curFrame = (float)_frameDelta.Elapsed.TotalSeconds;

            float mColor = Main.mouseTextColor * (1.0f / 255f);
            MasterColor = new Color((byte)(255f * mColor), (byte)(Main.masterColor * 200f * mColor), 0, Main.mouseTextColor);
            if (Main.dedServ || Main.netMode == NetmodeID.Server) { return; }

            _essNorm = PMath.InverseLerp(0.7f, 1.0f, Main.essScale);

            Projections.Instance.Update();
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i].Clear();
            }

            var camPos = Main.ViewPosition;
            var view = Main.ViewSize;

            var camPosT = camPos.ToTileCoordinates16();
            var viewT = view.ToTileCoordinates16();

            int cMinX = (int)camPos.X;
            int cMinY = (int)camPos.Y;

            int cMaxX = (int)(camPos.X + view.X);
            int cMaxY = (int)(camPos.Y + view.Y);

            var servConf = ProjectionsServerConfig.Instance;
            int maxLayer = servConf.AllowProjectionsInFrontOfPlayer ? (int)DrawLayer.__Count - 1 : (int)DrawLayer.__Count - 2;
            Rectangle lightRect = new Rectangle(camPosT.X - 4, camPosT.Y + 4, viewT.X + 4, viewT.Y + 4);
            foreach (var proj in _tileProjectors)
            {
                UpdateProjector(proj, ref lightRect, maxLayer, cMinX, cMinY, cMaxX, cMaxY);
            }

            foreach (var proj in _customProjectors)
            {
                UpdateProjector(proj, ref lightRect, maxLayer, cMinX, cMinY, cMaxX, cMaxY);
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active) { continue; }
                var pPlr = Main.player[i]?.PPlayer();
                if (pPlr?.CanProject ?? false)
                {
                    pPlr.UpdateProjectors((Projector proj) => UpdateProjector(proj, ref lightRect, maxLayer, cMinX, cMinY, cMaxX, cMaxY));
                }
            }

            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i].Sort(SortByProjectorDepth);
            }
            _audioBufferPool.Tick();
        }

        internal void ClearProjectors()
        {
            for (int i = 0; i < _tileProjectors.Count; i++)
            {
                _tileProjectors[i].Clear();
            }

            for (int i = 0; i < _customProjectors.Count; i++)
            {
                _customProjectors[i].Clear();
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    var pPlr = Main.player[i].PPlayer();
                    for (int j = 0; j < pPlr.ProjectorCount; j++)
                    {
                        if(pPlr.TryGetProjector(j, out var proj))
                        {
                            proj.Clear();
                        }
                    }
                }
            }

            _tileLUT.Clear();
            _tileProjectors.Clear();
            _customProjectors.Clear();
        }

        public override void OnWorldLoad()
        {
            _frameDelta.Restart();
            _prevFrame = 0;
            _curFrame = 0;
            _fxVisualizer.SetTarget(null);
            ClearProjectors();
            base.OnWorldLoad();
            Projections.LoadAllProjections();
            UISystem.CloseProjectorUI(false, true);
        }
        public override void OnWorldUnload()
        {
            _fxVisualizer.SetTarget(null);
            _frameDelta.Stop();
            ClearProjectors();
            base.OnWorldUnload();
            Projections.UnloadAllProjections();
            UISystem.CloseProjectorUI(false, true);
        }

        private static TagCompound ProjectorToTag(Projector projector)
        {
            TagCompound tag = new TagCompound();
            TagCompound tagData = new TagCompound();
            projector.Data.Save(tag);
            projector.Save(tagData);
            tag.Assign("Data", tagData);
            return tag;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            base.SaveWorldData(tag);
            List<TagCompound> projectors = new List<TagCompound>();

            foreach (var item in _tileProjectors)
            {
                if(item == null) { continue; }
                projectors.Add(ProjectorToTag(item));
            }

            foreach (var item in _customProjectors)
            {
                if (item == null) { continue; }
                projectors.Add(ProjectorToTag(item));
            }
            tag.Add("Projectors", projectors);
        }
        public override void LoadWorldData(TagCompound tag)
        {
            ClearProjectors();

            ProjectorData data = default;
            CurrentID = 0;
            if (tag.TryGetSafe("Projectors", out IList<TagCompound> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var projTag = list[i];
                    if (data.Load(projTag) && projTag.TryGetSafe<TagCompound>("Data", out var dataTag))
                    {
                        var proj = AddProjector(in data);
                        if(data.Type == ProjectorType.Custom)
                        {
                            CurrentID = Math.Max(CurrentID, data.UniqueID);
                        }
                        if (proj != null)
                        {
                            proj.Load(dataTag);
                        }
                    }
                    else
                    {
                        Projections.Log(LogType.Error, $"Could not load Projector! (Type: {data.id.type}, ID: {data.id.id:X16})");
                    }
                }
            }
            else
            {
                Projections.Log(LogType.Error, "Failed to load Projector data!");
            }
        }

        internal void EraseAllProjectors(bool eject, bool noSend)
        {
            _suppressKill[(int)ProjectorType.Tile] = true;
     
            for (int i = 0; i < _tileProjectors.Count; i++)
            {
                var projector = _tileProjectors[i];
                if (eject)
                {
                    projector.EjectProjections();
                }
                projector.Deactivate();

                var tile = Framing.GetTileSafely(projector.TilePosition);
                if (tile.HasTile && BaseProjectorTile.ProjectorTileLUT.Contains(tile.TileType))
                {
                    WorldGen.KillTile(projector.TilePosition.X, projector.TilePosition.Y, false, false, true);
                }
            }
            _suppressKill[(int)ProjectorType.Tile] = false;
            _tileProjectors.Clear();
            _tileLUT.Clear();

            if (Main.netMode != NetmodeID.SinglePlayer && !noSend)
            {
                ProjectionNetUtils.SendEraseProjectors(Main.myPlayer, false);
            }
        }

        internal static void DrawBehindWalls()
        {
            if (Instance == null) { return; }
            Instance._spriteBatch ??= new SpriteBatch(Main.graphics.GraphicsDevice);

            Instance._spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawProjectors(Instance._layers[(int)DrawLayer.BehindWalls], Instance._spriteBatch);
        }
        internal static void DrawBehindTiles()
        {
            if (Instance == null) { return; }
            DrawProjectors(Instance._layers[(int)DrawLayer.BehindTiles], Instance._spriteBatch);
        }
        internal static void DrawAfterTiles()
        {
            if (Instance == null) { return; }
            DrawProjectors(Instance._layers[(int)DrawLayer.AfterTiles], Instance._spriteBatch);
        }
        internal static void DrawAfterPlayers()
        {
            if (Instance == null) { return; }
            DrawProjectors(Instance._layers[(int)DrawLayer.AfterPlayers], Instance._spriteBatch);
            Instance._spriteBatch.End();
            Instance._fxVisualizer.Draw(Instance._spriteBatch, Instance.FrameDelta);
        }

        private static int SortByProjectorDepth(Projector lhs, Projector rhs)
        {
            return lhs.Settings.drawOrder.CompareTo(rhs.Settings.drawOrder);
        }
        private void UpdateProjector(Projector projector, ref Rectangle lightRect, int maxLayer, int cMinX, int cMinY, int cMaxX, int cMaxY)
        {
            projector.Update();

            int layer = (int)projector.Settings.drawLayer;
            layer = Utils.Clamp(layer, 0, maxLayer);
            if (projector.CanRender && projector.Overlaps(cMinX, cMaxX, cMinY, cMaxY))
            {
                var tilePos = projector.TilePosition;
                bool isValid = lightRect.Contains(tilePos.X + 1, tilePos.Y);

                if (isValid)
                {
                    projector.ProjectorColor = Lighting.GetColor(tilePos.X + 1, tilePos.Y);
                }

                projector.IsVisible = true;
                projector.DrawLighting();
                _layers[layer].Add(projector);
                return;
            }
            projector.IsVisible = false;
        }

        private bool TryGetOwningProjector(Point point, out int owning)
        {
            int index = _tileLUT.IndexOf(point);
            if (index < 0)
            {
                owning = -1; return false;
            }
            owning = _tileProjectors.IndexOf(_tileLUT[index].target.Reinterpret<Point, ulong>());
            return owning > -1;
        }
        private bool TryGetProjectorByPosition(Vector2 position, out Projector inst)
        {
            inst = _tileProjectors.SelectBy(position.ToTileCoordinates().Reinterpret<Point, ulong>());
            return inst != null;
        }
        private bool TryGetProjectorByTile(int x, int y, out Projector inst, bool exact = false)
        {
            if (exact)
            {
                inst = _tileProjectors.SelectBy(new Point(x, y).Reinterpret<Point, ulong>());
                return inst != null;

            }

            inst = null;
            if (TryGetOwningProjector(new Point(x, y), out int ind))
            {
                inst = _tileProjectors[ind];
            }
            return inst != null;
        }

        private void AddAllInRegion(Point point, Point size)
        {
            for (int yI = 0, yY = point.Y; yI < size.Y; yI++, yY++)
            {
                for (int xI = 0, xX = point.X; xI < size.X; xI++, xX++)
                {
                    _tileLUT.Add(new Redirect()
                    {
                        self = new Point(xX, yY),
                        target = point
                    });
                }
            }
        }

        private static bool IsTarget(in Redirect redirect, Point point)
        {
            return redirect.target == point;
        }

        private void RemoveByTarget(Point target)
        {
            _tileLUT.RemoveIf((in Redirect re) => IsTarget(in re, target));
        }

        private static int SortRedirect(in Redirect lhs, in Redirect rhs)
        {
            return SortRedirect(in lhs, in rhs.self);
        }
        private static int SortRedirect(in Redirect lhs, in Point rhs)
        {
            return lhs.self.CompareTo(rhs);
        }

        private struct Redirect : IEquatable<Redirect>, IComparable<Redirect>, IComparable<Point>
        {
            public Point self;
            public Point target;

            public int CompareTo(Redirect other)
            {
                return this.CompareTo(other.self);
            }

            public int CompareTo(Point other)
            {
                Span<int> tempA = stackalloc int[2]
                {
                self.X,
                self.Y
            };
                Span<int> tempB = stackalloc int[2]
                {
                other.X,
                other.Y
            };
                return tempA.ReadAs<int, ulong>().CompareTo(tempB.ReadAs<int, ulong>());
            }

            public override bool Equals(object obj)
            {
                return obj is Redirect redirect && Equals(redirect);
            }

            public bool Equals(Redirect other)
            {
                return self.Equals(other.self);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(self);
            }

            public static bool operator ==(Redirect left, Redirect right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Redirect left, Redirect right)
            {
                return !(left == right);
            }
        }
        private class Visualier
        {
            public const float DURATION = 12.0f;
            public const float ALPHA_DURATION = 0.75f;

            public bool IsInUse => projector != null;

            public float time = 0.0f;
            public Projector projector = null;
            public Asset<Effect> effect;

            public void Load()
            {
                if(Main.netMode == NetmodeID.Server) { return; }
                effect = ModContent.Request<Effect>("Projections/Content/Shaders/AudioRangeVisualizer");
            }

            public void SetTarget(Projector projector)
            {
                time = DURATION;
                _curPos = this.projector == null && projector != null ? projector.Position.ToTileCoordinates().ToWorldCoordinates() : _curPos;
                this.projector = projector;
            }

            private Vector2 _curPos;
            public bool Draw(SpriteBatch batch, float delta)
            {
                if (Main.netMode == NetmodeID.Server) { return false; }
                var fx = effect.Value;
                if(projector == null || fx == null)
                {
                    return false;
                }
                time -= delta;

                if(time <= 0)
                {
                    projector = null;
                    time = 0;
                    return false;
                }
                ref readonly var settings = ref projector.Settings;
                Vector2 projPos = projector.Position.ToTileCoordinates().ToWorldCoordinates();
                _curPos = Vector2.SmoothStep(_curPos, projPos, delta * 25);
                Vector2 position = _curPos - Main.screenPosition;

                float minPix = settings.audioRangeMin * 16.0f;
                float maxPix = Math.Max(settings.audioRangeMax * 16.0f, 16.0f);

                fx.Parameters["uAlpha"].SetValue(Math.Min(time / ALPHA_DURATION, 1.0f) * 0.25f);
                fx.Parameters["uInnerEdge"].SetValue(Math.Min(minPix / maxPix, 0.99f));
                fx.Parameters["uSizePix"].SetValue(Math.Max(maxPix * 2, 1.0f));

                float s = maxPix;

                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, Main.Rasterizer, fx, Main.GameViewMatrix.TransformationMatrix);
                batch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(position.X - s), (int)(position.Y - s), (int)maxPix * 2, (int)maxPix * 2), Color.White);
                batch.End();
                return true;
            }
        }
    }
}
