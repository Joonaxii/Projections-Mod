using Microsoft.Xna.Framework;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using System;

namespace Projections.Common.PTypes.InMemory
{
    public class InMemoryPBundle : PBundle
    {
        public override PMaterial Material => _material;
        private PMaterial _material;

        internal InMemoryPBundle() { }

        public override bool Load() => true;
        public override void Unload() { }

        public static InMemoryPBundle Create(PMaterial material, int minBundleSize, int maxBundleSize = 0)
        {
            InMemoryPBundle bundle = new InMemoryPBundle();
            bundle._material = material;
            bundle._minBundleSize = Math.Max(minBundleSize, 1);
            bundle._maxBundleSize = Math.Max(maxBundleSize, minBundleSize);
            return bundle;
        }

        public InMemoryPBundle AddItem(PType type, ProjectionIndex index, int stackSize)
        {
            return AddItem(type, index, stackSize, default, 0.25f);
        }
        public InMemoryPBundle AddItem(PType type, ProjectionIndex index, int stackSize, in PConditions conditions, float weight)
        {
            _entries.Add(new PBundleEntry()
            {
                type = type,
                index = index,
                stack = Math.Max(stackSize, 1),
                conditions = conditions
            });
            return this;
        }
    }
}
