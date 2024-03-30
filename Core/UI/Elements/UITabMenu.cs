using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.UI;

namespace Projections.Core.UI.Elements
{
    public class UIRegion : UIElementInteractable
    {
        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            float yPos = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                var element = Elements[i];
                if(element == null || (element is UIElementInteractable interact && !interact.IsEnabled))
                {
                    continue;
                }

                element.Top.Set(yPos, 0);
                element.Recalculate();
                element.Draw(spriteBatch);
                yPos += element.GetDimensions().Height;
            }
        }

        public IList<UIElement> GetElements()
        {
            return Elements;
        }
    }

    public class UITabMenu : UIElementInteractable
    {
        private UIStringSelect _curTab;
        private UIRegion[] _tabAreas = new UIRegion[0];
        private float _tabPercent;

        public UITabMenu(float tabHeight, float buttonSize, int initial, float textScale, params string[] tabs)
        {
            _tabPercent = tabHeight;

            _curTab = new UIStringSelect(true, tabs.Length, textScale)
            {
                ButtonSize = new Vector2(buttonSize, buttonSize),
            };

            _curTab.SetValue(initial, false);
            _curTab.Setup(tabs, tabs.Length);
            _curTab.Height.Set(0, _tabPercent);
            _curTab.Width.Set(0, 1);

            Array.Resize(ref _tabAreas, tabs.Length);
            for (int i = 0; i < _tabAreas.Length; i++)
            {
                _tabAreas[i] = new UIRegion();
            }
        }

        public IList<UIElement> GetTabElements(int tab)
        {
            return _tabAreas[tab].GetElements();
        }

        public void AddToTab(int tab, UIElement tabElement) 
        {
            _tabAreas[tab].Append(tabElement);
        }

        public void SetTab(int tab)
        {
            _curTab.SetValue(tab, false);
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            Append(_curTab);
            for (int i = 0; i < _tabAreas.Length; i++)
            {
                var area = _tabAreas[i];
                area.Width.Set(0, 1.0f);
                area.Height.Set(0, 1.0f - _tabPercent);
                area.Top.Set(0, _tabPercent);
                Append(area);
            }
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            _curTab.Draw(spriteBatch);
            for (int i = 0; i < _tabAreas.Length; i++)
            {
                if(_curTab.Value == i)
                {
                    _tabAreas[i].IgnoresMouseInteraction = false;
                    _tabAreas[i].Draw(spriteBatch);
                }
                else
                {
                    _tabAreas[i].IgnoresMouseInteraction = true;
                }
            }
        }
    }
}
