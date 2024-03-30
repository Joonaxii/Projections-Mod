using Terraria.UI;
using Projections.Core.UI.Elements;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.ID;
using Projections.Core.Systems;
using Microsoft.Xna.Framework.Input;
using Projections.Core.Data;
using Projections.Core.UI;
using Projections.Common.Netcode;
using Projections.Common.PTypes.Streamed;
using Projections.Common.ProjectorTypes;
using System;
using Projections.Core.Utilities;
using Terraria.ModLoader;
using ReLogic.Content;
using Projections.Core.Data.Structures;
using Projections.Common.PTypes;

namespace Projections.Content.UI
{
    public class ProjectorState : UIState
    {
        internal readonly static Color DefaultColor = new Color(63, 82, 151) * 0.7f;
        internal const float X_BUFFER = 10;
        internal const float Y_BUFFER = 0;
        internal const float WIDTH = 620;
        internal const float HEIGHT = 720;
        internal const float ICON_SIZE = ProjectorState.WIDTH / 7;
        internal const float ELEMENT_HEIGHT = 32;

        public Projector Projector => _projector;

        public bool IsDirty
        {
            get => _isDirty;
            internal set => _isDirty = value;
        }

        private UIDraggablePanel _background;
        private ProjectorMainTab _tab;
        private Projector _projector;

        private bool _isDirty;

        public override void OnInitialize()
        {
            _background = new UIDraggablePanel(new Vector2(Main.screenWidth * 0.4f, 200));
            _background.Width.Pixels = WIDTH;
            _background.Height.Pixels = HEIGHT;

            _background.BackgroundColor = new Color(33, 43, 79) * 0.8f;
            _background.SetPadding(0f);

            _background.DragLeft.Set(X_BUFFER + ICON_SIZE, 0);
            _background.DragWidth.Set(0, 1.0f);
            _background.DragHeight.Set(X_BUFFER + ICON_SIZE + 12, 0);

            _background.Recalculate();
            _tab = new ProjectorMainTab(this);
            _tab.Width.Set(0, 1);
            _tab.Height.Pixels = HEIGHT - 32;
            _tab.HAlign = 0.5f;
            _tab.VAlign = 0.5f;
            _tab.Top.Pixels = 10;
            _tab.SetPadding(0f);

            var tex = Main.Assets.Request<Texture2D>("Images/UI/SearchCancel");
            UIImageButton closeButton = new UIImageButton(tex);
            closeButton.Left.Pixels = -5;
            closeButton.Top.Pixels = 5;
            closeButton.HAlign = 1f;
            closeButton.OnLeftClick += (evt, listeningElement) =>
            {
                UISystem.CloseProjectorUI(false);
            };

            _background.Append(closeButton);
            closeButton.Recalculate();

            _background.Append(_tab);
            _tab.Recalculate();

            _background.Recalculate();
            Append(_background);
        }

        public void RefreshUI() => _tab?.RefreshProjectionSlot();

        public void SetProjector(Projector instance, bool apply)
        {
            if (apply)
            {
                _projector = instance;
                _isDirty = false;
                _tab?.Init();
            }
            else
            {
                if (_isDirty)
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        _projector.SendProjectorUpdate(SerializeType.Full, Main.myPlayer);
                    }
                    _isDirty = false;
                }
                _projector = instance;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Main.keyState.IsKeyDown(Keys.Escape))
            {
                Main.blockInput = false;
                UISystem.CloseProjectorUI(false);
                return;
            }

