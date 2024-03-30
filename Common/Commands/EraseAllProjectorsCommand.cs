using Microsoft.Xna.Framework;
using Projections.Common.Netcode;
using Projections.Core.Systems;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Common.Commands
{
    public class EraseAllProjectorsCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "erase_projectors";
        public override string Usage => "/erase_projectors <type> [-noeject]";
        public override string Description => "Erases all Tile Projectors from the world, ejecting Projections inside unless otherwise stated.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool noEject = false;
            bool canDo = caller.Player.IsHost();

            if (!canDo)
            {
                Main.NewText("You don't have the rights to run the EraseAllProjectors command!", Color.Red);
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-noeject", StringComparison.InvariantCultureIgnoreCase))
                {
                    noEject = true;
                }
            }
            ProjectorSystem.Instance?.EraseAllProjectors(!noEject, false);
        }
    }
}
