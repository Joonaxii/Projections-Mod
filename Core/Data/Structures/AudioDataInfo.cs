using System.IO;

namespace Projections.Core.Data.Structures
{
    internal struct AudioDataInfo
    {
        public int sampleRate;
        public uint sampleLength;
        public byte channels;
        public int variants;
        public long byteLength;
        public long position;

        public long GetAudioOffset(int variant, long bytePos) => position + variant * byteLength + bytePos;

        public void Deserialize(BinaryReader br, Stream stream)
        {
            sampleRate = br.ReadInt32();
            sampleLength = br.ReadUInt32();
            channels = br.ReadByte();
            variants = br.ReadInt32();
            position = stream.Position;
            byteLength = sampleRate * sampleLength * sizeof(short);
        }
    }
}