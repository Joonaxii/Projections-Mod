using Mono.Cecil.Cil;
using MonoMod.Cil;
using Projections.Core.Systems;
using Terraria;

namespace Projections.Common.ILEdits
{
    internal class NPCLootIL : ILEdit
    {
        public void Init()
        {
            IL_NPC.NPCLoot += PatchNPCLoot;
        }

        public void Deinit()
        {
            IL_NPC.NPCLoot -= PatchNPCLoot;
        }

        private static void PatchNPCLoot(ILContext ctx)
        {
            ILCursor cursor = new ILCursor(ctx);
            // We move the Meteor Head + Hard Mode check after the other things in the if statement 
            // and run HandleProjectionLoot before the Meteor Head + Hard Mode check, this way
            // the projections/materials assigned to Meteor Heads can drop from them in hard mode too
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg0(),
                i => i.MatchLdfld(CommonIL.NPC_type),
                i => i.Match(OpCodes.Ldc_I4_S, (sbyte)0x17),
                i => i.Match(OpCodes.Bne_Un_S),
                i => i.MatchLdsfld(CommonIL.Main_hardMode),
                i => i.Match(OpCodes.Brtrue_S)))
            {
                // We only want to delete the check if the edits later on succeed.
                // So we cache the current cursord index and return there to 
                // remove it after we find the new place to move the check to
                int meteorHeadCheckIdx = cursor.Index;
                var meteorLabel = cursor.DefineLabel();
                var prekillLabel = cursor.DefineLabel();

                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall(CommonIL.NPCLoader_PreKill)
                    ))
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.MarkLabelPrev(meteorLabel);
                    cursor.EmitDelegate(HandleProjectionLoot);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitLdfld(CommonIL.NPC_type);
                    cursor.Emit(OpCodes.Ldc_I4_S, (sbyte)0x17);
                    cursor.Emit(OpCodes.Bne_Un_S, prekillLabel);
                    cursor.EmitLdsfld(CommonIL.Main_hardMode);
                    cursor.Emit(OpCodes.Brfalse_S, prekillLabel);
                    cursor.EmitRet();
                    cursor.MarkLabel(prekillLabel);

                    // First we "defuse" the first meteor head check,
                    // the check is a jump label for an eralier if statement 
                    // so we'll just change the enemy type check to 0 which should
                    // never trigger while also allowing the previous if to jump properly.
                    cursor.Index = meteorHeadCheckIdx + 2;
                    cursor.Next.Operand = (sbyte)0;
                    while (cursor.Next.OpCode != OpCodes.Ret)
                    {
                        var current = cursor.Next;
                        if (current.Operand is ILLabel label && label.IsSameLabel(prekillLabel))
                        {
                            current.Operand = meteorLabel;
                        }
                        cursor.Index++;
                    }
                    cursor.GotoLabel(prekillLabel, MoveType.Before);
                }
            }
        }

        private static void HandleProjectionLoot(NPC npc)
            => ItemDropManager.DoDrops(npc, 1.0f, 0.0f);
    }
}
