using Microsoft.Xna.Framework;
using Projections.Content.Items;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Audio;
using Projections.Core.Utilities;
using Projections.Core.Maths;
using Projections.Common.Netcode;
using Projections.Common.Configs;
using Projections.Core.Textures;
using Projections.Core.Systems;

namespace Projections.Common.ProjectorTypes
{
    public class Projector
    {
        public PRarity Rarity => ActiveSlot.Rarity;
        public bool IsActive
        {
            get => _settings.IsActive;
            set => _settings.IsActive = value;
        }
        public bool IsPlaying
        {
            get => _settings.IsActive && _settings.IsPlaying && !ActiveSlot.IsEmpty;
            set => _settings.IsPlaying = value;
        }
        public bool IsPaused => !_settings.IsPlaying && _settings.IsActive;

        public ProjectorType Type => _data.id.type;

        public Vector2 Alignment
        {
            get => _settings.alignment;
            set => _settings.alignment = value;
        }
        public float Rotation
        {
            get => _settings.rotation;
            set => _settings.rotation = value;
        }

        public TimeSpan TimeProjected => TimeSpan.FromSeconds(_data.timeInUse);
        public float TimeProjectedSec => _data.timeInUse;
        public int Loops => _loops;

        public int SlotCount
        {
            get
            {
                ValidateSlotSize();
                return _data.slotCount;
            }
        }
        public bool CanRender => IsActive && ActiveSlot.CanDraw;
        public bool IsVisible
        {
            get;
            internal set;
        }

        public ref ProjectorData Data => ref _data;
        public int OwningPlayer
        {
            get => _data.id.type == ProjectorType.Player ? _data.Owner : -1;
            set
            {
                if (_data.id.type != ProjectorType.Player)
                {
                    Projections.Log(LogType.Warning, $"Setting the Owner is not supported for Projectors of type {_data.id}");
                    return;
                }
                _data.id.owner = (short)value;
            }
        }

        public uint CreatorTag
        {
            get => _data.creatorTag;
            set => _data.creatorTag = value;
        }
        public ulong UniqueID
        {
            get => _data.id.id;
            set => _data.id.id = value;
        }

        public RectF ProjectionArea
        {
            get
            {
                float width = ActiveSlot.Width * _settings.scale;
                float height = ActiveSlot.Height * _settings.scale;
                var align = _settings.alignment;
                Vector2 offset = new Vector2(width * -align.X, height * align.Y);
                return new RectF(offset + Hotspot, new Vector2(width, height));
            }
        }

        public Point TilePosition
        {
            get
            {
                return _data.id.type switch
                {
                    ProjectorType.Tile => _data.TilePosition,
                    _ => Position.ToTileCoordinates(),
                };
            }
        }
        public Point TileRegion
        {
            get
            {
                return _data.id.type switch
                {
                    ProjectorType.Tile => _data.tileRegion,
                    _ => new Point(1, 1),
                };
            }
        }

        public Vector2 Position
        {
            get
            {
                switch (_data.Type)
                {
                    default: return default;
                    case ProjectorType.Custom:
                        return _data.position;
                    case ProjectorType.Player:
                        if (_data.Owner < 0 || _data.Owner >= Main.maxPlayers || !Main.player[_data.Owner].active) { return default; }
                        return Main.player[_data.Owner].Center;
                    case ProjectorType.Tile:
                        return _data.TilePosition.ToWorldCoordinates();
                }
            }
            set
            {
                switch (_data.Type)
                {
                    default:
                        Projections.Log(LogType.Warning, $"Projector position setting not supported with ProjectorType {_data.Type}");
                        return;
                    case ProjectorType.Custom:
                        _data.position = value;
                        // TODO: Maybe some updating stuff?
                        break;
                }
            }
        }
        public Vector2 Hotspot
        {
            get => Position + _data.hotspot;
            set => _data.hotspot = value;
        }

        public Vector2 ProjectorCenter
        {
            get => Position + TileRegion.ToWorldCoordinates() * 0.5f;
        }