            if (_background.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();
            Main.CloseNPCChatOrSign();
            Main.ClosePlayerChat();
        }
    }

    public class ProjectorMainTab : UIElement
    {
        private UIText _name;
        private UIText _description;
        private UIText _timeUsed;
        private UITextTogglable _currentSlot;
        private UITexturedButton _playButton;
        private UITexturedButton _stopButton;
        private ProjectorItemSlot _projection;
        private UISliderElement<int> _activeSlot;
        private ProjectorSettingsUI _settings;
        protected ProjectorState _state;

        private static Asset<Texture2D>[] _controls = new Asset<Texture2D>[]
        {
            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl1"),
            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl2"),
            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl3"),

            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl4"),
            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl5"),
            ModContent.Request<Texture2D>("Projections/Content/UI/ProjectorControl6"),
        };

        public ProjectorMainTab(ProjectorState state)
        {
            _state = state;
        }

        private void CheckPlayIcon()
        {
            var proj = _state.Projector;
            int ind = proj != null ? (proj.IsPlaying ? 1 : 0) : 0;
            _playButton.SetImage(_controls[ind]);
            _playButton.SetHoverImage(_controls[ind + 3]);
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            const float DIV_PADDING = 10;

            const float DIVIDER_X_POS = ProjectorState.X_BUFFER + ProjectorState.ICON_SIZE + 5;
            const float TEXT_X_POS = DIVIDER_X_POS + 5;
            const float DIVIDER_POS_Y = ProjectorState.Y_BUFFER + ProjectorState.ICON_SIZE + 20;
            const float PANEL_Y_POS = DIVIDER_POS_Y + DIV_PADDING;

            _projection = new ProjectorItemSlot(null, ItemSlot.Context.ChestItem, 1.5f, ItemSlot.Context.InWorld, true);
            _projection.Left.Pixels = ProjectorState.X_BUFFER + ((ProjectorState.ICON_SIZE * 0.5f) - _projection.Width.Pixels * 0.5f);
            _projection.Top.Pixels = ProjectorState.Y_BUFFER + 12;
            _projection.OnItemUpdate += (_) =>
            {
                UpdateInfo();
            };

            _projection.HAlign = 0.0f;
            _projection.VAlign = 0.0f;
            Append(_projection);

            _activeSlot = new UISliderElement<int>(0, 0, 1);
            _activeSlot.ButtonSize = new Vector2(28, 28);
            _activeSlot.AllowWrapping = true;
            _activeSlot.HideInputField = true;
            _activeSlot.Top.Pixels = ProjectorState.Y_BUFFER + ProjectorState.ICON_SIZE;
            _activeSlot.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
            _activeSlot.Width.Set(ProjectorState.ICON_SIZE + 16, 0);
            _activeSlot.Left.Set(ProjectorState.X_BUFFER - 8, 0);
            Append(_activeSlot);

            _currentSlot = new UITextTogglable("Slot: 0", 1.0f);
            _currentSlot.TextOriginY = 0.0f;
            _currentSlot.TextOriginX = 0.5f;
            _currentSlot.Width.Set(ProjectorState.ICON_SIZE, 0);
            _currentSlot.Height.Set(ProjectorState.ELEMENT_HEIGHT - 8, 0);
            _currentSlot.IgnoresMouseInteraction = true;
            _currentSlot.Top.Pixels = ProjectorState.Y_BUFFER - 8;
            _currentSlot.Left.Set(ProjectorState.X_BUFFER - 2, 0);
            Append(_currentSlot);

            _playButton = new UITexturedButton(_controls[0], _controls[3]);
            _playButton.OnLeftClick += (_, _) =>
            {
                var proj = _state.Projector;
                if(proj == null) { return; }
                if (proj.IsPlaying)
                {
                    proj.Stop();
                }
                else
                {
                    proj.Play();
                }
                CheckPlayIcon();
            };
            _playButton.Top.Pixels = ProjectorState.Y_BUFFER + ProjectorState.ICON_SIZE;
            _playButton.Width.Pixels = ProjectorState.ELEMENT_HEIGHT - 8;
            _playButton.Height.Pixels = ProjectorState.ELEMENT_HEIGHT - 8;
            _playButton.Left.Set(ProjectorState.X_BUFFER + (ProjectorState.ICON_SIZE * 0.5f) - _playButton.Width.Pixels, 0);
            Append(_playButton);

            _stopButton = new UITexturedButton(_controls[2], _controls[5]);
            _stopButton.OnLeftClick += (_, _) =>
            {
                var proj = _state.Projector;
                if (proj == null) { return; }
                proj.Deactivate();
                CheckPlayIcon();
            };
            _stopButton.Width.Pixels = ProjectorState.ELEMENT_HEIGHT - 8;
            _stopButton.Height.Pixels = ProjectorState.ELEMENT_HEIGHT - 8;
            _stopButton.Top.Pixels = ProjectorState.Y_BUFFER + ProjectorState.ICON_SIZE;
            _stopButton.Left.Set(_playButton.Left.Pixels + _playButton.Width.Pixels, 0);
            Append(_stopButton);

            _name = new UIText("<Unknown>", 1.35f);
            _name.Left.Set(TEXT_X_POS, 0);
            _name.Top.Set(ProjectorState.Y_BUFFER, 0);
            _name.Width.Set(0, 0.5f);
            _name.Height.Set(24, 0);
            _name.HAlign = 0;
            _name.VAlign = 0.0f;
            _name.TextOriginY = 0.5f;
            _name.TextOriginX = 0.0f;
            Append(_name);
            _name.Recalculate();
            _name.IgnoresMouseInteraction = true;

            _timeUsed = new UIText($"Projected: {TimeSpan.FromSeconds(0.0).ToDurationString()}", 1.0f);
            _timeUsed.IgnoresMouseInteraction = true;
            _timeUsed.Width.Set(0, 0.25f);
            _timeUsed.Height.Set(24, 0);
            _timeUsed.TextOriginY = 0.5f;
            _timeUsed.TextOriginX = 0.0f;
            _timeUsed.Left.Set(TEXT_X_POS, 0.55f);
            _timeUsed.Top.Set(ProjectorState.Y_BUFFER, 0);
            Append(_timeUsed);

            _description = new UIText("No description...", 0.75f);
            _description.Left.Set(TEXT_X_POS + 4, 0);
            _description.Top.Set(ProjectorState.Y_BUFFER + 28, 0);
            _description.Width.Set(0, 1);
            _description.Height.Set(ProjectorState.ICON_SIZE - (ProjectorState.Y_BUFFER + 28), 0);
            _description.HAlign = 0;
            _description.VAlign = 0.0f;
            _description.TextOriginY = 0.0f;
            _description.TextOriginX = 0.0f;
            _description.IgnoresMouseInteraction = true;
            Append(_description);
            _description.Recalculate();


            var sepTop = new UISeparator(true, new StyleDimension(2, 0), 8, Color.Black)
            {
                HAlign = 0.5f,
                VAlign = 0.0f,
                Top = new (DIVIDER_POS_Y, 0),
            };
            Append(sepTop);

            _settings = new ProjectorSettingsUI(((ProjectorState.ELEMENT_HEIGHT * 1.75f) / (Height.Pixels - PANEL_Y_POS)), ProjectorState.ELEMENT_HEIGHT * 1.5f, _state, 1.5f);
            _settings.Left.Set(ProjectorState.X_BUFFER, 0);
            _settings.Top.Pixels = PANEL_Y_POS;
            _settings.HAlign = 0;
            _settings.VAlign = 0;
            _settings.Width.Set(ProjectorState.WIDTH - ProjectorState.X_BUFFER * 2, 0);
            _settings.Height.Set(Height.Pixels - PANEL_Y_POS, 0);
         
            Append(_settings);
            _settings.Recalculate();

            //UpdateInfo(_state.Projector?.Projection, (_state.Projector?.HasProjection).GetValueOrDefault());
            Recalculate();
            Init();
        }

        public void Init()
        {
            //UpdateInfo(_state.Projector?.Projection, (_state.Projector?.HasProjection).GetValueOrDefault());
            RefreshProjectionSlot();
        
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _projection.IsLocked = _activeSlot.IsHovered || _playButton.IsMouseHovering || _stopButton.IsMouseHovering;
        }

        private void CheckProjector()
        {
            _projection.SetProjector(_state.Projector);
            float target = 0;
            if(_state.Projector != null)
            {
                target = _state.Projector.TimeProjectedSec;
            }

            if(Math.Abs(_seconds - target) > 1.0f)
            {
                _seconds = target;
                _timeUsed.SetText($"Projected: {TimeSpan.FromSeconds(_seconds).ToDurationString()}");
            }  
        }
        public void RefreshProjectionSlot()
        {
            CheckProjector();
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            Projector projector = _state.Projector;
            int slots = projector?.SlotCount ?? 1;
            bool isEmpty = projector?.ActiveSlot.IsEmpty ?? true;
            int slotInd = projector?.ActiveSlotIndex ?? 0;

            CheckPlayIcon();

            _playButton.Interactable = !isEmpty;
            _stopButton.Interactable = !isEmpty;

            _activeSlot.min = 0;
            _activeSlot.max = slots - 1;
            _activeSlot.SetValue(slotInd);
            _currentSlot.SetText($"Slot: {slotInd + 1}/{slots}");

            _activeSlot.IsEnabled = slots > 1;
            _currentSlot.IsEnabled = slots > 1;

            Projection projection = projector?.ActiveSlot.Projection;
            _settings.UpdateInfo();
            if (projection != null)
            {
                _name.SetText(projection.Name);
                _description.SetText(projection.Description);
                return;
            }
          
            if (isEmpty)
            {
                _name.SetText("<No Projection>");
                _description.SetText("Nothing here...");
            }
            else
            {
                _name.SetText("<Unknown>");
                _description.SetText("You don't have this projection locally!");
            }
            _settings.UpdateInfo();
        }

        private float _seconds = 0;
        protected override void DrawChildren(SpriteBatch batch)
        {
            CheckProjector();

            int slots = _state.Projector?.SlotCount ?? 1;
            foreach (var element in Elements)
            {
                if(element == _activeSlot  ||
                    element == _currentSlot)
                {
                    if(slots > 1)
                    {
                        element.Draw(batch);
                        int slotI = 0;
                        if (element == _activeSlot && _activeSlot.GetValue(ref slotI))
                        {
                            slotI = _state.Projector.SetActiveSlot(slotI, true);
                            _activeSlot.SetValue(slotI, false);
                            _state.IsDirty = true;
                            RefreshProjectionSlot();
                        }
                    }
                    continue;
                }
                element.Draw(batch);
            }
        }
    }

    public class ProjectorSettingsUI : UIElement
    {
        private static readonly Vector2 DefaultBSize = new Vector2(ProjectorState.ELEMENT_HEIGHT, ProjectorState.ELEMENT_HEIGHT);

        private ProjectorState _mainState;
        private NamedUIElement<UISliderElement<int>> _offsetX;
        private NamedUIElement<UISliderElement<int>> _offsetY;
        private NamedUIElement<UISliderElement<float>> _scale;
        private NamedUIElement<UISliderElement<float>> _alignX;
        private NamedUIElement<UISliderElement<float>> _alignY;
        private NamedUIElement<UISliderElement<float>> _rotation;
        private NamedUIElement<UIBool> _flipX;
        private NamedUIElement<UIBool> _flipY;

        private NamedUIElement<UIEnum<DrawLayer>> _drawLayer;
        private NamedUIElement<UISliderElement<int>> _mask;
        private NamedUIElement<UISliderElement<float>> _drawOrder;
        private NamedUIElement<UIColor> _tint;
        private NamedUIElement<UISliderElement<float>> _brightness;
        private NamedUIElement<UIEnum<LightSourceLayer>> _emitLight;
        private NamedUIElement<UIEnum<ProjectionHideType>> _hideMode;
        private NamedUIElement<UIEnum<EmissionMode>> _emissionMode;
        private NamedUIElement<UIEnum<ShadingMode>> _shadingSource;

        private NamedUIElement<UISliderElement<float>> _volume;
        private NamedUIElement<UISliderElement<float>> _minAudioRange;
        private NamedUIElement<UISliderElement<float>> _maxAudioRange;


        private NamedUIElement<UIEnum<LoopMode>> _loopMode;
        private NamedUIElement<UISliderElement<int>> _loops;
        private NamedUIElement<UISliderElement<int>> _activeFrame;
        private NamedUIElement<UISliderElement<int>> _audioVariant;
        private UIProjectionLayers _layers;

        private UITabMenu _tabBar;

        public ProjectorSettingsUI(float headerSize, float buttonSizes, ProjectorState state, float textScale = 1.0f)
        {
            _mainState = state;

            _tabBar = new UITabMenu(headerSize, buttonSizes, 0, textScale, new string[] { 
                "General Settings", 
                "Rendering Settings", 
                "Audio Settings", 
                "Active Slot Settings",
            });
            _tabBar.Width.Set(0, 1.0f);
            _tabBar.Height.Set(0, 0.85f);
            _tabBar.Top.Set(0, 0.0f);
            Append(_tabBar);
         
            UpdateInfo();
        }

        public override void OnInitialize()
        {
            const float PADDING_Y = 0.0f;
            const float NAME_WIDTH_RATIO = 0.3f;
            const float TEXT_SCALE = 1;

            //Projector Settings
            {
                _tabBar.AddToTab(0, new UISeparator(true, new StyleDimension(2, 0), 12, Color.Black, "Transform", 1.25f));
                _offsetX = new NamedUIElement<UISliderElement<int>>("Offset X",
                    new UISliderElement<int>(int.MinValue, int.MaxValue, 1)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _offsetX.Width.Set(0, 1.0f);
                _offsetX.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _offsetX);

                _offsetY = new NamedUIElement<UISliderElement<int>>("Offset Y",
                 new UISliderElement<int>(int.MinValue, int.MaxValue, 1)
                 {
                     ButtonSize = DefaultBSize
                 }, NAME_WIDTH_RATIO, TEXT_SCALE);
                 _offsetY.Width.Set(0, 1.0f);
                _offsetY.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _offsetY);

                _alignX = new NamedUIElement<UISliderElement<float>>("Alignment X",
                    new UISliderElement<float>(0.0f, 1.0f, 0.1f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _alignX.Width.Set(0, 1.0f);
                _alignX.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _alignX);

                _alignY = new NamedUIElement<UISliderElement<float>>("Alignment Y",
                     new UISliderElement<float>(0.0f, 1.0f, 0.1f)
                     {
                         ButtonSize = DefaultBSize
                     }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _alignY.Width.Set(0, 1.0f);
                _alignY.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _alignY);

                _rotation = new NamedUIElement<UISliderElement<float>>("Rotation",
                     new UISliderElement<float>(0.0f, 359.0f, 1f)
                     {
                         AllowWrapping = true,
                         ButtonSize = DefaultBSize
                     }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _rotation.Width.Set(0, 1.0f);
                _rotation.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _rotation);

                _scale = new NamedUIElement<UISliderElement<float>>("Scale",
                    new UISliderElement<float>(0.001f, float.MaxValue, 0.1f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _scale.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 7, 0);
                _scale.Width.Set(0, 1.0f);
                _scale.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _scale);

                _flipX = new NamedUIElement<UIBool>("Flip X",
                    new UIBool(), NAME_WIDTH_RATIO, TEXT_SCALE, false);
                _flipX.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 8, 0);
                _flipX.Width.Set(0, 1.0f);
                _flipX.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _flipX);

                _flipY = new NamedUIElement<UIBool>("Flip Y",
                    new UIBool(), NAME_WIDTH_RATIO, TEXT_SCALE, false);
                _flipY.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 9, 0);
                _flipY.Width.Set(0, 1.0f);
                _flipY.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(0, _flipY);
            }

            //Render Settings
            {
                _tabBar.AddToTab(1, new UISeparator(true, new StyleDimension(2, 0), 12, Color.Black, "Rendering", 1.25f));

                _drawLayer = new NamedUIElement<UIEnum<DrawLayer>>("Draw Layer",
                    new UIEnum<DrawLayer>(0, (int)DrawLayer.__Count)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _drawLayer.Width.Set(0, 1.0f);
                _drawLayer.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _drawLayer);

                _drawOrder = new NamedUIElement<UISliderElement<float>>("Draw Order",
                    new UISliderElement<float>(float.MinValue, float.MaxValue, 1.0f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _drawOrder.Width.Set(0, 1.0f);
                _drawOrder.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _drawOrder);


                _hideMode = new NamedUIElement<UIEnum<ProjectionHideType>>("Hide Mode",
                     new UIEnum<ProjectionHideType>()
                     {
                         ButtonSize = DefaultBSize
                     }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _hideMode.Width.Set(0, 1.0f);
                _hideMode.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _hideMode);

                _tint = new NamedUIElement<UIColor>("Tint",
               new UIColor(Color32.White), NAME_WIDTH_RATIO, TEXT_SCALE);
                _tint.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 6, 0);
                _tint.Width.Set(0, 1.0f);
                _tint.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _tint);

                _brightness = new NamedUIElement<UISliderElement<float>>("Brightness",
               new UISliderElement<float>(0.0f, 100.0f, 1.0f)
               {
                   ButtonSize = DefaultBSize
               }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _brightness.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 6, 0);
                _brightness.Width.Set(0, 1.0f);
                _brightness.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _brightness);

                _emitLight = new NamedUIElement<UIEnum<LightSourceLayer>>("Light Source",
                    new UIEnum<LightSourceLayer>()
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _emitLight.Width.Set(0, 1.0f);
                _emitLight.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _emitLight);

                _emissionMode = new NamedUIElement<UIEnum<EmissionMode>>("Emission Mode",
                    new UIEnum<EmissionMode>()
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _emissionMode.Top.Set((ProjectorState.ELEMENT_HEIGHT + PADDING_Y) * 11, 0);
                _emissionMode.Width.Set(0, 1.0f);
                _emissionMode.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(1, _emissionMode);

                _shadingSource = new NamedUIElement<UIEnum<ShadingMode>>("Shading", new UIEnum<ShadingMode>()
                {
                    ButtonSize = DefaultBSize
                }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _shadingSource.Width.Set(0, 1.0f);
                _shadingSource.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                //_tabBar.AddToTab(1, _shadingSource);
            }

            //Audio Settings
            {
                _tabBar.AddToTab(2, new UISeparator(true, new StyleDimension(2, 0), 12, Color.Black, "Audio", 1.25f));

                _volume = new NamedUIElement<UISliderElement<float>>("Volume",
                    new UISliderElement<float>(0.0f, 100.0f, 1.0f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _volume.Width.Set(0, 1.0f);
                _volume.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(2, _volume);   
                
                
                _minAudioRange = new NamedUIElement<UISliderElement<float>>("Audio Tile Range (Min)",
                    new UISliderElement<float>(0.0f, 1024, 1.0f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _minAudioRange.Width.Set(0, 1.0f);
                _minAudioRange.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(2, _minAudioRange);

                _maxAudioRange = new NamedUIElement<UISliderElement<float>>("Audio Tile Range (Max)",
                    new UISliderElement<float>(0.0f, 1024, 1.0f)
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _maxAudioRange.Width.Set(0, 1.0f);
                _maxAudioRange.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(2, _maxAudioRange);
            }

            { // Active Slot Settings
                _tabBar.AddToTab(3, new UISeparator(true, new StyleDimension(2, 0), 12, Color.Black, "Active Slot Settings", 1.25f));
                _loopMode = new NamedUIElement<UIEnum<LoopMode>>("Loop Mode",
                    new UIEnum<LoopMode>()
                    {
                        ButtonSize = DefaultBSize
                    }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _loopMode.Width.Set(0, 1.0f);
                _loopMode.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(3, _loopMode);

                _loops = new NamedUIElement<UISliderElement<int>>("Loops",
                    new UISliderElement<int>(0, int.MaxValue, 1)
                    {
                        ButtonSize = DefaultBSize
                    }
                    , NAME_WIDTH_RATIO, TEXT_SCALE);
                _loops.Width.Set(0, 1.0f);
                _loops.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(3, _loops);

                _activeFrame = new NamedUIElement<UISliderElement<int>>("Active Frame",
                    new UISliderElement<int>(0, int.MaxValue, 1)
                    {
                        ButtonSize = DefaultBSize
                    }
                    , NAME_WIDTH_RATIO, TEXT_SCALE);
                _activeFrame.Width.Set(0, 1.0f);
                _activeFrame.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(3, _activeFrame);

                _audioVariant = new NamedUIElement<UISliderElement<int>>("Audio Variant",
                    new UISliderElement<int>(0, int.MaxValue, 1)
                    {
                        ButtonSize = DefaultBSize
                    }
                    , NAME_WIDTH_RATIO, TEXT_SCALE);

                _mask = new NamedUIElement<UISliderElement<int>>("Alpha Mask",
                 new UISliderElement<int>(-1, -1, -1, 1, (ref int value) => value < 0 ? "None" : $"Mask #{value}")
                 {
                     ButtonSize = DefaultBSize
                 }, NAME_WIDTH_RATIO, TEXT_SCALE);
                _mask.Width.Set(0, 1.0f);
                _mask.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(3, _mask);

                _audioVariant.Width.Set(0, 1.0f);
                _audioVariant.Height.Set(ProjectorState.ELEMENT_HEIGHT, 0);
                _tabBar.AddToTab(3, _audioVariant);

                _tabBar.AddToTab(3, new UISeparator(true, new StyleDimension(2, 0), 12, Color.Black, "Layer Setup", 1.25f));
                _layers = new UIProjectionLayers(ProjectorState.ELEMENT_HEIGHT);
                _layers.Width.Set(0, 1.0f);
                _layers.Height.Set(0, 1.0f);
                _tabBar.AddToTab(3, _layers);

            }
            Recalculate();
        }

        public void UpdateInfo()
        {
            var proj = _mainState.Projector;
            if (proj == null) { return; }

            _offsetX.Element.SetValue(proj.Settings.pixelOffset.X, false);
            _offsetY.Element.SetValue(proj.Settings.pixelOffset.Y, false);
            _alignX.Element.SetValue(proj.Settings.alignment.X, false);
            _alignY.Element.SetValue(proj.Settings.alignment.Y, false);
            _rotation.Element.SetValue(proj.Settings.rotation, false);
            _scale.Element.SetValue(proj.Settings.scale, false);

            _flipY.Element.SetValue(proj.Settings.FlipY, false);
            _flipY.Element.SetValue(proj.Settings.FlipY, false);
            _drawLayer.Element.SetValue(proj.Settings.drawLayer, false);
            _drawOrder.Element.SetValue(proj.Settings.drawOrder, false);
            _hideMode.Element.SetValue(proj.Settings.tileHideType, false);
            _brightness.Element.SetValue(proj.Settings.brightness * 100.0f, false);
            _tint.Element.SetValue(proj.Settings.tint, false);
            _emitLight.Element.SetValue(proj.Settings.EmitLight, false);
            _emissionMode.Element.SetValue(proj.Settings.emissionMode, false);

            _volume.Element.SetValue(proj.Settings.volume * 100.0f, false);
            _minAudioRange.Element.SetValue(proj.Settings.audioRangeMin, false);
            _maxAudioRange.Element.SetValue(proj.Settings.audioRangeMax, false);

            _shadingSource.Element.SetValue(proj.Settings.shadingSource, false);

            ref var active = ref proj.ActiveSlot;    
            _layers.Setup(_mainState.Projector);
            var projection = active.Projection;

            _layers.Interactable = !active.IsEmpty;
            _loopMode.Interactable = !active.IsEmpty;
            _loops.Interactable = !active.IsEmpty;

            _loops.Element.SetValue(active.Loops, false);
            _loopMode.Element.SetValue(active.LoopMode, false);

            _activeFrame.Interactable = !active.IsEmpty;
            _audioVariant.Interactable = !active.IsEmpty;
            _mask.Interactable = !active.IsEmpty && (active.Projection?.MaskCount ?? 0) > 0;

            _activeFrame.Element.max = Math.Max((projection?.FrameCount ?? 0) - 1, 0);
            _audioVariant.Element.max = Math.Max((projection?.AudioInfo.variants ?? 0) - 1, 0);
            _mask.Element.max = (projection?.MaskCount ?? 0) - 1;

            _activeFrame.IsEnabled = projection != null && (projection?.AnimationMode ?? AnimationMode.FrameSet) == AnimationMode.FrameSet;
            _audioVariant.IsEnabled = projection != null && (projection?.AnimationMode ?? AnimationMode.FrameSet) != AnimationMode.FrameSet && (projection?.HasAudio ?? false);
            _mask.IsEnabled = !active.IsEmpty && (projection?.MaskCount ?? 0) > 0;

            if (_activeFrame.IsEnabled)
            {
                _activeFrame.Element.SetValue(active.ActiveFrame, false);
            }

            if (_audioVariant.IsEnabled)
            {
                _audioVariant.Element.SetValue(active.AudioVariant, false);
            }

            if (_mask.IsEnabled)
            {
                _mask.Element.SetValue(active.Mask, false);
            }
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            ApplyChanges();
        }

        private void ApplyChanges()
        {
            var proj = _mainState.Projector;
            if (proj == null) { return; }

            ValueBuffer temp = default;
            {  // Transform Settings
                if (_offsetX.Element.GetValue(ref temp.As<int>()))
                {
                    proj.Settings.pixelOffset.X = temp.As<int>();
                    _mainState.IsDirty = true;
                }

                if (_offsetY.Element.GetValue(ref temp.As<int>()))
                {
                    proj.Settings.pixelOffset.Y = temp.As<int>();
                    _mainState.IsDirty = true;
                }

                if (_alignX.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.alignment.X = temp.As<float>();
                    _mainState.IsDirty = true;
                }

                if (_alignY.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.alignment.Y = temp.As<float>();
                    _mainState.IsDirty = true;
                }

                if (_rotation.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.rotation = temp.As<float>();
                    _mainState.IsDirty = true;
                }

                if (_scale.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.scale = Utils.Clamp(temp.As<float>(), 1, 10);
                    _mainState.IsDirty = true;
                }

                if (_flipX.Element.GetValue(ref temp.As<bool>()))
                {
                    proj.Settings.FlipX = temp.As<bool>();
                    _mainState.IsDirty = true;
                }

                if (_flipX.Element.GetValue(ref temp.As<bool>()))
                {
                    proj.Settings.FlipY = temp.As<bool>();
                    _mainState.IsDirty = true;
                }
            }


            { // Rendering Settings
                if (_drawLayer.Element.GetValue(ref temp.As<DrawLayer>()))
                {
                    proj.Settings.drawLayer = temp.As<DrawLayer>();
                    _mainState.IsDirty = true;
                }

                if (_drawOrder.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.drawOrder = Utils.Clamp(temp.As<float>(), -8192.0f, 8192.0f);
                    _mainState.IsDirty = true;
                }

                if (_hideMode.Element.GetValue(ref temp.As<ProjectionHideType>()))
                {
                    proj.Settings.tileHideType = temp.As<ProjectionHideType>();
                    _mainState.IsDirty = true;
                }

                if (_brightness.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.brightness = Utils.Clamp(temp.As<float>() * 0.01f, 0, 1);
                    _mainState.IsDirty = true;
                }
                
                if (_tint.Element.GetValue(ref temp.As<Color32>()))
                {
                    proj.Settings.tint = temp.As<Color32>();
                    _mainState.IsDirty = true;
                }

                if (_emitLight.Element.GetValue(ref temp.As<LightSourceLayer>()))
                {
                    proj.Settings.EmitLight = temp.As<LightSourceLayer>();
                    _mainState.IsDirty = true;
                }

                if (_emissionMode.Element.GetValue(ref temp.As<EmissionMode>()))
                {
                    proj.Settings.emissionMode = temp.As<EmissionMode>();
                    _mainState.IsDirty = true;
                }

                if (_shadingSource.Element.GetValue(ref temp.As<ShadingMode>()))
                {
                    proj.Settings.shadingSource = temp.As<ShadingMode>();
                    _mainState.IsDirty = true;
                }
            }


            { // Audio Settings

                if (_volume.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.volume = temp.As<float>() * 0.01f;
                    _mainState.IsDirty = true;
                }

                if (_minAudioRange.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.audioRangeMin = temp.As<float>();
                    _mainState.IsDirty = true;
                    ProjectorSystem.RefreshAudioRange(_mainState.Projector);
                }

                if (_maxAudioRange.Element.GetValue(ref temp.As<float>()))
                {
                    proj.Settings.audioRangeMax = temp.As<float>();
                    _mainState.IsDirty = true;
                    ProjectorSystem.RefreshAudioRange(_mainState.Projector);
                }
            }

            if(!proj.ActiveSlot.IsEmpty) 
            {
                if(_layers.GetValue(ref temp.As<ushort>()))
                {
                    proj.ActiveSlot.LayerState = temp.As<ushort>();
                    _mainState.IsDirty = true;
                    // TODO: Mark Projector for redraw. 
                }

                if (proj.ActiveSlot.HasAudio)
                {
                    if(_audioVariant.Element.GetValue(ref temp.As<int>()))
                    {
                        proj.ActiveSlot.AudioVariant = temp.As<int>();
                        proj.Validate();
                        _mainState.IsDirty = true;
                    }
                }

            }
        }
    }

    public class ProjectorLayerPanel : UIElement
    {
        private ProjectorState _mainState;

        private UITabMenu _tabBar;
        public ProjectorLayerPanel(float headerSize, float buttonSizes, ProjectorState state)
        {
            _mainState = state;

            _tabBar = new UITabMenu(headerSize, buttonSizes, 0, 1.0f, new string[] { "Projector Settings", "Layer Setup" });
            _tabBar.Width.Set(0, 1.0f);
            _tabBar.Height.Set(0, 0.85f);
            _tabBar.Top.Set(0, 0.0f);
            Append(_tabBar);

            UpdateInfo();
            Recalculate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            CheckChangedSettings();
        }

        private void CheckChangedSettings()
        {
            var proj = _mainState.Projector;
            if (proj == null) { return; }

            int temp = 0;
            
        }

        public void UpdateInfo()
        {
            //var proj = _mainState.Projector?.Projection;
            //if (proj != null && _mainState.Projector.IsValid)
            {
                //var layers = proj.Variants;
                //if (layers.Length > 0)
                //{
                //    string[] layersN = new string[proj.Count];
                //    for (int i = 0; i < layersN.Length; i++)
                //    {
                //        layersN[i] = proj[i].name;
                //    }
                //    _currentVariant.Element.Setup(layersN, layersN.Length);
                //    _currentVariant.Element.SetValue(Math.Min(_currentVariant.Element.Index, layers.Length - 1), false);
                //    return;
                //}
            }
            //_currentVariant.Element.Setup(null, 0);
        }
    }
}
