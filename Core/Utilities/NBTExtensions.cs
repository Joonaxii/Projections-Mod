using Microsoft.Xna.Framework;
using Projections.Core.Data.Structures;
using System;
using System.Collections;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace Projections.Core.Utilities
{
    public static class NBTExtensions
    {
        public static T GetSafe<T>(this TagCompound tag, string key, T defaultV = default)
        {
            try
            {
                if (typeof(T).IsEnum)
                {
                    return GetEnum<T>(tag, key, defaultV);
                }

                return tag.Get<T>(key);
            }
            catch
            {
                return defaultV;
            }
        }
        public static bool TryGetSafe<T>(this TagCompound tag, string key, out T value, T defaultV = default)
        {
           
            try
            {
                if (typeof(T).IsEnum)
                {
                    value = GetEnum<T>(tag, key, defaultV);
                    return true;
                }

                if(tag.TryGet(key, out value))
                {
                    return true;
                }
                value = defaultV;
                return false;
            }
            catch
            {
                value = defaultV;
                return false;
            }
        }

        public static void AddEnum<T>(this TagCompound tag, string key, T enumVal)
        {
            switch (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))))
            {
                case TypeCode.Byte:
                    tag.Assign(key, Convert.ToByte(enumVal));
                    break;
                case TypeCode.SByte:
                    tag.Assign(key, Convert.ToSByte(enumVal));
                    break;
                case TypeCode.UInt16:
                    tag.Assign(key, Convert.ToUInt16(enumVal));
                    break;
                case TypeCode.Int16:
                    tag.Assign(key, Convert.ToInt16(enumVal));
                    break;
                case TypeCode.UInt32:
                    tag.Assign(key, Convert.ToUInt32(enumVal));
                    break;
                case TypeCode.Int32:
                    tag.Assign(key, Convert.ToInt32(enumVal));
                    break;
                case TypeCode.UInt64:
                    tag.Assign(key, Convert.ToUInt64(enumVal));
                    break;
                case TypeCode.Int64:
                    tag.Assign(key, Convert.ToInt64(enumVal));
                    break;
            }
        }
        public static T GetEnum<T>(this TagCompound tag, string key, T defaultV = default)
        {
            switch (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))))
            {
                case TypeCode.Byte:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<byte>(key));
                case TypeCode.SByte:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<sbyte>(key));
                case TypeCode.UInt16:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<ushort>(key));
                case TypeCode.Int16:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<short>(key));
                case TypeCode.UInt32:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<uint>(key));
                case TypeCode.Int32:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<int>(key));
                case TypeCode.UInt64:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<ulong>(key));
                case TypeCode.Int64:
                    return (T)Enum.ToObject(typeof(T), tag.GetSafe<long>(key));
            }
            return defaultV;
        }

        public static TagCompound Set(this TagCompound tag, ProjectionIndex index)
        {
            tag.Set("GroupID", index.group, true);
            tag.Set("MainID", index.target, true);
            return tag;
        }
        public static TagCompound New(ProjectionIndex index) => new TagCompound().Set(index);

        public static void Assign(this TagCompound tag, string key, ProjectionIndex index)
        {
            tag.Set(key, new TagCompound().Set(index), true);
        }

        public static ProjectionIndex GetPIndex(this TagCompound tag)
        {
            return new ProjectionIndex(tag.GetSafe<uint>("GroupID", 0), tag.GetSafe<uint>("MainID", 0));
        }
        public static ProjectionIndex GetPIndex(this TagCompound tag, string key)
        {
            return tag.GetSafe<TagCompound>(key)?.GetPIndex() ?? ProjectionIndex.Zero;
        }

        public static TagCompound Assign<T>(this TagCompound tag, string key, T value)
        {
            if (typeof(T).IsEnum)
            {
                tag.AddEnum(key, value);
                return tag;
            }

            TagIO.Serialize(value);
            tag.Set(key, value, true);
            return tag;
        }

        public static void Assign(this TagCompound tag, string key, Vector2 vec)
        {
            tag.Set(key, new TagCompound
            {
                { "X", vec.X },
                { "Y", vec.Y }
            }, true);
        }
        public static Vector2 GetVector2(this TagCompound tag, string key, Vector2 defVal = default)
        {
            tag = tag.Get<TagCompound>(key);
            return new Vector2(
                tag.GetSafe("X", defVal.X),
                tag.GetSafe("Y", defVal.Y));
        }

        public static void Assign(this TagCompound tag, string key, Point vec)
        {
            tag.Set(key, new TagCompound
            {
                { "X", vec.X },
                { "Y", vec.Y }
            }, true);
        }
        public static Point GetPoint(this TagCompound tag, string key, Point defVal = default)
        {
            tag = tag.Get<TagCompound>(key);
            return new Point(
                tag.GetSafe("X", defVal.X),
                tag.GetSafe("Y", defVal.Y));
        }

        public static void Assign(this TagCompound tag, string key, Color index)
        {
            tag.Set(key, new TagCompound
            {
                { "R", index.R },
                { "G", index.G },
                { "B", index.B },
                { "A", index.A }
            }, true);
        }
        public static Color GetColor(this TagCompound tag, string key, Color defVal = default)
        {
            tag = tag.Get<TagCompound>(key);
            return new Color(
                tag.GetSafe("R", defVal.R),
                tag.GetSafe("G", defVal.G),
                tag.GetSafe("B", defVal.B),
                tag.GetSafe("A", defVal.A));
        }

        public static void Assign(this TagCompound tag, string key, Color32 index)
        {
            tag.Set(key, new TagCompound
            {
                { "R", index.r },
                { "G", index.g },
                { "B", index.b },
                { "A", index.a }
            }, true);
        }
        public static Color32 GetColor32(this TagCompound tag, string key, Color32 defVal = default)
        {
            tag = tag.Get<TagCompound>(key);
            return new Color32(
                tag.GetSafe("R", defVal.r),
                tag.GetSafe("G", defVal.g),
                tag.GetSafe("B", defVal.b),
                tag.GetSafe("A", defVal.a));
        }
    }
}
