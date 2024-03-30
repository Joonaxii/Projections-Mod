using System.IO;
using Projections.Core.Utilities;

namespace Projections.Core.Data.Structures
{
    public struct ItemConditions
    {
        public bool AreMet => _conditions.AreMet;
        public float Chance => _chance;
        public float Weight => _weight;

        private PConditions _conditions;
        private float _chance;
        private float _weight;

        public void Reset()
        {
            _conditions.Reset();
            _chance = 1.0f;
            _weight = 1.0f;
        }

        public void Deserialize(BinaryReader br)
        {
            _conditions.Deserialize(br);
            _chance = br.ReadSingle();
            _weight = br.ReadSingle();
        }

        public void Serialize(BinaryWriter bw)
        {
            _conditions.Serialize(bw);
            bw.Write(_chance);
            bw.Write(_weight);
        }

        public static ItemConditions Create(float chance, float weight)
        {
            return new ItemConditions()
            {
                _conditions = default,
                _chance = chance,
                _weight = weight
            };
        }

        public static ItemConditions Create(in PConditions conditions, float chance, float weight)
        {
            return new ItemConditions()
            {
                _conditions = conditions,
                _chance = chance,
                _weight = weight
            };
        }
    }

    public struct PConditions
    {
        public bool AreMet => worldConditions.AreMet(biomeConditions);

        public WorldConditions worldConditions;
        public BiomeConditions biomeConditions;

        public void Reset()
        {
            worldConditions = WorldConditions.None;
            biomeConditions = BiomeConditions.None;
        }

        public void Deserialize(BinaryReader br)
        {
            worldConditions = br.Read<WorldConditions>();
            biomeConditions = br.Read<BiomeConditions>();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(worldConditions);
            bw.Write(biomeConditions);
        }
    }
}