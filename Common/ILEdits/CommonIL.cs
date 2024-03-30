using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace Projections.Common.ILEdits
{
    internal static class CommonIL
    {
        internal delegate void IL_EditCall();

        public static FieldInfo Main_player { get; } =
            typeof(Main).GetField(nameof(Main.player), BindingFlags.Public | BindingFlags.Static);

        public static FieldInfo Main_hardMode { get; } =
            typeof(Main).GetField(nameof(Main.hardMode), BindingFlags.Public | BindingFlags.Static);

        public static MethodInfo NPC_CountKillForAchievements { get; } =
          typeof(NPC).GetMethod("CountKillForAchievements", BindingFlags.NonPublic | BindingFlags.Instance);

        public static FieldInfo NPC_type { get; } =
            typeof(NPC).GetField(nameof(NPC.type), BindingFlags.Public | BindingFlags.Instance);

        public static MethodInfo NPC_AnyInteractions { get; } =
          typeof(NPC).GetMethod(nameof(NPC.AnyInteractions), BindingFlags.Public | BindingFlags.Instance);

        public static MethodInfo NPCLoader_PreKill { get; } =
           typeof(NPCLoader).GetMethod(nameof(NPCLoader.PreKill), BindingFlags.Public | BindingFlags.Static);

        public static MethodInfo Main_DoDraw_WallsAndBlacks { get; } =
           typeof(Main).GetMethod("DoDraw_WallsAndBlacks", BindingFlags.NonPublic | BindingFlags.Instance);

        public static MethodInfo Main_DrawPlayers_BehindNPCs { get; } =
            typeof(Main).GetMethod("DrawPlayers_BehindNPCs", BindingFlags.NonPublic | BindingFlags.Instance);

        public static MethodInfo Main_DrawPlayers_AfterProjectiles { get; } =
            typeof(Main).GetMethod("DrawPlayers_AfterProjectiles", BindingFlags.NonPublic | BindingFlags.Instance);

        public static MethodInfo SpriteBatch_End { get; } =
             typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.End), BindingFlags.Public | BindingFlags.Instance);

        public static MethodInfo Item_PickAnItemSlotTOSpawnItemOn { get; } =
            typeof(Item).GetMethod("PickAnItemSlotTOSpawnItemOn", BindingFlags.NonPublic | BindingFlags.Static);


        private static ILEdit[] _edits = new ILEdit[]
        {
            new QuickStackIL(),
            new NPCLootIL(),
            new RenderingIL(),
        };

        public static void Init()
        {
            foreach (var edit in _edits)
            {
                edit.Init();
            }
        }

        public static void Deinit()
        {
            foreach (var edit in _edits)
            {
                edit.Deinit();
            }
        }

        internal static void MarkLabelPrev(this ILCursor cursor, ILLabel label)
        {
            cursor.Index--;
            cursor.MarkLabel(label);
            cursor.Index++;
        }

        internal static bool IsSameLabel(this ILLabel lhs, ILLabel other)
        {
            return lhs.Target == other.Target;
        }
    }
}
