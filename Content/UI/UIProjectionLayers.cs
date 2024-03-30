using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Core.UI.Elements;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace Projections.Content.UI
{
    public class UIProjectionLayers : UIElementInteractable
    {
        private const int MAX_PER_COLUMN = 8;
        private UIProjectionLayer[] _allLayers = new UIProjectionLayer[Projection.MAX_LAYERS];

        private bool _isDirty;
        private Projector _projector;
        private ushort _mask;

        public UIProjectionLayers(float height)
        {
            int numOfColumns = (int)Math.Max(Math.Ceiling(_allLayers.Length / (float)MAX_PER_COLUMN), 1);
            float widthOfColumn = 1.0f / numOfColumns;

            float xPos = 0.0f;
            float yPos = 0.0f;
            for (int i = 0, j = 1; i < _allLayers.Length; i++, j++)
            {
                _allLayers[i] = new UIProjectionLayer(i, height);
                _allLayers[i].Left.Set(0, xPos);
                _allLayers[i].Top.Set(yPos, 0.0f);
                _allLayers[i].Width.Set(0, widthOfColumn);
                _allLayers[i].Height.Set(height, 0);

                yPos += height;
                if (j >= MAX_PER_COLUMN)
                {
                    j = 0;
                    xPos += widthOfColumn;
                    yPos = 0;
                }
                Append(_allLayers[i]);
            }
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            Recalculate();
        }

        public void Setup(Projector projector)
        {
            _projector = projector;
            int count = _projector?.ActiveSlot.Projection?.Layers.Length ?? 0;
            for (int i = 0; i < _allLayers.Length; i++)
            {
                _allLayers[i].SetProjector(i < count ? _projector : null);
            }
            _mask = _projector?.ActiveSlot.LayerState ?? 0x00;
            _isDirty = false;
        }

        public override void Update(GameTime gameTime)
        {
            int count = _projector?.ActiveSlot.Projection?.Layers.Length ?? 0;

            bool val = false; 
            for (int i = 0, j = 1; i < count; i++, j <<= 1)
            {
                var lr = _allLayers[i];
                if (lr.GetValue(ref val))
                {
                    _mask = (ushort)(val ? (_mask | j) : (_mask & ~j));
                    _isDirty = true;
                }
            }
        }

        public bool GetValue(ref ushort value)
        {
            if (_isDirty)
            {
                value = _mask;
                _isDirty = false;
                return true;
            }
            return false;
        }
    }

    public class UIProjectionLayer : UIElementInteractable
    {
        private UIText _name;
        private UIBool _toggle;
        private bool _isDirty;
        private Projector _projector;
        private int _layer;

        public UIProjectionLayer(int layer, float size)
        {
            _layer = layer;
            Width.Set(0, 1);
            Height.Set(size, 0);

            _name = new UIText("Layer");
            _toggle = new UIBool();
            _toggle.Width.Set(0, 0.5f);
            _toggle.Height.Set(size, 0);
            _toggle.Left.Set(0, 0.5f);

            _toggle.OnLeftClick += (_, _) => 
            {
                var proj = _projector?.ActiveSlot.Projection;
                if (proj == null)
                {
                    return;
                }

                if(proj.IsLayerUnlocked(_layer, _projector.ActiveSlot.Stack, out _))
                {
                    _projector.ActiveSlot.ToggleLayer(_layer, _toggle.Value);
                    _isDirty = true;
                    return;
                }
                _toggle.SetValue(_projector.ActiveSlot.IsLayerEnabled(_layer), false, false, true);
            };
            _name.TextOriginX = 0.0f;
            _name.Width.Set(0, 0.5f);

            Append(_name);
            Append(_toggle);
        }

        public void SetProjector(Projector projector)
        {
            _projector = projector;
            _isDirty = false;
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            var proj = _projector?.ActiveSlot.Projection;
            _isDirty = false;
            if (proj == null)
            {
                _name.SetText("----------");
                _name.TextColor = Colors.RarityTrash;
                _toggle.Interactable = false;
                _toggle.overrideText = "----------";
                _toggle.RefreshUI();
                return;
            }

            if(_layer >= proj.Layers.Length)
            {
                _name.SetText("----------");
                _name.TextColor = Colors.RarityTrash;
                _toggle.Interactable = false;
                _toggle.overrideText = "----------";
                _toggle.RefreshUI();
                return;
            }

            _name.SetText(proj.Layers[_layer].Name);
            if (!proj.IsLayerUnlocked(_layer, _projector.ActiveSlot.Stack, out int unlocksAt))
            {
                _toggle.Interactable = false;
                _name.TextColor = Colors.RarityTrash;
                _toggle.overrideText = $"Unlocks At X{unlocksAt}!";
                _toggle.RefreshUI();
                return;
            }
            _name.TextColor = Color.White;
            _toggle.Interactable = true;
            _toggle.overrideText = "";
            _toggle.RefreshUI();
            _toggle.SetValue(_projector.ActiveSlot.IsLayerEnabled(_layer), false, false, true);
        }

        public bool GetValue(ref bool value)
        {
            if (_isDirty)
            {
                value = _toggle.Value;
                _isDirty = false;
                return true;
            }
            return false;
        }
    }
}