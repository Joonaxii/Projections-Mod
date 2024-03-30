using System;
using System.IO;

namespace Projections.Common.PTypes.OnDisk
{
    public abstract class OnDiskPBundle : PBundle
    {
        public string Path => _material.Path;
        public Stream IOStream => _material.IOStream;
        public BinaryReader Reader => _material.Reader;
        public override PMaterial Material => _material;

        protected OnDiskPMaterial _material;

        public OnDiskPBundle(string path, uint id)
        {
            _material = GetMaterial(path, id);
        }

        ~OnDiskPBundle()
        {
            _material.Unload();
        }

        public abstract bool Deserialize(BinaryReader br, Stream stream);

        public override bool Load()
        {
            if (!_material.OpenStream() ||
                !_material.Deserialize(Reader, IOStream)) { return false; }
            bool success = Deserialize(Reader, IOStream);
            _material.CloseStream();
            return success;
        }

        public override void Unload()
        {
            _material.Unload();
        }

        protected abstract OnDiskPMaterial GetMaterial(string path, uint id);
    }
}
