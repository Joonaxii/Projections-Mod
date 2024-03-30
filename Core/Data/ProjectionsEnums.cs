using Projections.Common.Players;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Common.PTypes.Streamed;
using Projections.Content.Items;
using Projections.Content.Items.RarityStones;
using Projections.Core.Data.Structures;
using System;
using System.Runtime.InteropServices;
using Terraria;

namespace Projections.Core.Data
{
    [Flags]
    internal enum ReadFrameFlags : byte
    {
        None = 0x00,
        Diffuse = 0x1,
        Emission = 0x2,
    }

    internal enum FrameFormat : byte
    {
        RGBA32,
        Indexed8,
        Indexed16,
    }

    /// <summary>
    /// Flags used by <see cref="ProjectorSettings"/>. 
    /// </summary>
    public enum ProjectorFlags : uint
    {
        None = 0x00,
        IsActive = 0x01,
        IsPlaying = 0x02,
        FlipX = 0x04,
        FlipY = 0x08,
        EmitDiffuse = 0x10,
        EmitEmission = 0x20,

        MaskLightMode = 0x40 | 0x80
    }

    /// <summary>
    ///  <see cref="Projection"/> layer flags, first 16 bits are reserved/used for generic flags, the last 16 bits are implementation specific.
    /// </summary>
    public enum LayerFlags : uint
    {
        None = 0x00,
        DefaultOn = 0x01,

        __Mask_Impl_Defined = 0xFF_FF_00_00U,
    }

    /// <summary>
    /// Flags used for <see cref="Projection"/> Frames, first 16 bits are reserved/used for generic flags, the last 16 bits are implementation specific.
    /// </summary>
    public enum FrameFlags : uint
    {
        None = 0x00,

        __Mask_Impl_Defined = 0xFF_FF_00_00U,
    }

    /// <summary>
    /// <see cref="Projection"/>, <see cref="PMaterial"/>, and <see cref="PBundle"/> pool types
    /// </summary>
    public enum PoolType : byte
    {
        Trader,
        NPC,
        FishingQuest,
        Treasure,
        __Count
    }

    /// <summary>
    /// Type for ingridients used in <see cref="Projection"/>, <see cref="PMaterial"/>, and <see cref="PBundle"/> recipes.
    /// </summary>
    public enum RecipeType : byte
    {
        None = 0x00,
        Vanilla,
        Modded,
        Projection,
        ProjectionMaterial,
        ProjectionBundle,
    }

    /// <summary>
    /// Various supported texture formats, primarily used when reading <see cref="Projection"/>, <see cref="PMaterial"/>, and <see cref="PBundle"/> Icon data from disk.
    /// </summary>
    public enum TexFormat : byte
    {
        None,
        PNG,
        DDS,

        /// <summary>
        /// My own texture format similar to BMP, very fast to read from disk, but doesn't use compression.
        /// </summary>
        JTEX,

        /// <summary>
        /// My implementation of an RLE compression for pixel data, very fast to decode, might not work for every kind of texture.
        /// <para>Primarily used for <see cref="StreamedProjection"/> frames' pixel data and <see cref="StreamedPMaterial"/> icons etc.</para>
        /// </summary>
        RLE,
    }

    /// <summary>
    /// <see cref="ProjectionItem" />'s original source type, only really used when doing calculations related to the item's coin value.
    /// </summary>
    public enum ProjectionSourceType : byte
    {
        None = 0x00,
        Crafted,
        Drop,
        Shop,
    }

    /// <summary>
    /// The serialization type used when sending <see cref="Projector"/> updates over the network.
    /// </summary>
    public enum SerializeType : byte
    {
        Partial,
        Full
    }

    /// <summary>
    /// Flags for <see cref="ProjectionsPlayer"/>.
    /// </summary>
    public enum PPlayerFlags : byte
    {
        None = 0x00,
        CanProject = 0x01,
    }

    /// <summary>
    /// Audio related information, first 4 bits are used for the audio type, the last 4 bits are used/reserved for audio related flags.
    /// </summary>
    public enum AudioType : byte
    {
        SFX,
        Music,
        Ambient,

        __TypeMask = 0x0F,
        __StereoFlag = 0x80,
    }

