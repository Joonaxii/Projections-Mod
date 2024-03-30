using MonoMod.Cil;
using Projections.Core.Data;
using Projections.Core.Systems;
using Terraria;

namespace Projections.Common.ILEdits
{
    internal class RenderingIL : ILEdit
    {
        public void Init()
        {
            IL_Main.DoDraw_WallsTilesNPCs += PatchWallTilesNPCDraw;
            IL_Main.DoDraw += PatchDoDraw;
        }


        public void Deinit()
        {
            IL_Main.DoDraw_WallsTilesNPCs -= PatchWallTilesNPCDraw;
            IL_Main.DoDraw -= PatchDoDraw;
        }

        private static void PatchWallTilesNPCDraw(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCall(CommonIL.Main_DoDraw_WallsAndBlacks)))
            {
                cursor.EmitDelegate(ProjectorSystem.DrawBehindWalls);
            }
            else
            {
                Projections.Log(LogType.Error, $"Failed to match first target for emitting {nameof(ProjectorSystem.DrawBehindWalls)}");
            }

            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt(CommonIL.SpriteBatch_End)))
            {
                cursor.EmitDelegate(ProjectorSystem.DrawBehindTiles);
            }
            else
            {
                Projections.Log(LogType.Error, $"Failed to match first target for emitting {nameof(ProjectorSystem.DrawBehindTiles)}");
            }

            if (cursor.TryGotoNext(MoveType.Before,
               i => i.MatchLdarg(0),
               i => i.MatchCall(CommonIL.Main_DrawPlayers_BehindNPCs)))
            {
                cursor.EmitDelegate(ProjectorSystem.DrawAfterTiles);
            }
            else
            {
                Projections.Log(LogType.Error, $"Failed to match first target for emitting {nameof(ProjectorSystem.DrawAfterTiles)}");
            }
        }

        private static void PatchDoDraw(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall(CommonIL.Main_DrawPlayers_AfterProjectiles)))
            {
                cursor.EmitDelegate(ProjectorSystem.DrawAfterPlayers);
            }
            else
            {
                Projections.Log(LogType.Error, $"Failed to match first target for emitting {nameof(ProjectorSystem.DrawAfterPlayers)}");
            }
        }
    }
}
