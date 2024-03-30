using System.IO;

namespace Projections.Common.PTypes.OnDisk
{
    public abstract class OnDiskProjection : Projection
    {
        public string Path
        {
            get => Material is OnDiskPMaterial dMat ? dMat.Path : "";
        }

        public override PMaterial Material => _material;

        public Stream IOStream => (Material as OnDiskPMaterial)?.IOStream;
        public BinaryReader Reader => (Material as OnDiskPMaterial)?.Reader;
        protected PMaterial _material;

        public OnDiskProjection(string path, uint id)
        {
            _material = GetMaterial(path, id);
        }
        ~OnDiskProjection()
        {
            CloseStream();
        }

        internal override bool Load()
        {
            _markedOfReset = false;

            if (!_material.Load())
            {
                _isLoaded = false;
                return false;
            }
            _isLoaded = OnLoad();
            return _isLoaded;
        }

        public bool OpenStream()
        {
            return Material is OnDiskPMaterial pMat && pMat.OpenStream();
        }
        public bool CloseStream()
        {
            return Material is OnDiskPMaterial pMat && pMat.CloseStream();
        }

        public abstract bool Deserialize(BinaryReader br, Stream stream);
        protected abstract PMaterial GetMaterial(string path, uint id);
    }
}