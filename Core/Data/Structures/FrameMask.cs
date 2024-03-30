using Projections.Core.Textures;
using Projections.Core.Utilities;
using System;
using System.IO;

namespace Projections.Core.Data.Structures
{
    public struct FrameMask
    {
        public string name;
        public uint hash;
        public int size;
        public long position;

        public bool ReadMask(Stream stream, Span<byte> buffer)
        {
            stream.Seek(position, SeekOrigin.Begin);

            // If the size that was read before is less than 0, then the mask is uncompressed and we can just read the bytes as is.
            // If however the size is 0, then the frame is invalid/empty and we should return false to not use a mask.
            if(size == 0)
            {
                return false;
            } 
            else if(size < 0)
            {
                stream.Read(buffer);
            }
            else
            {
                Texture2DUtils.LoadRLE(stream, buffer, buffer.Length);
            }
            return true;
        }

        public void Deserialize(BinaryReader br, Stream stream)
        {
            size = br.ReadInt32();
            name = size != 0 ? br.ReadShortString() : "";
            hash = name.GetProjectionHash();

            position = stream.Position;
            stream.Seek(size < 0 ? ~size : size, SeekOrigin.Current);
        }
    }
}