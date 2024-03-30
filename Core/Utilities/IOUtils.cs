using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Systems;
using Terraria.ModLoader;

namespace Projections.Core.Utilities
{
    public static class IOUtils
    {
        public static bool ExtractModPath(ReadOnlySpan<char> input, out ReadOnlySpan<char> mod, out ReadOnlySpan<char> path)
        {
            mod = default;
            path = default;

            if (input.IsContentPath())
            {
                input = input.Slice(5);

                int ind = input.IndexOf(':');
                if (ind < 0) { return false; }
                mod = input.Slice(0, ind);
                path = input.Slice(ind + 1);
                return true;
            }
            return false;
        }

        internal static Stream OpenRead(string path)
        {
            if (ExtractModPath(path, out var mod, out var file))
            {
                string fileStr = file.ToString();
                if (ModLoader.TryGetMod(mod.ToString(), out var modV)
                    && modV.HasAsset(fileStr))
                {
                    return modV.GetFileStream(fileStr, true);
                }
                return null;
            }
            return File.Exists(path) ? new FileStream(path, FileMode.Open) : null;
        }

        public static Span<char> ReadShortString(this BinaryReader br, Span<char> buffer)
        {
            int chars = br.ReadByte();
            if (chars < 1) { return buffer.Slice(0, 0); }

            Span<byte> temp = stackalloc byte[chars];
            br.Read(temp);

            int len = Encoding.UTF8.GetCharCount(temp);
            return buffer.Slice(0, Encoding.UTF8.GetChars(temp, buffer));
        }
        public static string ReadShortString(this BinaryReader br)
        {
            int chars = br.ReadByte();
            if (chars < 1) { return ""; }

            Span<byte> temp = stackalloc byte[chars];
            br.Read(temp);

            int len = Encoding.UTF8.GetCharCount(temp);
            Span<char> strOut = stackalloc char[len];
            strOut = strOut.Slice(0, Encoding.UTF8.GetChars(temp, strOut));
            return new string(strOut);
        }

        public static ProjectionIndex ReadShortParsedIndex(this BinaryReader br, OnIntern intern = null)
        {
            int chars = br.ReadByte();
            if (chars < 1) { return ProjectionIndex.Zero; }

            Span<byte> temp = stackalloc byte[chars];
            br.Read(temp);

            int len = Encoding.UTF8.GetCharCount(temp);
            Span<char> strOut = stackalloc char[len];
            return strOut.Slice(0, Encoding.UTF8.GetChars(temp, strOut)).ParseProjection(intern);
        }

        public static void SkipShortString(this BinaryReader br)
        {
            int chars = br.ReadByte();
            if (chars < 1) { return; }
            br.BaseStream.Seek(chars, SeekOrigin.Current);
        }

        public static void Write<T>(this BinaryWriter bw, T en) where T : unmanaged
        {
            bw.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref en, 1)));
        }

        public static T Read<T>(this BinaryReader br) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                Span<byte> stack = stackalloc byte[size];
                br.Read(stack);
                return MemoryMarshal.Read<T>(stack);
            }
        }
        public static void Read<T>(this BinaryReader br, ref T outVal) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                Span<byte> stack = stackalloc byte[size];
                br.Read(stack);
                outVal = MemoryMarshal.Read<T>(stack);
            }
        }

        public static T Read<T>(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outVal, T defVal = default) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                if (size > span.Length)
                {
                    outVal = span.Slice(span.Length);
                    return defVal;
                }
                outVal = span.Slice(size);
                return MemoryMarshal.Read<T>(span);
            }
        }
        public static T Read<T>(this Span<byte> span, out Span<byte> outVal, T defVal = default) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                if (size > span.Length)
                {
                    outVal = span.Slice(span.Length);
                    return defVal;
                }
                outVal = span.Slice(size);
                return MemoryMarshal.Read<T>(span);
            }
        }

        public static ReadOnlySpan<byte> Read<T>(this ReadOnlySpan<byte> span, out T vOut, T defVal = default) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                if (size > span.Length)
                {
                    vOut = defVal;
                    return span.Slice(span.Length);
                }
                vOut = MemoryMarshal.Read<T>(span);
                return span.Slice(size);
            }
        }
        public static Span<byte> Read<T>(this Span<byte> span, out T vOut, T defVal = default) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                if (size > span.Length)
                {
                    vOut = defVal;
                    return span.Slice(span.Length);
                }
                vOut = MemoryMarshal.Read<T>(span);
                return span.Slice(size);
            }
        }
    }
}