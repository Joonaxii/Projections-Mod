using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Projections.Common.Commands
{
    public class ReloadProjectionsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "reload_projections";
        public override string Usage => "/reload_projections";
        public override string Description => "Reloads all projections.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Projections.ReloadAllProjections())
            {
                Main.NewText("Marked Projections for reload!", Color.Green);
            }
            else
            {
                Main.NewText("Failed to reload Projections!", Color.Red);
            }
        }
    }
}
