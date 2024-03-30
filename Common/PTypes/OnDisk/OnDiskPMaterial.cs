using System;
using System.IO;
using Projections.Core.Utilities;
using System.Text;
using Projections.Core.Data;

namespace Projections.Common.PTypes.OnDisk
{
    public abstract class OnDiskPMaterial : PMaterial
    {
        public string Path => _path;
        private string _path;

        public Stream IOStream => _stream;
        public BinaryReader Reader => _br;

        private Stream _stream;
        private BinaryReader _br;
        private uint _identifier;

        public OnDiskPMaterial(string path, uint id)
        {
            _identifier = id;
            _path = path;
        }

        ~OnDiskPMaterial()
        {
            CloseStream();
        }

        public bool OpenStream()
        {
            if (_stream != null) { return false; }
            try
            {
                _stream = IOUtils.OpenRead(Path);
                if (_stream == null)
                {
                    Projections.Log(LogType.Error, "Failed to Open Projection Stream!");
                    return false;
                }
            }
            catch (Exception e)
            {
                Projections.Log(LogType.Error, $"Failed to Open Projection Stream!\n\n{e.StackTrace}\n\n{e.Message}");
                return false;
            }
            _br = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
            return true;
        }
        public bool CloseStream()
        {
            if (_stream == null) { return false; }
            _br.Dispose();
            _stream.Dispose();
            _stream = null;
            _br = null;
            return true;
        }

        public abstract bool Deserialize(BinaryReader br, Stream stream);

        public override bool Load()
        {
            if (!OpenStream()) { return false; }
            uint id = _br.ReadUInt32();

            if (id != _identifier)
            {
                Projections.Log(LogType.Error, $"Could not load P-Material of type {GetType()}! (Header [0x{id:X8}] doesn't match the ID  [0x{_identifier:X8}] d)");
                CloseStream();
                return false;
            }

            bool success = Deserialize(_br, _stream);
            CloseStream();
            return success;
        }

        public override void Unload()
        {
            CloseStream();
        }
    }
}