using Microsoft.Xna.Framework.Graphics;
using Projections.Common.Configs;
using Projections.Common.Players;
using Projections.Core.UI.Elements;
using Projections.Core.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.UI;

namespace Projections.Content.UI
{
    public class PlayerInterfaceState : UIState
    {
        private const float SLOT_SCALE = 0.875f;
        private PlayerProjectorSlot[] _projectionSlots = new PlayerProjectorSlot[ProjectionsPlayer.PLAYER_PROJECTORS];

        private Asset<Texture2D> _creativeUIIcon;

        public override void OnInitialize()
        {    
            _creativeUIIcon ??= Main.Assets.Request<Texture2D>("Images/UI/Creative/Infinite_Icons");
            for (int i = 0; i < _projectionSlots.Length; i++)
            {
                if (_projectionSlots[i] == null)
                {
                    _projectionSlots[i] = new PlayerProjectorSlot(i, SLOT_SCALE);
                    Append(_projectionSlots[i]);
                }
            }
        }

        public void Init()
        {
            var plr = Main.LocalPlayer.PPlayer();
            for (int i = 0; i < _projectionSlots.Length; i++)
            {
                _projectionSlots[i].Setup(plr);
            }
            Recalculate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            var pPlayer = Main.LocalPlayer?.PPlayer();
            if(pPlayer == null) { return; }
  
            if (pPlayer.CanProject && Main.EquipPage == 2)
            {
                var config = ProjectionsClientConfig.Instance;

                float originX = (config.PlayerInterfaceXOffset * 0.01f) * Main.screenWidth;
                float originY = (Main.mapStyle == 1 ? Main.miniMapHeight + 16 : 0.0f) + (config.PlayerInterfaceYOffset * 0.01f) * Main.screenHeight;

                for (int i = 0; i < _projectionSlots.Length; i++)
                {
                    var proj = _projectionSlots[i];
                    proj.Left.Set(originX, 0.0f);
                    proj.Top.Set(originY, 0.0f);
                    proj.Draw(spriteBatch);
                    originY += proj.Height.Pixels + 2;
                }
            }
        }
    }
}
