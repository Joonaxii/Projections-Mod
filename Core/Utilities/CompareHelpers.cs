using Microsoft.Xna.Framework;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Core.Data.Structures;
using Projections.Core.Maths;
using System.Collections;

namespace Projections.Core.Utilities
{
    public static class CompareHelpers
    {
        public static int CompareTo(this Point lhs, Point rhs)
        {
            int ret = lhs.X.CompareTo(rhs.X);
            return ret == 0 ? lhs.Y.CompareTo(rhs.Y) : ret;
        }
        public static int CompareByProjectorRef(in Projector lhs, in Projector rhs)
        {
            return Comparer.Default.Compare(lhs, rhs);
        }

        public static int CompareByProjectorID(in Projector lhs, in Projector rhs)
        {
            return lhs.UniqueID.CompareTo(rhs.UniqueID);
        }
        public static int CompareByProjectorID(in Projector lhs, in ulong rhs)
        {
            return lhs.UniqueID.CompareTo(rhs);
        }
        public static int CompareByProjectorID(in Projector lhs, in Point rhs)
        {
            return lhs.UniqueID.CompareTo(rhs.Reinterpret<Point, ulong>());
        }

        public static int CompareByProjectionID(in Projection lhs, in Projection rhs)
        {
            return lhs.Index.CompareTo(rhs.Index);
        }
        public static int CompareByProjectionID(in Projection lhs, in ProjectionIndex rhs)
        {
            return lhs.Index.CompareTo(rhs);
        }

        public static int CompareByProjectionID(in PBundle lhs, in PBundle rhs)
        {
            return lhs.Index.CompareTo(rhs.Index);
        }
        public static int CompareByProjectionID(in PBundle lhs, in ProjectionIndex rhs)
        {
            return lhs.Index.CompareTo(rhs);
        }

        public static int CompareByProjectionID(in PMaterial lhs, in PMaterial rhs)
        {
            return lhs.Index.CompareTo(rhs.Index);
        }
        public static int CompareByProjectionID(in PMaterial lhs, in ProjectionIndex rhs)
        {
            return lhs.Index.CompareTo(rhs);
        }
    }
}