        public Color32 ProjectorTint
        {
            get
            {
                var conf = ProjectionsClientConfig.Instance;
                Color32 plrTint = conf?.PlayerProjectionTint ?? Color32.White;
                Color32 tint = _settings.tint;
                if (Main.myPlayer != OwningPlayer)
                {
                    tint = Color32.Multiply(tint, plrTint);
                }
                return tint;
            }
            set
            {
                _settings.tint = value;
            }
        }
        public Color32 ProjectorColor
        {
            get => _projectorColor;
            set => _projectorColor = value;
        }

        public virtual float Volume
        {
            get
            {
                var conf = ProjectionsClientConfig.Instance;
                if (conf == null) { return 0; }

                float finalVol = _volume * (conf.Volume * 0.01f) * _settings.volume;

                switch (ActiveSlot.AudioType)
                {
                    case AudioType.SFX:
                        finalVol *= Main.soundVolume;
                        break;
                    case AudioType.Ambient:
                        finalVol *= Main.ambientVolume;
                        break;
                    case AudioType.Music:
                        finalVol *= Main.musicVolume;
                        break;
                }

                if (Main.myPlayer != OwningPlayer)
                {
                    finalVol *= conf.PlayerVolume;
                }
                return finalVol;
            }
        }

        public float Time => _time;

        public AudioBuffer AudioBuffer => _audioBuffer;
        public ProjectorTexture Texture => _texture;

        public virtual SpriteBatch OverrideBatch => null;

        public ref ProjectorSettings Settings => ref _settings;
        public ref ProjectorSlot ActiveSlot => ref GetSlot(_settings.activeSlot);
        public int ActiveSlotIndex => _settings.activeSlot;

        // Information/Data
        private ProjectorData _data;
        private ProjectorSettings _settings;
        private ProjectorSlot[] _slots = new ProjectorSlot[0];

        // Resources
        private ProjectorTexture _texture;
        private AudioBuffer _audioBuffer;

        // Runtime
        private AudioSource _sfx;
        private Color32 _projectorColor;
        private float _time;
        private float _volume;
        private int _loops;

        internal Projector(in ProjectorData data)
        {
            _data = data;
            _sfx = new AudioSource();
            _sfx.OnBufferEnd = OnBufferEnd;
            _slots = new ProjectorSlot[Math.Max(1, data.slotCount)];
            _texture = new ProjectorTexture();
            _settings.Reset();
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Setup(ProjectionIndex.Zero, 0);
                _slots[i].Reset();
            }
        }

        public ref ProjectorSlot GetSlot(int index)
        {
            ValidateSlotSize();
            return ref _slots[Math.Min(index, _slots.Length - 1)];
        }

        public bool Contains(Vector2 position)
            => ProjectionArea.Contains(position);

        public bool Overlaps(Vector2 position, Vector2 size)
            => ProjectionArea.Overlaps(position, size);

        public bool Overlaps(float x, float y, float width, float height)
            => ProjectionArea.Overlaps(new Vector2(x, y), new Vector2(width, height));

        public bool Overlaps(ref RectF rect)
            => ProjectionArea.Overlaps(ref rect);

        public void Save(TagCompound tag)
        {
            _settings.Save("Settings", tag);
            tag.Assign("ProjectorColor", _projectorColor);

            ValidateSlotSize();
            List<TagCompound> slots = new List<TagCompound>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                var tagC = new TagCompound();
                _slots[i].Save(tagC);
                slots.Add(tagC);
            }
            tag.Assign("Slots", slots);

