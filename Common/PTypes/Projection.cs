using System;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Collections;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Textures;

namespace Projections.Common.PTypes
{
    public abstract class Projection
    {
        public const int MAX_LAYERS = 16;

        public bool IsLoaded => _isLoaded;

        public string Name => Material.Name;
        public string Description => Material.Description;
        public ProjectionIndex Index => Material.Index;

        public PRarity Rarity => Material.Rarity;
        public int Priority => Material.Priority;
        public int Value => Material.Value;

        public Texture2D Icon => Material.Icon;
        public abstract PMaterial Material { get; }

        public ushort DefaultLayerState
        {
            get
            {
                ushort state = 0;
                for (int i = 0, j = 1; i < Math.Min(_layers.Count, MAX_LAYERS); i++, j <<= 1)
                {
                    if (_layers[i].DefaultState)
                    {
                        state |= (ushort)j;
                    }
                }
                return state;
            }
        }

        public ReadOnlySpan<int> Tags => _tags.Span;
        public ReadOnlySpan<Layer> Layers => _layers.Span;
        public ReadOnlySpan<DropSource> Sources => Material.Sources;

        public bool HasAudio => AudioInfo.variants > 0 && AudioInfo.sampleCount > 0 && AudioInfo.sampleRate > 0;

        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int MaskCount { get; }
        public abstract int FrameCount { get; }
        public abstract float LoopTime { get; }
        public abstract float Duration { get; }
        public abstract AnimationMode AnimationMode { get; }
        public abstract ref readonly AudioInfo AudioInfo { get; }

        protected uint _users;
        protected RefList<int> _tags = new RefList<int>();
        protected RefList<Layer> _layers = new RefList<Layer>(MAX_LAYERS);
        protected RefList<StackThreshold> _stackThresholds = new RefList<StackThreshold>();
        protected internal bool _isLoaded;
        protected internal bool _markedOfReset;

        public void RegisterUser()
        {
            if (_isLoaded && _users++ == 0)
            {
                _markedOfReset = false;
                OnFirstUserIn();
            }
        }
        public void UnregisterUser()
        {
            if (!_isLoaded || _users == 0) { return; }
            if (--_users <= 0)
            {
                _markedOfReset = true;
            }
        }

        internal virtual bool Load()
        {
            _markedOfReset = false;
            _isLoaded = OnLoad();
            return _isLoaded;
        }
        internal virtual void Unload()
        {
            _users = 0;
            Material.Unload();
            OnUnload();
            _isLoaded = false;
            _markedOfReset = false;
        }

        public virtual void Update()
        {
            if (_users == 0 && _markedOfReset)
            {
                OnLastUserOut();
                _markedOfReset = false;
            }
        }

        public abstract float GetFrameDuration(int frame);

        public virtual bool GetFrame(ushort layerMask, int frame, int mask, ProjectorTexture texture) => false;
        public virtual bool GetAudio(int variant, long sample, Span<byte> buffer, ref AudioState sampleRead) => false;

        public virtual int GetValidFrame(int frame, int stack)
        {
            if (frame <= 0) { return 0; }
            return Math.Min(frame, GetUnlockCount(stack) - 1);
        }
        public virtual int GetUnlockCount(int stackSize)
        {
            switch (AnimationMode)
            {
                case AnimationMode.LoopAudio:
                case AnimationMode.LoopVideo:
                    return FrameCount;
            }

            if (_stackThresholds.Count <= 1)
            {
                return _stackThresholds.Count < 1 || _stackThresholds[0].stack <= stackSize ? FrameCount : 0;
            }

            int accu = 0;
            for (int i = 0; i < _stackThresholds.Count; i++)
            {
                ref var st = ref _stackThresholds[i];
                if (st.stack > stackSize) { break; }
                accu += st.frames;
            }
            return Math.Min(accu, FrameCount);
        }

        public virtual bool IsLayerUnlocked(int layer, int stackSize, out int stackUnlock)
        {
            if (layer < 0 || layer >= _layers.Count) { stackUnlock = 0; return false; }
            stackUnlock = _layers[layer]._stackThreshold;
            return _layers[layer]._stackThreshold <= stackSize;
        }

        public bool IsValidUnlock(int frame, int stackSize, ushort activeMask)
        {
            int unlockC = GetUnlockCount(stackSize);
            if (unlockC <= frame) { return false; }
            for (int i = 0, mask = 1; i < _layers.Count; i++, mask <<= 1)
            {
                if ((activeMask & mask) != 0 && IsLayerUnlocked(i, stackSize, out _))
                {
                    return true;
                }
            }
            return false;
        }

        public ushort GetLayerState(ushort wanted, int stack)
        {
            ushort outState = 0x00;
            ushort defaultState = DefaultLayerState;
            for (int i = 0, k = 1; i < _layers.Count; i++, k <<= 1)
            {
                outState |= (ushort)((IsLayerUnlocked(i, stack, out _) ? wanted : defaultState) & k);
            }
            return outState;
        }

        protected virtual void OnFirstUserIn() { }
        protected virtual void OnLastUserOut() { }

        protected abstract bool OnLoad();
        protected abstract void OnUnload();
    }
}