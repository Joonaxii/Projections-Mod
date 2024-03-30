using Projections.Common.PTypes.OnDisk;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projections.Common.PTypes.Streamed
{
    public class StreamedPBundle : OnDiskPBundle
    {
        public StreamedPBundle(string path, uint id) : base(path, id) { }

        private static void ReadEntry(ref PBundleEntry entry, BinaryReader br)
        {
            entry.type = br.Read<PType>();
            entry.index = br.ReadShortParsedIndex();
            entry.stack = Math.Max(br.ReadInt32(), 1);
            entry.conditions.Deserialize(br);
            entry.weight = Math.Max(br.ReadSingle(), 0.0001f);
        }

        public override bool Deserialize(BinaryReader br, Stream stream)
        {
            _minBundleSize = Math.Max(br.ReadInt32(), 1);
            _maxBundleSize = Math.Max(br.ReadInt32(), _minBundleSize);

            _entries.Resize(br.ReadInt32());
            for (int i = 0; i < _entries.Count; i++)
            {
                ReadEntry(ref _entries[i], br);
            }
            return true;
        }

        protected override OnDiskPMaterial GetMaterial(string path, uint id)
            => new StreamedPMaterial(path, id);
    }
}
