using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Projections.Core.UI
{
    public class UIDraggablePanel : UIPanel
    {
        public Func<UIElement, bool> ShouldDrawChild;

        public ref StyleDimension DragTop => ref _dragTop;
        public ref StyleDimension DragLeft => ref _dragLeft;
        public ref StyleDimension DragWidth => ref _dragWidth;
        public ref StyleDimension DragHeight => ref _dragHeight;

        private StyleDimension _dragTop;
        private StyleDimension _dragLeft;
        private StyleDimension _dragWidth;
        private StyleDimension _dragHeight;

        private Vector2 _offset;
        private bool _dragging;

        public UIDraggablePanel(Vector2 position = default)
        {
            _dragTop.Set(0, 0);
            _dragLeft.Set(0, 0);
            _dragWidth.Set(0, 1);
            _dragHeight.Set(0, 1);

            Left.Set(position.X, 0f);
            Top.Set(position.Y, 0f);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            DragStart(evt);
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            DragEnd(evt);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (_dragging)
            {
                Left.Set(Main.mouseX - _offset.X, 0f);
                Top.Set(Main.mouseY - _offset.Y, 0f);
                Recalculate();
            }

            var parentSpace = Parent.GetDimensions().ToRectangle();
            if (!GetDimensions().ToRectangle().Intersects(parentSpace))
            {
                Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
                Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
                Recalculate();
            }
        }
        public override void OnDeactivate()
        {
            StopDrag();
            base.OnDeactivate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            foreach (var element in Elements)
            {
                if (ShouldDrawChild?.Invoke(element) ?? true)
                {
                    element.Draw(spriteBatch);
                }
            }
        }

        public void StopDrag()
        {
            _dragging = false;
            _offset = default;
        }

        private static bool ContainsPoint(Vector2 point, ref CalculatedStyle dimensions)
        {
            if (point.X > dimensions.X && point.Y > dimensions.Y && point.X < dimensions.X + dimensions.Width)
            {
                return point.Y < dimensions.Y + dimensions.Height;
            }
            return false;
        }

        private void DragStart(UIMouseEvent evt)
        {
            if (_dragging) { return; }
            CalculatedStyle dimensions = new CalculatedStyle();

            var selfDim = GetDimensions();

            float dX = _dragLeft.GetValue(selfDim.Width);
            float dY = _dragTop.GetValue(selfDim.Height);

            dimensions.X = selfDim.X + dX;
            dimensions.Y = selfDim.Y + dY;
            dimensions.Width = _dragWidth.GetValue(selfDim.Width - dX);
            dimensions.Height = _dragHeight.GetValue(selfDim.Height - dY);

            if (!ContainsPoint(evt.MousePosition, ref dimensions))
            {
                return;
            }

            _offset = new Vector2(
                evt.MousePosition.X - Left.Pixels,
                evt.MousePosition.Y - Top.Pixels);
            _dragging = true;
        }

        private void DragEnd(UIMouseEvent evt)
        {
            if (!_dragging) { return; }
            Vector2 endMousePosition = evt.MousePosition;
            _dragging = false;

            Left.Set(endMousePosition.X - _offset.X, 0f);
            Top.Set(endMousePosition.Y - _offset.Y, 0f);

            Recalculate();
        }
    }
}
