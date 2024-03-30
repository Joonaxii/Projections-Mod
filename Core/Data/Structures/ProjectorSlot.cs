using System;
using System.IO;
using Terraria.ModLoader.IO;
using Projections.Core.Utilities;
using System.Text;
using Projections.Common.PTypes;
using System.Threading.Tasks;

namespace Projections.Core.Data.Structures
{
    public struct ProjectorSlot
    {
        public bool IsEmpty => !_index.IsValidID() || _stack < 1;

        public bool IsValid =>
            _index.IsValidID() &&
            (_projection?.IsValidUnlock(_activeFrame, _stack, _layerState) ?? true);
        public bool CanDraw => IsValid && _projection != null && _projection.IsLoaded;
        public bool HasAudio => _projection?.HasAudio ?? false;
        public bool AllowFrameSetting => _projection != null && _projection.AnimationMode == AnimationMode.FrameSet;

        public PRarity Rarity => _projection?.Rarity ?? 0;
        public Projection Projection => _projection;

        public int Width => Projection?.Width ?? 1;
        public int Height => Projection?.Height ?? 1;

        public ProjectionIndex Index => _index;
        public ushort LayerState
        {
            get => _projection?.GetLayerState(_layerState, Stack) ?? 0;
            set => _layerState = value;
        }
        public int Stack
        {
            get => _stack;
            internal set => _stack = value;
        }

        public LoopMode LoopMode
        {
            get => _loopMode;
            set => _loopMode = value;
        }

        public int Loops
        {
            get => _loops;
            set => _loops = value;
        }

        public int AudioVariant
        {
            get => _audioVariant;
            set => _audioVariant = value;
        }
        public int ActiveFrame
        {
            get => _activeFrame;
            set => _activeFrame = value;
        }
        public int Mask
        {
            get => _mask;
            set => _mask = value;
        }

        public AudioType AudioType => (_projection?.AudioInfo.type ?? AudioType.SFX) & AudioType.__TypeMask;

        private ProjectionIndex _index;
        private int _stack;
        private int _loops;
        private int _mask;
        private LoopMode _loopMode;
        private ushort _layerState;

        private int _activeFrame;
        private int _audioVariant;

        private Projection _projection;

        public bool IsLayerEnabled(int i)
        {
            return (_layerState & 1 << i) != 0;
        }

        public void ToggleLayer(int i, bool state)
        {
            if (state)
            {
                _layerState |= (ushort)(1 << i);
            }
            else
            {
                _layerState &= (ushort)~(1 << i);
            }
        }

        public bool Setup(ProjectionIndex index, int stackSize, ushort? layerState = null)
        {
            var prev = _projection;
            _activeFrame = 0;
            _stack = stackSize;
            _index = index;
            bool isNew = prev != _projection;

            Validate();
            if (layerState != null)
            {
                _layerState = layerState.Value;
            }
            return isNew;
        }

        public void ResetState()
        {
            _loops = 0;
            _activeFrame = 0;
        }

        public void Reset()
        {
            ResetState();
            _audioVariant = 0;
            _mask = -1;
            _layerState = _projection?.DefaultLayerState ?? 0x1;
            _loopMode = LoopMode.Next;
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(_index);
            bw.Write(_stack);
            bw.Write(_mask);
            bw.Write(_loops);
            bw.Write(_loopMode);
            bw.Write(_layerState);
            bw.Write(_activeFrame);
            bw.Write(_audioVariant);
        }
        public void Deserialize(BinaryReader br)
        {
            br.Read(ref _index);
            _stack = br.ReadInt32();
            _mask = br.ReadInt32();
            _loops = br.ReadInt32();
            _loopMode = br.Read<LoopMode>();
            _layerState = br.ReadUInt16();
            _activeFrame = br.ReadInt32();
            _audioVariant = br.ReadInt32();
            Validate();
        }

        public int Deserialize(ref ReadOnlySpan<byte> span)
        {
            int oLen = span.Length;
            span = span.Read(out _index);
            span = span.Read(out _stack);
            span = span.Read(out _mask);
            span = span.Read(out _loops);
            span = span.Read(out _loopMode);
            span = span.Read(out _layerState);
            span = span.Read(out _activeFrame);
            span = span.Read(out _audioVariant);

            Validate();
            return oLen - span.Length;
        }

        public void Save(TagCompound tag)
        {
            tag.Assign("Index", _index);
            tag.Assign("Stack", _stack);
            tag.Assign("Mask", _mask);
            tag.Assign("Loops", _loops);
            tag.Assign("LoopMode", _loopMode);
            tag.Assign("LayerState", _layerState);
            tag.Assign("ActiveFrame", _activeFrame);
            tag.Assign("AudioVariant", _audioVariant);
        }
        public void Load(TagCompound tag)
        {
            _index = tag.GetPIndex("Index");
            _stack = tag.GetSafe("Stack", _index.IsValidID() ? 1 : 0);
            _mask = tag.GetSafe("Loops", -1);
            _loops = tag.GetSafe("Loops", 0);
            _loopMode = tag.GetSafe("LoopMode", LoopMode.Infinite);
            _layerState = tag.GetSafe<ushort>("LayerState", 0x1);
            _activeFrame = tag.GetSafe<int>("ActiveFrame");
            _audioVariant = tag.GetSafe<int>("AudioVariant");
            Validate();
        }

        public void Validate()
        {
            var proj = Projections.GetProjection(_index);
            if(proj != _projection)
            {
                _projection?.UnregisterUser();
                _projection = proj;
                _projection?.RegisterUser();
            }

            if (_projection != null)
            {
                _mask = Math.Min(_mask, _projection.MaskCount - 1);
                _activeFrame = _projection.GetValidFrame(_activeFrame, _stack);
                _audioVariant = Math.Min(_audioVariant, Math.Max(_projection.AudioInfo.variants - 1, 0));
            }
            else
            {
                _mask = -1;
                _activeFrame = -1;
                _audioVariant = -1;
            }
        }
        public void Clear()
        {
            _index = ProjectionIndex.Zero;
            _stack = 0;
            _layerState = 0x1;
            _activeFrame = -1;
            _audioVariant = -1;
        }

        public bool SetFrame(int frame)
        {
            if (!AllowFrameSetting) { return false; }
            int newFrame = _projection.GetValidFrame(frame, Stack);
            if (newFrame == frame) { return false; }

            _activeFrame = newFrame;
            return _activeFrame != frame;
        }

        public bool LoopsMet(int loops)
        {
            return _loopMode != LoopMode.Infinite && loops >= _loops;
        }
    }
}