using Microsoft.Xna.Framework;
using Projections.Core.Data.Structures;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Projections.Common.Configs
{
    public class ProjectionsClientConfig : ModConfig
    {
        public static ProjectionsClientConfig Instance
        {
            get => ModContent.GetInstance<ProjectionsClientConfig>();
        }
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Range(0.0f, 100.0f)]
        [DefaultValue(80.0f)]
        [Slider]
        public float Volume { get; set; }

        [Range(0.0f, 100.0f)]
        [DefaultValue(100.0f)]
        [Slider]
        public float PlayerVolume { get; set; }

        [DefaultValue(typeof(Color), "255, 255, 255, 255")]
        public Color PlayerProjectionTint { get; set; }

        [DefaultValue(90.25f), Range(0.0f, 100.0f)]
        public float PlayerInterfaceXOffset { get; set; }

        [DefaultValue(16.0f), Range(0.0f, 100.0f)]
        public float PlayerInterfaceYOffset { get; set; }

        [DefaultValue(true)]
        public bool ShowTraderWareRefreshMessage { get; set; }

    }
}
