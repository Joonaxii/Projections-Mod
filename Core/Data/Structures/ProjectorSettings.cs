using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria.ModLoader.IO;
using Projections.Core.Utilities;

namespace Projections.Core.Data.Structures
{
    public struct ProjectorSettings
    {
        public bool IsActive
        {
            get => flags.HasFlag(ProjectorFlags.IsActive);
            set => flags = flags.SetMask(ProjectorFlags.IsActive, value);
        }
        public bool IsPlaying
        {
            get => flags.HasFlag(ProjectorFlags.IsPlaying);
            set => flags = flags.SetMask(ProjectorFlags.IsPlaying, value);
        }

        public LightSourceLayer EmitLight
        {
            get => flags.GetRange<ProjectorFlags, LightSourceLayer>(12, ProjectorFlags.MaskLightMode);
            set => flags = flags.SetRange(12, ProjectorFlags.MaskLightMode, value);
        }

        public bool FlipX
        {
            get => flags.HasFlag(ProjectorFlags.FlipX);
            set => flags = flags.SetMask(ProjectorFlags.FlipX, value);
        }
        public bool FlipY
        {
            get => flags.HasFlag(ProjectorFlags.FlipY);
            set => flags = flags.SetMask(ProjectorFlags.FlipY, value);
        }

        public ProjectorFlags flags;
        public int activeSlot;
        public Point pixelOffset;
        public Vector2 alignment;
        public float rotation;
        public Color32 tint;
        public float brightness;
        public ShadingMode shadingSource;
        public DrawLayer drawLayer;
        public ProjectionHideType tileHideType;
        public EmissionMode emissionMode;
        public float drawOrder;
        public float scale;
        public float volume;
        public float audioRangeMin;
        public float audioRangeMax;

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(flags);
            bw.Write(activeSlot);
            bw.Write(pixelOffset);
            bw.Write(alignment);
            bw.Write(tint);
            bw.Write(drawLayer);
            bw.Write(shadingSource);
            bw.Write(tileHideType);
            bw.Write(emissionMode);
            bw.Write(drawOrder);
            bw.Write(brightness);
            bw.Write(scale);
            bw.Write(volume);
            bw.Write(audioRangeMin);
            bw.Write(audioRangeMax);
        }

        public void Deserialize(BinaryReader br)
        {
            flags = br.Read<ProjectorFlags>();
            activeSlot = br.ReadInt32();
            pixelOffset = br.Read<Point>();
            alignment = br.Read<Vector2>();
            tint = br.Read<Color32>();
            drawLayer = br.Read<DrawLayer>();
            shadingSource = br.Read<ShadingMode>();
            tileHideType = br.Read<ProjectionHideType>();
            emissionMode = br.Read<EmissionMode>();
            drawOrder = br.ReadSingle();
            brightness = br.ReadSingle();
            scale = br.ReadSingle();
            volume = br.ReadSingle();
            audioRangeMin = br.ReadSingle();
            audioRangeMax = br.ReadSingle();
        }

        public int Deserialize(ref ReadOnlySpan<byte> span)
        {
            int ogLen = span.Length;
            flags = span.Read<ProjectorFlags>(out span);
            activeSlot = span.Read<int>(out span);
            pixelOffset = span.Read<Point>(out span);
            alignment = span.Read<Vector2>(out span);
            tint = span.Read<Color32>(out span);
            drawLayer = span.Read<DrawLayer>(out span);
            shadingSource = span.Read<ShadingMode>(out span);
            tileHideType = span.Read<ProjectionHideType>(out span);
            emissionMode = span.Read<EmissionMode>(out span);
            drawOrder = span.Read<float>(out span);
            brightness = span.Read<float>(out span);
            scale = span.Read<float>(out span);
            volume = span.Read<float>(out span);
            audioRangeMin = span.Read<float>(out span);
            audioRangeMax = span.Read<float>(out span);
            return ogLen - span.Length;
        }

        public void Save(string name, TagCompound tag)
        {
            TagCompound newTag = new TagCompound();
            newTag.AddEnum("Flags", flags);
            newTag.Assign("ActiveSlot", activeSlot);
            newTag.Assign("PixelOffset", pixelOffset);
            newTag.Assign("Alignment", alignment);
            newTag.Assign("Tint", tint);
            newTag.AddEnum("ShadingSource", shadingSource);
            newTag.AddEnum("TileHide", tileHideType);
            newTag.AddEnum("EmissionMode", emissionMode);
            newTag.AddEnum("DrawLayer", drawLayer);
            newTag.Assign("DrawOrder", drawOrder);
            newTag.Assign("Brightness", brightness);
            newTag.Assign("Scale", scale);
            newTag.Assign("Volume", volume);
            newTag.Assign("AudioRangeMin", audioRangeMin);
            newTag.Assign("AudioRangeMax", audioRangeMax);
            tag.Assign(name, newTag);
        }

        public void Load(TagCompound tag)
        {
            flags = tag.GetEnum("Flags", ProjectorFlags.IsActive);
            activeSlot = tag.GetSafe("ActiveSlot", 0);

            pixelOffset = tag.GetPoint("PixelOffset", default);
            alignment = tag.GetVector2("Alignment", new Vector2(0.5f, 1.0f));
            rotation = tag.GetSafe("Rotation", 0.0f);
            tint = tag.GetColor32("Tint");

            shadingSource = tag.GetEnum<ShadingMode>("ShadingSource");
            tileHideType = tag.GetEnum<ProjectionHideType>("TileHide");
            emissionMode = tag.GetEnum<EmissionMode>("EmissionMode");
            drawLayer = tag.GetEnum<DrawLayer>("DrawLayer");
            drawOrder = tag.GetSafe("DrawOrder", 0.0f);
            scale = Math.Max(tag.GetSafe("Scale", 1.0f), 0.01f);
            brightness = tag.GetSafe("Brightness", 1.0f);
            volume = tag.GetSafe("Volume", 1.0f);
            audioRangeMin = tag.GetSafe("AudioRangeMin", 8.0f);
            audioRangeMax = tag.GetSafe("AudioRangeMax", Math.Max(audioRangeMin + 1.0f, 32.0f));
        }

        public void Reset()
        {
            pixelOffset.X = 0;
            pixelOffset.Y = 0;
            tint = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            alignment = new Vector2(0.5f, 1.0f);
            rotation = 0;
            activeSlot = 0;
            volume = 0.8f;
            drawLayer = DrawLayer.BehindTiles;
            shadingSource = ShadingMode.Tile;
            tileHideType = ProjectionHideType.None;
            emissionMode = EmissionMode.Default;
            drawOrder = 0.0f;
            scale = 1;
            brightness = 0.0f;
            audioRangeMin = 64;
            audioRangeMax = 128;
            flags = flags.SetRange(12, ProjectorFlags.MaskLightMode, 0);
        }
    }
}