    /// <summary>
    /// The general Projector types for <see cref="Projector"/>.
    /// </summary>
    public enum ProjectorType : byte
    {
        /// <summary>
        /// Used for projectors that are tied to tiles.
        /// </summary>
        Tile,

        /// <summary>
        /// Used for projectors that a player has equipped.
        /// </summary>
        Player,

        /// <summary>
        /// Used for every other type of projector.
        /// </summary>
        Custom,

        __Count,
    }

    /// <summary>
    /// Used to tell the projector what to do when X number of loops have been reached for a given <see cref="ProjectorSlot"/>, behavior might differ depending on the <see cref="AnimationMode"/> of the <see cref="Projection"/>.
    /// </summary>
    public enum LoopMode : byte
    {
        Infinite = 0x00,
        Next = 0x01,
        NextSlot = 0x03,
        Stop = 0x04,
    }

    /// <summary>
    /// Used to tell if a given <see cref="ProjectionIndex"/> is a <see cref="Projection"/>, <see cref="PMaterial"/> or a <see cref="PBundle"/>.
    /// </summary>
    public enum PType : byte
    {
        TProjection,
        TPMaterial,
        TPBundle,
    }

    internal enum PlayerUpdateType : byte
    {
        None = 0x00,
        Time = 0x01,
        Flags = 0x02,
        Obtained = 0x04,
        ResetObtained = 0x08,

        All = 0xFF,
    }

    internal enum MessageType : byte
    {
        CreateProjector,
        KillProjector,
        UpdateProjector,
        UpdateProjectorSlot,
        
        PlayerTime,
        PlayerObtainedProjection,
        PlayerProjectorFlags,
        EraseAllProjectors,

        ClientResetDropInfo,
        ClientSendDropInfo,
        ClientSendDropInfoChunk,

        ClientUpdateCustom,
    }

    /// <summary>
    /// The layer to draw the <see cref="Projection"/> on.
    /// </summary>
    public enum DrawLayer : byte
    {
        BehindWalls,
        BehindTiles,
        AfterTiles,
        AfterPlayers,

        __Count
    }

    /// <summary>
    /// Currently unused, but is planned to be used for telling which parts of a <see cref="Projection"/> emit light in the game world.
    /// </summary>
    public enum LightSourceLayer : byte
    {
        None = 0x00,
        BaseColor = 0x01,
        Emission = 0x02,
        Both = 0x03,
    }

    /// <summary>
    /// Tells the default <see cref="Projector"/> drawing implementation which parts of a projector should <b>NOT</b> be drawn.
    /// </summary>
    public enum ProjectionHideType : byte
    {
        None = 0x00,
        Diffuse = 0x01,
        Emission = 0x02,
        Both = 0x03,
    }

    /// <summary>
    /// Currently unused, potentially used in the future. Tells the <see cref="Projector"/> how to apply lighting to the projection.
    /// </summary>
    public enum ShadingMode : byte
    {
        Projector = 0x00,
        Tile = 0x01,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum AnimationMode : byte
    {
        /// <summary>
        /// Frames are treated as alterante versions of the <see cref="Projection"/>, loops are per-frame based on the frame duration.
        /// </summary>
        FrameSet = 0x00,

        /// <summary>
        /// Frames are used to control loops.
        /// </summary>
        LoopVideo = 0x01,

        /// <summary>
        /// Audio is used to control loops. If no audio is present, falls back to <see cref="AnimationMode.LoopVideo"/>.
        /// </summary>
        LoopAudio = 0x02,
    }

    internal enum PFrameFlags : uint
    {
        None = 0x00,

        HasDiffuse = 0x01,
        CompressedDiffuse = 0x02,
        UseTargetDiffuse = 0x04,

        HasEmission = 0x08,
        CompressedEmission = 0x10,
        UseTargetEmission = 0x20,
    }

    /// <summary>
    /// Various flags related to biome/"biome effects", primarily used with <see cref="Projection"/> and <see cref="PMaterial"/> drops etc.
    /// </summary>
    public enum BiomeConditions : ulong
    {
        None = 0x00000000,
        Sky = 0x00000001,
        Surface = 0x00000002,
        Underground = 0x00000004,
        Caverns = 0x00000008,
        Underworld = 0x00000010,

        MaskElevation = Sky | Surface | Underground | Caverns | Underworld,

