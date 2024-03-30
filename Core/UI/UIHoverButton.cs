using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace Projections.Core.UI
{
    public class UIHoverButton : UIElement
    {
        public string hoverText;
        private Rectangle _rect;
        private Asset<Texture2D> _texture;
        private float _visibilityActive = 1f;
        private float _visibilityInactive = 0.4f;

        public UIHoverButton(Asset<Texture2D> texture, string hoverText, Rectangle? rect = null)
        {
            _texture = texture;
            this.hoverText = hoverText;
            _rect = rect != null ? rect.Value : new Rectangle(0, 0, texture.Width(), texture.Height());
            Refresh(true);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
        }

        public void SetVisibility(float whenActive, float whenInactive)
        {
            _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
            _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
        }

        public void Refresh(bool recalc = true)
        {
            Width.Set(_rect.Width, 0f);
            Height.Set(_rect.Height, 0f);

            if (recalc)
            {
                Recalculate();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if(!PlayerInput.IgnoreMouseInterface && ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            spriteBatch.Draw(_texture.Value, dimensions.Position(), _rect, Color.White * (IsMouseHovering ? _visibilityActive : _visibilityInactive));

            if (IsMouseHovering)
            {
                Main.hoverItemName = hoverText;
            }         
        }
    }
}
