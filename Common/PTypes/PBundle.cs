using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Collections;
using System;
using Microsoft.Xna.Framework;
using Projections.Core.Utilities;
using Terraria;

namespace Projections.Common.PTypes
{
    public abstract class PBundle
    {
        public string Name => Material.Name;
        public string Description => Material.Description;

        public ProjectionIndex Index => Material.Index;
        public PRarity Rarity => Material.Rarity;
        public int Priority => Material.Priority;

        public abstract PMaterial Material { get; }
        public ReadOnlySpan<PBundleEntry> Entries => _entries.Span;

        public int MinBundleSize => _minBundleSize;
        public int MaxBundleSize => _maxBundleSize;

        public bool IsValid => Index.IsValidID() && _entries.Count > 0 && Math.Max(_minBundleSize, _maxBundleSize) > 0;


        protected int _minBundleSize;
        protected int _maxBundleSize;
        protected RefList<PBundleEntry> _entries = new RefList<PBundleEntry>();

        public bool SpawnAsItems(Vector2 position)
        {
            Span<PBundleEntry> entries = _entries.Count > 32 ? new PBundleEntry[_entries.Count] : stackalloc PBundleEntry[_entries.Count];
            int total = 0;
            
            for (int i = 0; i < _entries.Count; i++)
            {
                ref var entry = ref _entries[i];
                if (entry.IsValid && entry.ConditionsMet)
                {
                    entries[total++] = entry;
                    total++;
                }
            }

            if(total < 1)
            {
                return false;
            }

            var selection = entries.Slice(0, total);
            int toSpawn = Main.rand.Next(_minBundleSize, _maxBundleSize + 1);
            for (int i = 0; i < toSpawn; i++)
            {
                int ind = selection.SelectRandomIndex();
                ref var entry = ref entries[ind];



                entry.weight *= 0.5f;
            }
            return true;
        }

        public abstract bool Load();
        public abstract void Unload();
    }
}
