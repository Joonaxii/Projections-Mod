using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Projections.Core.Utilities
{
    public static class SpanExtensions
    {
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Span<T> span)
        {
            ReadOnlySpan<T> read = span;
            return read;
        }

        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> span)
        {
            ReadOnlySpan<T> read = CollectionsMarshal.AsSpan(span);
            return read;
        }

        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] span)
        {
            ReadOnlySpan<T> read = span.AsSpan();
            return read;
        }

        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this IList<T> span)
        {
            if(span is T[] arr)
            {
                return arr.AsReadOnlySpan();
            }
            else if(span is List<T> list)
            {
                return list.AsReadOnlySpan();
            }
            return default;
        }

        public static U ReadAs<T, U>(this ReadOnlySpan<T> span) where T : unmanaged where U : unmanaged
        {
            return MemoryMarshal.Read<U>(MemoryMarshal.AsBytes(span));
        }

        public static U ReadAs<T, U>(this Span<T> span) where T : unmanaged where U : unmanaged
        {
            ReadOnlySpan<T> rosp = span;
            return rosp.ReadAs<T, U>();
        }

        public static void CopyToReverse<T>(this Span<T> src, Span<T> dst) where T : struct
        {
            for (int i = dst.Length - 1; i >= 0; i--)
            {
                src[i] = dst[i];
            }
        }

        public static void Memset<T>(this Span<T> data, T value) where T : unmanaged
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = value;
            }
        }

        public static void Memset<T>(this Span<T> data, ref T value) where T : unmanaged
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = value;
            }
        }
    }
}