using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Projections.Core.UI.Elements
{
    public class UISeparator : UIElementInteractable
    {
        public Color Color;
        private bool _horizontal;
        private float _buffer;
        private string _text;
        private float _textScale;
        private Vector2 _textSize;
        private DynamicSpriteFont _font;

        public UISeparator(bool horizontal, StyleDimension size, float buffer, Color? color = null, string text = "", float textScale = 1)
        {
            _textScale = textScale;
            _font = FontAssets.MouseText.Value;
            _text = text;
            _textSize = ChatManager.GetStringSize(_font, text, new Vector2(_textScale));

            IgnoresMouseInteraction = true;
            _horizontal = horizontal;
            _buffer = buffer;
            Color = color ?? Color.White;
            if (horizontal)
            {
                Width.Set(0, 1);
                Height = new StyleDimension(size.Pixels + buffer + _textSize.Y, size.Precent);
            }
            else
            {
                Width = new StyleDimension(size.Pixels + buffer, size.Precent);
                Height.Set(0, 1);
            }
   
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            var rect = GetDimensions().ToRectangle();

            if (_horizontal)
            {
                rect.Height = (int)(rect.Height - _buffer);
                rect.Y = (int)(rect.Y + _buffer * 0.5f);
            }
            else
            {
                rect.Width = (int)(rect.Width - _buffer);
                rect.X = (int)(rect.X + _buffer * 0.5f);
            }

            if (!string.IsNullOrWhiteSpace(_text) && _horizontal)
            {
                Vector2 pos = new Vector2(rect.X, rect.Y);
                Utils.DrawBorderString(spriteBatch, _text, pos, Color.Gray, _textScale, 0.0f, 0);

                rect.Y = (int)(rect.Y + _textSize.Y);
                rect.Height = (int)(rect.Height - _textSize.Y);
            }

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color);
        }
    }
}
