using Projections.Core.UI.Elements;
using System;

namespace Projections.Core.UI
{
    public class UITabBar : UIElementInteractable
    {
        public string CurrentTab
        {
            get => _currentTab < 0 || _currentTab >= _tabs.Length ? "" : _tabs[_currentTab].GetText();
            set
            {
                for (int i = 0; i < _tabs.Length; i++)
                {

                }
            }
        }
        public int TabIndex
        {
            get => _currentTab;
            set
            {
                _currentTab = value;
            }
        }

        public Action<int> OnTabChange;

        private int _currentTab;
        private UIButton[] _tabs;

        public UITabBar(params string[] tabs)
        {
            _currentTab = 0;
            _tabs = new UIButton[tabs?.Length ?? 0];
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i] = new UIButton("");
            }
        }

        public bool SetTab(int tab)
        {
            if(tab >= _tabs.Length || tab < 0 || tab == _currentTab)
            {
                return false;
            }

            _tabs[_currentTab].Selected = false;
            _tabs[tab].Selected = true;
            _currentTab = tab;
            OnTabChange?.Invoke(_currentTab);
            return true;
        }
    }
}
