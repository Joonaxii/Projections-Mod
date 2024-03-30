using System.Collections.Generic;

namespace Projections.Core.Utilities
{
    public static class ArrayUtils
    {
        public static void CopyFrom<T>(this HashSet<T> lhs, IEnumerable<T> other, bool clear = false)
        {
            if (clear) { lhs.Clear(); }
            foreach (var v in other)
            {
                lhs.Add(v);
            }
        }
    }
}