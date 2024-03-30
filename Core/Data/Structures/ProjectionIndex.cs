using Projections.Core.Data;
using Projections.Core.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Terraria.ModLoader.IO;

namespace Projections.Core.Data.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ProjectionIndex :
        IEquatable<ProjectionIndex>, IComparable<ProjectionIndex>,
        IEquatable<ulong>, IComparable<ulong>
    {
        public static ProjectionIndex Zero { get; } = new ProjectionIndex();

        [FieldOffset(0)] private ulong _hash;
        [FieldOffset(0)] public uint group;
        [FieldOffset(4)] public uint target;

        public ProjectionIndex(uint group, uint main)
        {
            this.group = group;
            target = main;
        }

        public override bool Equals(object obj) => obj is ProjectionIndex index && Equals(index);

        public override int GetHashCode() => _hash.GetHashCode();

        public static bool operator ==(ProjectionIndex left, ProjectionIndex right)
           => left.Equals(right);

        public static bool operator !=(ProjectionIndex left, ProjectionIndex right)
           => !(left == right);

        public bool IsValidID() => _hash != 0;

        public bool Equals(ProjectionIndex other)
            => _hash == other._hash;

        public int CompareTo(ProjectionIndex other)
         => _hash.CompareTo(other._hash);

        public bool Equals(ulong other)
            => _hash == other;

        public int CompareTo(ulong other)
         => _hash.CompareTo(other);
    }

    public struct ProjectionSource
    {
        public static ProjectionSource None => new ProjectionSource(ProjectionSourceType.None);
        public static ProjectionSource Drop => new ProjectionSource(ProjectionSourceType.Drop);
        public static ProjectionSource Shop => new ProjectionSource(ProjectionSourceType.Shop);

        public ProjectionSourceType Source => (ProjectionSourceType)(_value >> 24);
        public int Recipe => (int)(_value & 0x00_FF_FF_FF);
        private uint _value;

        public ProjectionSource(int recipeIndex)
        {
            _value = (uint)recipeIndex | (uint)ProjectionSourceType.Crafted << 24;
        }

        public ProjectionSource(ProjectionSourceType source)
        {
            _value = (uint)source << 24;
        }

        public ProjectionSource(ProjectionSourceType source, uint rawValue)
        {
            _value = rawValue & 0x00_FF_FF_FFU | (uint)source << 24;
        }

        public void Serialize(BinaryWriter bw) => bw.Write(_value);
        public void Deserialize(BinaryReader br) => _value = br.ReadUInt32();

        public void Save(string name, TagCompound tag)
        {
            tag.Assign(name, _value);
        }

        public void Load(string name, TagCompound tag)
        {
            _value = tag.Get<uint>(name);
        }
    }
}