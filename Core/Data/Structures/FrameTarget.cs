using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Projections.Core.Data.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FrameTarget : IEquatable<FrameTarget>
    {
        /*****************************************************************
         *                --- Binary Representation ---                  *
         *             3-2-1111111-00000000000000000000000               *
         *                                                               *
         *   0 = Size/Frame Index                                        *
         *   1 = Unused/Layer Index                                      *
         *   2 = Is Compressed/Is Emission                               * 
         *   3 = Is Targeted                                             *
         *                                                               *
         *  A FrameTarget with values set to zero means an empty frame.  *
         *  Bits marked 1 aren't used with normal frames.                *
         *****************************************************************/

        public static FrameTarget Zero => new FrameTarget();

        private const uint VALUE_MASK = 0x7FFFFFU;
        private const uint AUX_VAL_MASK = 0x7FU;
        private const uint FLAG_MODE_MASK = 0x80000000U;
        private const uint FLAG_AUX_MASK = 0x40000000U;

        public bool IsTargeted => (_data & FLAG_MODE_MASK) != 0;
        public bool AuxFlag => (_data & FLAG_AUX_MASK) != 0;

        public int Value => (int)(_data & VALUE_MASK);
        public int AuxValue => (int)((_data >> 23) & AUX_VAL_MASK);

        public long SeekAmount => IsTargeted ? 0 : Value;

        [FieldOffset(0)] private uint _data;

        public void Deserialize(BinaryReader br)
        {
            _data = br.ReadUInt32();
        }

        public override bool Equals(object obj)
            => obj is FrameTarget target && Equals(target);

        public bool Equals(FrameTarget other)
            => _data == other._data;

        public override int GetHashCode()
            => HashCode.Combine(_data);

        public static bool operator ==(FrameTarget lhs, FrameTarget rhs) 
            => lhs._data == rhs._data;

        public static bool operator !=(FrameTarget lhs, FrameTarget rhs) 
            => lhs._data != rhs._data;
    }
}