        Forest = 0x00000020,
        Desert = 0x00000040,
        Ocean = 0x00000080,
        Snow = 0x00000100,
        Jungle = 0x00000200,
        Meteorite = 0x00000400,
        Mushroom = 0x00000800,
        Dungeon = 0x00001000,
        Temple = 0x00002000,
        Aether = 0x00004000,
        BeeHive = 0x00008000,
        Granite = 0x00010000,
        Marble = 0x00020000,
        Graveyard = 0x00040000,

        MaskBiome = Forest | Desert | Ocean | Snow | Jungle | Meteorite | Mushroom | Dungeon | Temple | Aether | BeeHive | Granite | Marble | Graveyard,

        InOldOnesArmy = 0x00080000,
        Town = 0x00100000,
        InWaterCandle = 0x00200000,
        InPeaceCandle = 0x00400000,
        InShadowCandle = 0x00800000,
        TowerSolar = 0x01000000,
        TowerNebula = 0x02000000,
        TowerVortex = 0x04000000,
        TowerStardust = 0x08000000,

        MaskEffect = InOldOnesArmy | Town | InWaterCandle | InPeaceCandle | InShadowCandle | TowerSolar | TowerNebula | TowerVortex | TowerStardust,

        Purity = 0x10000000,
        Corruption = 0x20000000,
        Crimson = 0x40000000,
        Hallowed = 0x80000000,

        MaskPurity = Purity | Corruption | Crimson | Hallowed,

        RequireAll_00 = 0x100000000,
        RequireAll_01 = 0x200000000,
        RequireAll_02 = 0x400000000,
        RequireAll_03 = 0x800000000,
    }

    /// <summary>
    /// Various flags related to the game world's current state, primarily used with <see cref="Projection"/> and <see cref="PMaterial"/> drops etc.
    /// </summary>
    public enum WorldConditions : ulong
    {
        None = 0,

        IsHardmode = 0x0001,
        IsRegular = 0x0002,
        IsExpert = 0x0004,
        IsMaster = 0x0008,
        IsDay = 0x0010,
        IsNight = 0x0020,
        IsBloodMoon = 0x0040,
        IsEclipse = 0x0080,
        IsPumpkinMoon = 0x0100,
        IsFrostMoon = 0x0200,
        IsHalloween = 0x0400,
        IsChristmas = 0x0800,

        IsSandstorm = 0x1000,
        IsRaining = 0x2000,
        IsStorm = 0x4000,
        ___RESERVED = 0x8000,

        MainMask = 0xFFFF,

        DWN_KingSlime = 0x00_00_00_00_01_00_00UL,
        DWN_EyeOfCthulhu = 0x00_00_00_00_02_00_00UL,
        DWN_EaterOfWorld = 0x00_00_00_00_04_00_00UL,
        DWN_BrainOfCthulhu = 0x00_00_00_00_08_00_00UL,
        DWN_QueenBee = 0x00_00_00_00_10_00_00UL,
        DWN_Skeletron = 0x00_00_00_00_20_00_00UL,
        DWN_Deerclops = 0x00_00_00_00_40_00_00UL,
        DWN_WallOfFlesh = 0x00_00_00_00_80_00_00UL,
        DWN_QueenSlime = 0x00_00_00_01_00_00_00UL,
        DWN_TheTwins = 0x00_00_00_02_00_00_00UL,
        DWN_TheDestroyer = 0x00_00_00_04_00_00_00UL,
        DWN_SkeletronPrime = 0x00_00_00_08_00_00_00UL,
        DWN_Plantera = 0x00_00_00_10_00_00_00UL,
        DWN_Golem = 0x00_00_00_20_00_00_00UL,
        DWN_DukeFishron = 0x00_00_00_40_00_00_00UL,
        DWN_EmpressOfLight = 0x00_00_00_80_00_00_00UL,
        DWN_LunaticCultist = 0x00_00_01_00_00_00_00UL,
        DWN_MoonLord = 0x00_00_02_00_00_00_00UL,
        DWN_MourningWood = 0x00_00_04_00_00_00_00UL,
        DWN_Pumpking = 0x00_00_08_00_00_00_00UL,
        DWN_Everscream = 0x00_00_10_00_00_00_00UL,
        DWN_SantaNK1 = 0x00_00_20_00_00_00_00UL,
        DWN_IceQueen = 0x00_00_40_00_00_00_00UL,
        DWN_SloarPillar = 0x00_00_80_00_00_00_00UL,
        DWN_NebulaPillar = 0x00_01_00_00_00_00_00UL,
        DWN_VortexPillar = 0x00_02_00_00_00_00_00UL,
        DWN_StardustPillar = 0x00_04_00_00_00_00_00UL,
        DWN_GoblinArmy = 0x00_08_00_00_00_00_00UL,
        DWN_Pirates = 0x00_10_00_00_00_00_00UL,
        DWN_FrostLegion = 0x00_20_00_00_00_00_00UL,
        DWN_Martians = 0x00_40_00_00_00_00_00UL,
        DWN_PumpkinMoon = 0x00_80_00_00_00_00_00UL,
        DWN_FrostMoon = 0x01_00_00_00_00_00_00UL,
        RequireAllDown = 0x80_00_00_00_00_00_00UL,
    }

