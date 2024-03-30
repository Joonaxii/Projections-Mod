using Projections.Core.Data;

namespace Projections.Core.Data.Structures
{
    public struct AudioInfo
    {
        public readonly long ByteLength => _byteLength;
        public long BytesPerSec => sampleRate * 2 * (stereo ? 2 : 1);

        public AudioType type;
        public bool stereo;
        public ushort variants;
        public long sampleCount;
        public uint sampleRate;
        private long _byteLength;

        public readonly long GetOffset(int variant) => variant * _byteLength;

        public void Setup()
        {
            _byteLength = sampleCount * 2 * (stereo ? 2 : 1);
        }
    }
}