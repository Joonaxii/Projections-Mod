using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Projections.Common.Commands
{
    public class LoadProjectionsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "load_projections";
        public override string Usage => "/load_projections";
        public override string Description => "Loads all projections if projections have been unloaded.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Projections.IsLoaded)
            {
                Main.NewText("Projections are already loaded!", Color.Yellow);
                return;
            }

            if (Projections.LoadAllProjections())
            {
                Main.NewText("Marked Projections for loading!", Color.Green);
            }
            else
            {
                Main.NewText("Failed to load Projections!", Color.Red);
            }
        }
    }
}