            try
            {
                TagCompound custom = new TagCompound();
                SaveCustom(custom);
                tag.Add("CustomData", custom);
            }
            catch (Exception e)
            {
                Projections.Log(LogType.Error, $"There was an error while saving projector Custom Data!\n\n{e.Message}\n\n{e.StackTrace})");
            }
        }
        public void Load(TagCompound tag)
        {
            ValidateSlotSize();

            if(tag.TryGetSafe<TagCompound>("Settings", out var set))
            {
                _settings.Load(set);
            }
        
            _projectorColor = tag.GetColor32("ProjectorColor", Color32.White);

            if (tag.TryGetSafe<IList<TagCompound>>("Slots", out var slotTags))
            {
                int count = Math.Min(slotTags.Count, SlotCount);
                for (int i = count; i < SlotCount; i++)
                {
                    _slots[i].Reset();
                }

                for (int i = 0; i < count; i++)
                {
                    _slots[i].Load(slotTags[i]);
                }
            }

            if (tag.TryGetSafe<TagCompound>("CustomData", out var custom))
            {
                try
                {
                    LoadCustom(custom);
                }
                catch (Exception e)
                {
                    Projections.Log(LogType.Error, $"There was an error while loading projector Custom Data!\n\n{e.Message}\n\n{e.StackTrace})");
                }
            }
        }

        public void Serialize(BinaryWriter bw, SerializeType sType)
        {
            _settings.Serialize(bw);
            bw.Write(_projectorColor);

            bw.Write(sType);
            if (sType != SerializeType.Partial)
            {
                ValidateSlotSize();
                for (int i = 0; i < SlotCount; i++)
                {
                    _slots[i].Serialize(bw);
                }
            }

            ushort customLen = 0;
            bw.Write(customLen);
            var stream = bw.BaseStream;
            long startPos = stream.Position;
            try
            { // Serialize Potential Custom Data and it's length so we can skip over it after it's deserialized

                SerializeCustom(bw, sType);
                long endPos = stream.Position;
                customLen = (ushort)(endPos - startPos);
                stream.Seek(startPos - 2, SeekOrigin.Begin);
                bw.Write(customLen);
                stream.Seek(endPos, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                Projections.Log(LogType.Error, $"There was an error while serializing projector Custom Data!\n\n{e.Message}\n\n{e.StackTrace})");
                stream.SetLength(startPos);
            }
        }
        public void Deserialize(BinaryReader br, out SerializeType sType)
        {
            _settings.Deserialize(br);
            _projectorColor = br.Read<Color32>();
            sType = br.Read<SerializeType>();
            if (sType != SerializeType.Partial)
            {
                ValidateSlotSize();
                for (int i = 0; i < SlotCount; i++)
                {
                    _slots[i].Deserialize(br);
                }
            }

            ushort customLength = br.ReadUInt16();
            var stream = br.BaseStream;
            long startPos = stream.Position;
            long endPos = startPos + customLength;
            try
            { // Deserializing Custom projector data and then skipping to the intended end, in case the custom data isn't fully read etc
                DeserializeCustom(br, sType);
                stream.Seek(endPos, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                Projections.Log(LogType.Error, $"There was an error while deserializing projector Custom Data!\n\n{e.Message}\n\n{e.StackTrace})");
                stream.Seek(endPos, SeekOrigin.Begin);
            }
        }

        internal static int SkipSettings(ref ReadOnlySpan<byte> data, out bool isPlaying, out int activeSlot, out SerializeType sType)
        {
            int oLen = data.Length;
            ProjectorSettings settings = default;
            settings.Deserialize(ref data);
            isPlaying = settings.IsPlaying && settings.IsActive;
            activeSlot = settings.activeSlot;
            data.Read<Color32>(out data);
            sType = data.Read<SerializeType>(out data);
            return oLen - data.Length;
        }

        public int TryPushProjection(ProjectionItem projection, bool allowEmpty = false)
        {
            if (projection.Item.stack < 1 || projection.IsEmpty) { return 0; }
            int pushed = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                switch (TryPushToSlot(projection, i, allowEmpty))
                {
                    case 2:
                        return 2;
                    case 1:
                        pushed = 1;
                        break;
                }
            }
            return pushed;
        }
        public int TryPushToSlot(ProjectionItem item, int slot, bool allowIfEmpty = false)
        {
            if (slot < 0 || slot >= SlotCount || item == null || item.Item.stack < 1 || item.IsEmpty) { return 0; }

            ref var pSlot = ref _slots[slot];
            if (pSlot.IsEmpty)
            {
                if (!allowIfEmpty) { return 0; }
                int itemsToMove = Math.Min(Item.CommonMaxStack, item.Item.stack);

                pSlot.Setup(item.Index, itemsToMove);
                pSlot.Reset();
                item.Item.stack -= itemsToMove;

                if (item.Item.stack <= 0)
                {
                    item.Item.TurnToAir();
                }
                return item.Item.stack <= 0 ? 2 : 1;
            }

            if (pSlot.Index == item.Index)
            {
                int itemsToMove = Math.Max(Math.Min(Math.Min(Item.CommonMaxStack, item.Item.stack), Item.CommonMaxStack - pSlot.Stack), 0);
                pSlot.Stack += itemsToMove;
                item.Item.stack -= itemsToMove;
                if (item.Item.stack <= 0)
                {
                    item.Item.TurnToAir();
                }
                return item.Item.stack <= 0 ? 2 : 1;
            }
            return 0;
        }

        public virtual void Validate()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _slots[i].Validate();
            }
        }

        public virtual void ValidateActive(bool markForRedraw, bool markForAudioSync)
        {
            ActiveSlot.Validate();
        }

        public virtual void DrawLighting()
        {
            if (Main.netMode == NetmodeID.Server) { return; }
            var emit = _settings.EmitLight;
            if (emit != LightSourceLayer.None && IsVisible)
            {
                // TODO: Rewrite this entirely when possible
            }
        }

        public virtual void Update()
        {
            float delta = ProjectorSystem.Instance?.FrameDelta ?? 0.0f;
            if (IsPlaying)
            {
                if (_sfx.IsPlaying && _sfx.SetVolume(Volume) && _sfx.Volume <= 0.0f)
                {
                    _sfx.Stop();
                }
                _sfx.Update();
                _time += delta;
                _data.timeInUse += delta;
            }
        }

        public bool Play(int? slot = null)
        {
            if (slot != null)
            {
                SetActiveSlot(slot.Value);
                goto reset;
            }

            if (IsPaused)
            {
                _settings.IsPlaying = true;
                _sfx.Resume();
                return true;
            }

            _settings.IsActive = true;
            _settings.IsPlaying = true;
            if (!ActiveSlot.IsValid) { return false; }

        reset:
            _time = 0;
            _loops = 0;
            StopAudio();

            if (IsPlaying)
            {

            }

            return IsPlaying;
        }

        public bool Stop()
        {
            if (!_settings.IsPlaying)
            {
                return false;
            }

            _settings.IsPlaying = false;
            _sfx.Pause();
            return true;
        }

        public bool Deactivate()
        {
            if (!IsActive) { return false; }
            IsActive = false;
            StopAudio();
            return true;
        }

        public void Clear()
        {
            // TODO: Possibly clear/stop other stuff and call some overrideable method for Clearing
            StopAudio();
        }

        public int EjectProjections()
        {
            int ejectCount = 0;
            var pos = Hotspot;
            for (int i = 0; i < _slots.Length; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Index.IsValidID() && slot.Stack > 0)
                {
                    ProjectionNetUtils.SpawnProjectionItem<ProjectionItem>(slot.Index, pos, slot.Stack);
                    slot.Clear();
                    ejectCount++;
                }
            }
            return ejectCount;
        }

        public int SetActiveSlot(int index, bool exact = false)
        {
            int cur = _settings.activeSlot;
            if (cur == index) { return cur; }

            if (exact)
            {
                index = Utils.Clamp(index, 0, SlotCount - 1);
                _settings.activeSlot = index;
                _time = 0;
                _loops = 0;
                StopAudio();

                ActiveSlot.ResetState();

                if (IsPlaying)
                {
                    SubmitAudio(0);
                }
                SubmitFrame();
                return _settings.activeSlot;
            }

            if (!FindNextValidSlot(index, out index) || index == cur)
            {
                return cur;
            }

            _settings.activeSlot = index;
            _time = 0;
            _loops = 0;
            StopAudio();

            ActiveSlot.ResetState();

            if (IsPlaying)
            {
                SubmitAudio(0);
            }
            SubmitFrame();
            return _settings.activeSlot;
        }

        public virtual bool GetLayerTextureUV(int layer, ref Rectangle uvs)
        {
            ref var slot = ref ActiveSlot;
            if (slot.CanDraw)
            {
                uvs.X = 0;
                uvs.Y = 0;
                uvs.Width = slot.Width;
                uvs.Height = slot.Height;
                return true;
            }
            return false;
        }

        public virtual bool Draw(SpriteBatch batch)
        {
            batch = OverrideBatch ?? batch;
            if (batch == null) { return false; }

            var tex = Texture;
            if (tex?.CanDraw ?? false) { return false; }

            ref var settings = ref _settings;
            if (settings.scale <= 0.001f) { return false; }

            Color32 tintColor = ProjectorTint;
            if (tintColor.a < 1) { return false; }
            Color32 lightColor = Color32.Multiply(ProjectorColor, tintColor);

            SpriteEffects effects = SpriteEffects.None;
            if (settings.FlipX) { effects |= SpriteEffects.FlipHorizontally; }
            if (settings.FlipY) { effects |= SpriteEffects.FlipVertically; }

            ProjectorTexFlags flags = (ProjectorTexFlags)~settings.tileHideType;

            MathLow.Premultiply(ref lightColor);
            MathLow.Premultiply(ref tintColor);

            var area = ProjectionArea;
            var offset = Main.drawToScreen ? Vector2.Zero : Main.screenPosition;

            area.x -= offset.X;
            area.y -= offset.Y;


            Rectangle dst = area;
            Rectangle src = default;
            for (int i = 0; i < tex.LayerCount; i++)
            {
                if (GetLayerTextureUV(i, ref src))
                {
                    tex.DrawLayer(batch, i, flags, ref src, ref dst, lightColor, tintColor, effects, Vector2.Zero, 0.0f);
                }
            }

            return true;
        }

        protected virtual void OnBufferEnd(long position)
        {
            if (!IsPlaying || !ActiveSlot.HasAudio) { return; }

            var proj = ActiveSlot.Projection;
            ref readonly var aud = ref proj.AudioInfo;

            if (proj.AnimationMode == AnimationMode.LoopAudio)
            {

            }

            if (ActiveSlot.LoopsMet(_loops))
            {

            }
        }

        protected virtual void SubmitAudio(long position)
        {
            AudioState state = default;
            if (CheckAudioBuffer() &&
                ActiveSlot.Projection.GetAudio(ActiveSlot.AudioVariant, position, _audioBuffer.Span, ref state))
            {
                _sfx.Play(_audioBuffer, position, Volume);
            }
        }
        protected virtual void SubmitFrame()
        {
            if (ActiveSlot.CanDraw)
            {
                ActiveSlot.Projection.GetFrame(ActiveSlot.LayerState, ActiveSlot.ActiveFrame, ActiveSlot.Mask, _texture);
            }
        }

        protected virtual void SaveCustom(TagCompound tag) { }
        protected virtual void LoadCustom(TagCompound tag) { }

        protected virtual void SerializeCustom(BinaryWriter bw, SerializeType sType) { }
        protected virtual void DeserializeCustom(BinaryReader br, SerializeType sType) { }

        internal void ValidateSlotSize()
        {
            if (_slots.Length != _data.slotCount)
            {
                Array.Resize(ref _slots, _data.slotCount);
            }
        }

        protected bool FindNextValidSlot(int slotIn, out int nextSlot)
        {
            nextSlot = slotIn;
            while (true)
            {
                if (nextSlot >= _slots.Length)
                {
                    nextSlot = 0;
                }

                if (nextSlot == slotIn)
                {
                    return false;
                }
                ref var slot = ref _slots[nextSlot];

                if (slot.IsValid)
                {
                    return true;
                }
                nextSlot++;
            }
        }

        protected virtual void StopAudio()
        {
            _sfx.Stop();
            _audioBuffer = ProjectorSystem.ReturnAudioBuffer(_audioBuffer);
        }
        protected bool CheckAudioBuffer()
        {
            var proj = ActiveSlot.Projection;
            if(!(proj?.IsLoaded ?? false))
            {
                _audioBuffer = ProjectorSystem.ReturnAudioBuffer(_audioBuffer);
                return false;
            }

            bool hasAudio = proj.HasAudio;
            bool canPlay = hasAudio && Volume > 0;

            if (canPlay && _audioBuffer == null)
            {
                _audioBuffer = ProjectorSystem.RequestAudioBuffer();
            }
            else if (!canPlay && _audioBuffer != null)
            {
                _audioBuffer = ProjectorSystem.ReturnAudioBuffer(_audioBuffer);
            }
            return _audioBuffer != null;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ProjectorID
    {
        [FieldOffset(0)] public ProjectorType type;
        [FieldOffset(4)] public ulong id;
        [FieldOffset(4)] public short owner;
        [FieldOffset(6)] public short projectorIndex;
        [FieldOffset(4)] public uint uniqueID;
        [FieldOffset(4)] public Point tilePosition;

        public void Reset()
        {
            type = ProjectorType.__Count;
            id = 0;
        }

        public static ProjectorID FromTile(int x, int y)
        {
            return new ProjectorID()
            {
                type = ProjectorType.Tile,
                tilePosition = new Point(x, y)
            };
        }
        public static ProjectorID FromTile(Point tilePosition)
        {
            return new ProjectorID()
            {
                type = ProjectorType.Tile,
                tilePosition = tilePosition,
            };
        }
        public static ProjectorID FromCustom(uint uniqueID)
        {
            return new ProjectorID()
            {
                type = ProjectorType.Custom,
                uniqueID = uniqueID,
            };
        }
        public static ProjectorID FromPlayer(int player, int projectorIndex)
        {
            return new ProjectorID()
            {
                type = ProjectorType.Player,
                owner = (short)player,
                projectorIndex = (short)projectorIndex,
            };
        }
        public static ProjectorID FromNull()
        {
            return new ProjectorID()
            {
                type = ProjectorType.__Count,
                id = 0,
            };
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(type);
            bw.Write(id);
        }
        public void Deserialize(BinaryReader br)
        {
            type = br.Read<ProjectorType>();
            id = br.ReadUInt64();
        }

        public void Deserialize(ref ReadOnlySpan<byte> span)
        {
            type = span.Read<ProjectorType>(out span);
            id = span.Read<ulong>(out span);
        }

        public void Load(TagCompound tag)
        {
            type = tag.GetEnum<ProjectorType>("Type");
            id = tag.GetSafe("ID", 0UL);
        }
        public TagCompound Save(TagCompound tag)
        {
            tag.Assign("Type", type);
            tag.Assign("ID", id);
            return tag;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ProjectorData
    {
        private const int GENERAL_DATA_END = 32;

        public ProjectorType Type => id.type;
        public Point TilePosition => id.tilePosition;
        public short Owner => id.owner;
        public short ProjectorIndex => id.projectorIndex;
        public uint UniqueID => id.uniqueID;

        [FieldOffset(0)] public ProjectorID id;
        [FieldOffset(12)] public Vector2 hotspot;
        [FieldOffset(20)] public int slotCount;
        [FieldOffset(24)] public float timeInUse;
        [FieldOffset(28)] public uint creatorTag;

        // Tile
        [FieldOffset(GENERAL_DATA_END)] public Point tileRegion;
        [FieldOffset(GENERAL_DATA_END + 8)] public int style;

        // Custom      
        [FieldOffset(GENERAL_DATA_END)] public Vector2 position;

        public bool MatchType(in ProjectorData data)
        {
            return id.type != data.id.type && creatorTag == data.creatorTag;
        }

        public static ProjectorData NewCustom(uint creatorID, Vector2 hotspot, int slots, Vector2 position)
        {
            return new ProjectorData()
            {
                id = ProjectorID.FromCustom(0),
                hotspot = hotspot,
                slotCount = slots,
                timeInUse = 0.0f,
                position = position,
                creatorTag = creatorID,
            };
        }
        public static ProjectorData NewPlayer(uint creatorID, Vector2 hotspot, int slots, int player, int projectorIndex)
        {
            return new ProjectorData()
            {
                id = ProjectorID.FromPlayer(player, projectorIndex),
                hotspot = hotspot,
                slotCount = slots,
                timeInUse = 0.0f,
                creatorTag = creatorID,
            };
        }
        public static ProjectorData NewTile(uint creatorID, Vector2 hotspot, int slots, Point tilePosition, Point tileRegion, int style)
        {
            return new ProjectorData()
            {
                id = ProjectorID.FromTile(tilePosition),
                hotspot = hotspot,
                slotCount = slots,
                timeInUse = 0.0f,
                tileRegion = tileRegion,
                creatorTag = creatorID,
                style = style,
            };
        }
        public static ProjectorData NewNull()
        {
            return new ProjectorData()
            {
                id = ProjectorID.FromNull(),
            };
        }

        public void Serialize(BinaryWriter bw)
        {
            id.Serialize(bw);
            bw.Write(hotspot);
            bw.Write(slotCount);
            bw.Write(timeInUse);
            bw.Write(creatorTag);

            switch (id.type)
            {
                case ProjectorType.Custom:
                    bw.Write(position);
                    break;
                case ProjectorType.Tile:
                    bw.Write(tileRegion);
                    bw.Write(style);
                    break;
            }
        }
        public void Deserialize(BinaryReader br)
        {
            id.Deserialize(br);
            br.Read(ref hotspot);
            slotCount = Math.Max(br.ReadInt32(), 1);
            timeInUse = br.ReadSingle();
            creatorTag = br.ReadUInt32();

            switch (id.type)
            {
                case ProjectorType.Custom:
                    br.Read(ref position);
                    break;
                case ProjectorType.Tile:
                    br.Read(ref tileRegion);
                    style = br.ReadInt32();
                    break;
            }
        }

        public int Deserialize(ref ReadOnlySpan<byte> span)
        {
            int oLen = span.Length;
            id.Deserialize(ref span);
            span = span.Read(out hotspot);
            slotCount = Math.Max(span.Read<int>(out span), 1);
            span = span.Read(out timeInUse);
            span = span.Read(out creatorTag);

            switch (id.type)
            {
                case ProjectorType.Custom:
                    span = span.Read(out position);
                    break;
                case ProjectorType.Tile:
                    span = span.Read(out tileRegion);
                    span = span.Read(out style);
                    break;
            }
            return oLen - span.Length;
        }

        public void Save(TagCompound tag)
        {
            tag.Assign("ID", id.Save(new TagCompound()));
            tag.Assign("Hotspot", hotspot);
            tag.Assign("SlotCount", slotCount);
            tag.Assign("TimeInUse", timeInUse);
            tag.Assign("CreatorTag", creatorTag);

            switch (id.type)
            {
                case ProjectorType.Custom:
                    tag.Assign("Position", position);
                    break;
                case ProjectorType.Tile:
                    tag.Assign("TileRegion", tileRegion);
                    tag.Assign("Style", style);
                    break;
            }
        }
        public bool Load(TagCompound tag)
        {
            id.Reset();
            if (tag.TryGetSafe<TagCompound>("ID", out var idTag))
            {
                id.Load(idTag);
                hotspot = tag.GetVector2("Hotspot");
                slotCount = tag.GetSafe("SlotCount", 1);
                timeInUse = tag.GetSafe("TimeInUse", 0.0f);
                creatorTag = tag.GetSafe<uint>("CreatorTag", 0);
                switch (id.type)
                {
                    case ProjectorType.Custom:
                        position = tag.GetVector2("Position");
                        break;
                    case ProjectorType.Tile:
                        tileRegion = tag.GetPoint("TileRegion");
                        style = tag.GetSafe("Style", 0);
                        break;
                }
                return true;
            }
            return false;
        }
    }
}