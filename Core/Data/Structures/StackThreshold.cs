using System;
using System.IO;

namespace Projections.Core.Data.Structures
{
    /// <summary>
    /// Stack thresholds for frame regions.
    /// </summary>
    public struct StackThreshold
    {
        public int stack;
        public int frames;

        public void Deserialize(BinaryReader br)
        {
            stack = Math.Max(br.ReadInt32(), 1);
            frames = Math.Max(br.ReadInt32(), 1);
        }
    }
}