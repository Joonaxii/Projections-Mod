using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Projections.Core.Systems;
using Terraria.Localization;
using Terraria;
using Projections.Common.Netcode;

namespace Projections.Common.Configs
{
    public class ProjectionsServerConfig : ModConfig
    {
        public static ProjectionsServerConfig Instance
        {
            get => ModContent.GetInstance<ProjectionsServerConfig>();
        }
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(true)]
        public bool AllowProjectionsInFrontOfPlayer { get; set; }

        [DefaultValue(-1), Range(-1, int.MaxValue)]
        public int MaxProjectionValue { get; set; }

        [DefaultValue(false)]
        public bool DisablePrices { get; set; }

        [DefaultValue(false)]
        public bool AllowProjectionGiveCommands { get; set; }

        [DefaultValue(true)]
        public bool BuildIgnoreListFromOpenVoidBag { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
        {
            if (Main.player[whoAmI].IsHost())
            {
                return base.AcceptClientChanges(pendingConfig, whoAmI, ref message);
            }
            message = NetworkText.FromLiteral("You cannot change the server config because you are not the host!");
            return false;
        }

        public override void OnChanged()
        {
            ProjectorSystem.ValidateProjectors();
        }
    }
}