    /// <summary>
    /// Rarity of the <see cref="Projection"/> or <see cref="PMaterial"/>, primarily used for selecting the <see cref="RarityStone"/> for crafting recipes of <see cref="Projection"/> and for visuals.
    /// </summary>
    public enum PRarity : byte
    {
        Basic,
        Intermediate,
        Advanced,
        Expert,
        Master,

        __Count
    }

    internal enum LogType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Flags used by <see cref="PMaterial"/> to allow things like Shimmering etc.
    /// </summary>
    public enum PMaterialFlags : byte
    {
        None = 0x00,

        /// <summary>
        /// Currently unused, but might be used in the future.
        /// </summary>
        AllowUpload = 0x01,

        AllowShimmer = 0x02,
    }

    /// <summary>
    /// How the <see cref="Projector"/> should display emissive parts of the <see cref="Projection"/>.
    /// </summary>
    public enum EmissionMode : byte
    {
        Default,
        OnlyWhenDark,
    }

    /// <summary>
    /// Flags used by <see cref="DropSource"/>.
    /// </summary>
    public enum PDropFlags : byte
    {
        None = 0x00,
        IsModded = 0x01,
        IsNetID = 0x02,
    }

    /// <summary>
    /// Various extension methods related to enums.
    /// </summary>
    public static class EnumExt
    {
        public static T GetRange<T>(this T input, int shift, T mask) where T : unmanaged
        {
            return GetRange<T, T>(input, shift, mask);
        }

        public static U GetRange<T, U>(this T input, int shift, T mask) where T : unmanaged where U : unmanaged
        {
            unsafe
            {
                Span<ulong> curValue = stackalloc ulong[2];
                Span<byte> spn = MemoryMarshal.AsBytes(curValue);
                MemoryMarshal.Write<T>(spn, ref input);
                MemoryMarshal.Write<T>(spn.Slice(8), ref mask);

                curValue[0] >>= shift;
                curValue[0] &= curValue[1];
                return MemoryMarshal.Read<U>(spn.Slice(0, sizeof(U)));
            }
        }

        public static T SetRange<T>(this T input, int shift, T mask, T value) where T : unmanaged
        {
            return SetRange<T, T>(input, shift, mask, value);
        }

        public static T SetRange<T, U>(this T input, int shift, T mask, U value) where T : unmanaged where U : unmanaged
        {
            unsafe
            {
                Span<ulong> curValue = stackalloc ulong[3];
                Span<byte> spn = MemoryMarshal.AsBytes(curValue);
                Span<byte> spnT = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref input, 1));
                MemoryMarshal.Write<T>(spn, ref input);
                MemoryMarshal.Write<T>(spn.Slice(8), ref mask);
                MemoryMarshal.Write<U>(spn.Slice(16), ref value);

                curValue[0] = (curValue[0] & ~(curValue[1] << shift)) | ((curValue[2] & curValue[1]) << shift);
                return MemoryMarshal.Read<T>(spn.Slice(0, sizeof(T)));
            }
        }

