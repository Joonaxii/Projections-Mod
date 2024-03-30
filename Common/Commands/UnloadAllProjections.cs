using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Projections.Common.Commands
{
    public class UnloadAllProjections : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "unload_projections";
        public override string Usage => "/unload_projections";
        public override string Description => "Unloads all Projections, allowing changes to be made to Projections that are on disk.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!Projections.IsLoaded)
            {
                Main.NewText("Projections are already unloaded!", Color.Yellow);
                return;
            }

            if (Projections.UnloadAllProjections())
            {
                Main.NewText("Marked all Projections for unloading!", Color.Green);
            }
            else
            {
                Main.NewText("Failed to unload Projections!", Color.Red);
            }
        }
    }
}
