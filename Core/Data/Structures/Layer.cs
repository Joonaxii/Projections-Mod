using System.IO;
using System;
using Projections.Core.Utilities;
using Projections.Common.PTypes;

namespace Projections.Core.Data.Structures
{
    /// <summary>
    /// A definiton used for a <see cref="Projection"/> layer. Contains information about a layer like it's name, unlock trhreshold and flags.
    /// </summary>
    public struct Layer
    {
        public string Name => _name;
        public bool DefaultState => _flags.HasFlag(LayerFlags.DefaultOn);
        public int StackThreshold => _stackThreshold;

        public LayerFlags Flags => _flags;

        private string _name;
        private LayerFlags _flags;
        public int _stackThreshold;

        internal Layer(string name, LayerFlags flags, int stackThreshold)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "Default" : name;
            _flags = flags;
            _stackThreshold = stackThreshold;
        }

        public static Layer NewLayer(string name = "Default", LayerFlags flags = LayerFlags.DefaultOn, int stackThreshold = 1)
        {
            return new Layer(name, flags, Math.Max(stackThreshold, 1));
        }

        internal void Deserialize(BinaryReader br)
        {
            _flags = br.Read<LayerFlags>();
            _name = br.ReadShortString();
            _stackThreshold = br.ReadInt32();
        }
    }
}