        public static T SetMask<T>(this T input, T mask, bool value) where T : unmanaged
        {
            unsafe
            {
                Span<ulong> curValue = stackalloc ulong[2];
                Span<byte> spn = MemoryMarshal.AsBytes(curValue);
                MemoryMarshal.Write<T>(spn, ref input);
                MemoryMarshal.Write<T>(spn.Slice(8), ref mask);

                curValue[0] = value ? (curValue[0] | curValue[1]) : (curValue[0] & ~curValue[1]);
                return MemoryMarshal.Read<T>(spn.Slice(0, sizeof(T)));
            }
        }

        public static bool AreMet(this WorldConditions worldFlags, BiomeConditions biomeFlags)
        {
            WorldConditions flags = 0;

            if (Main.masterMode) { flags |= WorldConditions.IsMaster; }
            else if (Main.expertMode) { flags |= WorldConditions.IsExpert; }
            else { flags |= WorldConditions.IsRegular; }

            if (Main.hardMode) { flags |= WorldConditions.IsHardmode; }

            flags |= Main.IsItDay() ? WorldConditions.IsDay : WorldConditions.IsNight;

            if (Main.bloodMoon) { flags |= WorldConditions.IsBloodMoon; }
            if (Main.eclipse) { flags |= WorldConditions.IsEclipse; }

            if (Main.IsItStorming) { flags |= WorldConditions.IsStorm; }
            if (Main.IsItRaining) { flags |= WorldConditions.IsRaining; }

            if (Main.halloween) { flags |= WorldConditions.IsHalloween; }
            if (Main.xMas) { flags |= WorldConditions.IsChristmas; }
            if (Main.pumpkinMoon) { flags |= WorldConditions.IsPumpkinMoon; }
            if (Main.snowMoon) { flags |= WorldConditions.IsFrostMoon; }

            if ((flags & WorldConditions.MainMask) != 0 && (flags & WorldConditions.MainMask & worldFlags) == 0)
            {
                return false;
            }

            var wrldFlags = flags & (WorldConditions)0xFFFFFFFF0000U;
            if (wrldFlags != 0)
            {
                Span<bool> bosses = stackalloc bool[33]
                {
                    NPC.downedSlimeKing,
                    NPC.downedBoss1,
                    NPC.downedBoss2,
                    NPC.downedBoss2,
                    NPC.downedQueenBee,
                    NPC.downedBoss3,
                    NPC.downedDeerclops,
                    Main.hardMode,
                    NPC.downedQueenSlime,
                    NPC.downedMechBoss2,
                    NPC.downedMechBoss1,
                    NPC.downedMechBoss3,
                    NPC.downedPlantBoss,
                    NPC.downedGolemBoss,
                    NPC.downedFishron,
                    NPC.downedEmpressOfLight,
                    NPC.downedAncientCultist,
                    NPC.downedMoonlord,
                    NPC.downedHalloweenTree,
                    NPC.downedHalloweenKing,
                    NPC.downedChristmasTree,
                    NPC.downedChristmasSantank,
                    NPC.downedChristmasIceQueen,
                    NPC.downedTowerSolar,
                    NPC.downedTowerNebula,
                    NPC.downedTowerVortex,
                    NPC.downedTowerStardust,
                    NPC.downedGoblins,
                    NPC.downedPirates,
                    NPC.downedFrost,
                    NPC.downedMartians,
                    NPC.downedHalloweenTree | NPC.downedHalloweenKing,
                    NPC.downedChristmasTree | NPC.downedChristmasSantank | NPC.downedChristmasIceQueen,
                };

                bool allDown = flags.HasFlag(WorldConditions.RequireAllDown);
                var tFlags = WorldConditions.None;

                for (ulong i = 0, j = 0x010000; i < 33; i++, j <<= 1)
                {
                    if (bosses[(int)i])
                    {
                        tFlags |= (WorldConditions)j;
                    }
                }
                tFlags &= wrldFlags;
                if (allDown ? tFlags != wrldFlags : tFlags == WorldConditions.None)
                {
                    return false;
                }
            }

            var biomes = biomeFlags & (BiomeConditions.MaskPurity | BiomeConditions.MaskBiome | BiomeConditions.MaskElevation | BiomeConditions.MaskEffect);
            if (biomes != 0)
            {
                var plr = Main.LocalPlayer;
                Span<bool> curElevation = stackalloc bool[5]
                {
                    plr.ZoneSkyHeight,
                    plr.ZoneOverworldHeight,
                    plr.ZoneDirtLayerHeight,
                    plr.ZoneRockLayerHeight,
                    plr.ZoneUnderworldHeight,
                };
                Span<bool> curBiomes = stackalloc bool[14]
               {
                    plr.ZoneOverworldHeight,
                    plr.ZoneDesert,
                    plr.ZoneBeach,
                    plr.ZoneSnow,
                    plr.ZoneJungle,
                    plr.ZoneMeteor,
                    plr.ZoneGlowshroom,
                    plr.ZoneDungeon,
                    plr.ZoneLihzhardTemple,
                    plr.ZoneShimmer,
                    plr.ZoneHive,
                    plr.ZoneGranite,
                    plr.ZoneMarble,
                    plr.ZoneGraveyard,
                };
                Span<bool> curEffect = stackalloc bool[9]
                {
                    plr.ZoneOldOneArmy,
                    plr.townNPCs > 2.0f,
                    plr.ZoneWaterCandle,
                    plr.ZonePeaceCandle,
                    plr.ZoneShadowCandle,
                    plr.ZoneTowerSolar,
                    plr.ZoneTowerNebula,
                    plr.ZoneTowerVortex,
                    plr.ZoneTowerStardust,
                };
                Span<bool> curEvil = stackalloc bool[4]
                {
                    plr.ZonePurity,
                    plr.ZoneCorrupt,
                    plr.ZoneCrimson,
                    plr.ZoneHallow,
                };

                bool reqAll = biomeFlags.HasFlag(BiomeConditions.RequireAll_00);
                BiomeConditions tempCur = BiomeConditions.None;
                var tmpIn = biomes & BiomeConditions.MaskElevation;
                if (tmpIn != BiomeConditions.None)
                {
                    //Elevation
                    for (ulong i = 0, j = 1; i < 5; i++, j <<= 1)
                    {
                        if (curElevation[(int)i])
                        {
                            tempCur |= (BiomeConditions)j;
                        }
                    }
                    tempCur &= biomes;
                    if (reqAll ? tmpIn != tempCur : tempCur == BiomeConditions.None)
                    {
                        return false;
                    }
                }

                reqAll = biomeFlags.HasFlag(BiomeConditions.RequireAll_01);
                tempCur = BiomeConditions.None;

                tmpIn = biomes & BiomeConditions.MaskBiome;
                if (tmpIn != BiomeConditions.None)
                {
                    //Biome
                    for (ulong i = 0, j = (ulong)BiomeConditions.Forest; i < 14; i++, j <<= 1)
                    {
                        if (curBiomes[(int)i])
                        {
                            tempCur |= (BiomeConditions)j;
                        }
                    }
                    tempCur &= biomes;
                    if (reqAll ? tmpIn != tempCur : tempCur == BiomeConditions.None)
                    {
                        return false;
                    }
                }

                reqAll = biomeFlags.HasFlag(BiomeConditions.RequireAll_02);
                tempCur = BiomeConditions.None;
                tmpIn = biomes & BiomeConditions.MaskEffect;

                if (tmpIn != BiomeConditions.None)
                {
                    //Effect
                    for (ulong i = 0, j = (ulong)BiomeConditions.InOldOnesArmy; i < 9; i++, j <<= 1)
                    {
                        if (curEffect[(int)i])
                        {
                            tempCur |= (BiomeConditions)j;
                        }
                    }
                    tempCur &= biomes;
                    if (reqAll ? tmpIn != tempCur : tempCur == BiomeConditions.None)
                    {
                        return false;
                    }
                }

                reqAll = biomeFlags.HasFlag(BiomeConditions.RequireAll_03);
                tempCur = BiomeConditions.None;
                tmpIn = biomes & BiomeConditions.MaskPurity;
                if (tmpIn != BiomeConditions.None)
                {
                    //Purity
                    for (ulong i = 0, j = (ulong)BiomeConditions.Purity; i < 4; i++, j <<= 1)
                    {
                        if (curEvil[(int)i])
                        {
                            tempCur |= (BiomeConditions)j;
                        }
                    }
                    tempCur &= biomes;
                    if (reqAll ? tmpIn != tempCur : tempCur == BiomeConditions.None)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
