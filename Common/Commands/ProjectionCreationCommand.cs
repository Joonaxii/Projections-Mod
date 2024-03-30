using Microsoft.Xna.Framework;
using Projections.Common.Configs;
using Projections.Common.Netcode;
using Projections.Content.Items;
using Projections.Core.Data;
using Projections.Core.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Common.Commands
{
    public class ProjectionCreationCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "projection";
        public override string Usage => "/projection <name> [-m -b -c <num> -p <index>] (Example: /projection Test:Item -c 12 -m)";
        public override string Description => "Creates a Projection or a P-Material with a specified stack size.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {          
            bool allowed = (ProjectionsServerConfig.Instance?.AllowProjectionsInFrontOfPlayer ?? false) || caller.Player.IsHost();

            if (!allowed)
            {
                Main.NewText("Not allowed to create Projections via command!", Color.Red);
                return;
            }

            if (args.Length < 1)
            {
                Main.NewText("Not enough arguments!", Color.Red);
                return;
            }

            var idx = args[0].ParseProjectionID();
            PType type = PType.TProjection;
            int count = 1;
            int player = -1;
            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    var str = args[i];
                    switch (str)
                    {
                        case "-m":
                        case "-M":
                            type = PType.TPMaterial;
                            break;
                        case "-b":
                        case "-B":
                            type = PType.TPBundle;
                            break;
                        case "-c":
                        case "-C":
                            if (++i < args.Length)
                            {
                                count = Utils.Clamp(int.TryParse(args[i], out int val) ? val : 1, 1, 9999);
                            }
                            break;

                        case "-p":
                        case "-P":
                            if (++i < args.Length)
                            {
                                player = int.TryParse(args[i], out int val) ? val : -1;
                            }
                            break;
                    }
                }
            }

            player = player < 0 || player >= Main.maxPlayers ? caller.Player.whoAmI : player;
            ProjectionNetUtils.SpawnProjectionItem(type.GetProjectionItemID(), idx, Main.player[player].Center, count);
        }
    }
}
