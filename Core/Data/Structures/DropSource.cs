using System;
using System.IO;
using Projections.Core.Utilities;

namespace Projections.Core.Data.Structures
{
    public struct DropSource
    {
        public const int DROP_SOURCE_SIZE = 34;
        public bool IsValid
        {
            get
            {
                return _type switch
                {
                    PoolType.NPC => !_flags.HasFlag(PDropFlags.IsModded) || _id != 0,
                    PoolType.Treasure => _id != 0,
                    PoolType.FishingQuest or PoolType.Trader => true,
                    _ => false,
                };
            }
        }

        public PoolType Type => _type;

        public float Weight => _conditions.Weight;
        public float Chance => _conditions.Chance;
        public bool ConditionsMet => _conditions.AreMet;

        public bool IsNetID => _flags.HasFlag(PDropFlags.IsNetID);
        public int Stack => _stack;
        public int ID
        {
            get 
            {
                return _type switch
                { 
                    PoolType.NPC or PoolType.Treasure => _id,
                    _ => 0,
                    };
            }
        }

        private PoolType _type;
        private ItemConditions _conditions;

        private PDropFlags _flags;
        private int _stack;
        private int _id;

        public void Deserialize(BinaryReader br)
        {
            _type = (PoolType)br.ReadByte();
            _conditions.Deserialize(br);
            switch (_type)
            {
                case PoolType.NPC:
                    _flags = (PDropFlags)br.ReadByte();
                    _stack = Math.Max(br.ReadInt32(), 1);
                    {
                        string path = br.ReadShortString();
                        _id = _flags.HasFlag(PDropFlags.IsModded) ? Projections.GetNPCIndex(path) : br.ReadInt32();
                        break;
                    }
                case PoolType.Treasure:
                    _flags = (PDropFlags)br.ReadByte();
                    _stack = Math.Max(br.ReadInt32(), 1);
                    {
                        string path = br.ReadShortString();
                        _id = _flags.HasFlag(PDropFlags.IsModded) ? Projections.GetItemIndex(path) : br.ReadInt32();
                        break;
                    }
                case PoolType.FishingQuest:
                    _stack = Math.Max(br.ReadInt32(), 1);
                    break;
            }
        }

        public void ReadPacket(BinaryReader br)
        {
            _type = (PoolType)br.ReadByte();
            _conditions.Deserialize(br);
            _flags = (PDropFlags)br.ReadByte();
            _stack = br.ReadUInt16() + 1;
            _id = br.ReadInt32();
        }

        public void WritePacket(BinaryWriter bw)
        {
            bw.Write((byte)_type);
            _conditions.Serialize(bw);
            bw.Write((byte)_flags);
            bw.Write((ushort)(Math.Min(Math.Max(_stack, 1) - 1, ushort.MaxValue)));
            bw.Write(_id);
        }
    }
}