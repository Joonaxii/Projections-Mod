using System;

namespace Projections.Core.Data.Structures
{
    public ref struct AudioState
    {
        public long position;
        public Span<byte> data;
    }
}