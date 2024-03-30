using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.PTypes;
using Projections.Core.Data.Structures;
using Projections.Core.Maths;
using Projections.Core.Utilities;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Reflection;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Projections.Core.UI.Elements
{
    internal struct ValueBuffer
    {
        public ulong data;
        public string strVal;
        public ref T As<T>() where T : unmanaged
        {
            var spn = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
            return ref MemoryMarshal.AsRef<T>(spn);
        }
    }

    internal static class UIReflection
    {
        private static readonly ReflectableField<UIImageButton, float> _buttonVisibilityActive;
        private static readonly ReflectableField<UIImageButton, float> _buttonVisibilityInactive;

        static UIReflection()
        {
            _buttonVisibilityActive = new ReflectableField<UIImageButton, float>("_visibilityActive", BindingFlags.Instance | BindingFlags.NonPublic);
            _buttonVisibilityInactive = new ReflectableField<UIImageButton, float>("_visibilityInactive", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static void GetVisibility(this UIImageButton button, out float active, out float inactive)
        {
            active = _buttonVisibilityActive.Get(button);
            inactive = _buttonVisibilityInactive.Get(button);
        }
    }

    internal static class UIExtensions
    {
        public static void DrawTinted(this UIText text, SpriteBatch batch, float tint)
        {
            Color prevT = text.TextColor;
            Color prevS = text.ShadowColor;

            text.TextColor = prevT * tint;
            text.ShadowColor = prevS * tint;
            text.Draw(batch);
            text.TextColor = prevT;
            text.ShadowColor = prevS;
        }

        public static void DrawTinted(this UIPanel panel, SpriteBatch batch, float tint)
        {
            Color prevBR = panel.BorderColor;
            Color prevBG = panel.BackgroundColor;

            panel.BorderColor = prevBR * tint;
            panel.BackgroundColor = prevBG * tint;
            panel.Draw(batch);
            panel.BorderColor = prevBR;
            panel.BackgroundColor = prevBG;
        }

        public static void DrawTinted(this UIImageButton button, SpriteBatch batch, float tint)
        {
            button.GetVisibility(out float visActive, out float visInactive);
            button.SetVisibility(visActive * tint, visInactive * tint);
            button.Draw(batch);
            button.SetVisibility(visActive, visInactive);
        }
    }

    public class UITextTogglable : UIElementInteractable
    {
        public string Text => _text.Text;

        public bool IsWrapped
        {
            get => _text.IsWrapped;
            set => _text.IsWrapped = value;
        }

        public float TextOriginX
        {
            get => _text.TextOriginX;
            set => _text.TextOriginX = value;
        }
        public float TextOriginY
        {
            get => _text.TextOriginY;
            set => _text.TextOriginY = value;
        }

        private UIText _text;

        public UITextTogglable(string text, float scale = 1.0f, bool large = false)
        {
            _text = new UIText(text, scale, large);
            _text.Width.Set(0, 1);
            _text.Height.Set(0, 1);
            Append(_text);
        }

        public void SetText(string text) => _text.SetText(text);

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            _text.DrawTinted(spriteBatch, CurrentTint);
            for (int i = 1; i < Elements.Count; i++)
            {
                Elements[i].Draw(spriteBatch);
            }
        }
    }

    public class UIElementInteractable : UIElement
    {
        public bool IsEnabled
        {
            get
            {
                UIElement current = this;
                while (current != null)
                {
                    if (current is UIElementInteractable uiI && !uiI._enabled) { return false; }
                    current = current.Parent;
                }
                return true;
            }
            set
            {
                _enabled = value;
                this.IgnoresMouseInteraction = !_enabled || !_interactable;
            }
        }

        public bool Interactable
        {
            get
            {
                UIElement current = this;
                while (current != null)
                {
                    if (current.IgnoresMouseInteraction || 
                        (current is UIElementInteractable uiI && (!uiI._interactable || !uiI._enabled))) { return false; }
                    current = current.Parent;
                }
                return true;
            }
            set
            {
                _interactable = value;
                this.IgnoresMouseInteraction = !_enabled || !_interactable;
            }
        }

        public float CurrentTint
        {
            get => Interactable ? 1.0f : InteractTint;
        }
        public float InteractTint
        {
            get;
            set;
        } = 0.75f;

        private bool _interactable = true;
        private bool _enabled = true;

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) { return; }
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsEnabled) { return; }
            base.Draw(spriteBatch);
        }
    }

    public interface IControl<T>
    {
        T Value { get; set; }
        string ValueStr { get; }
        TypeHelpers.StringFormat<T> Formatter { set; }

        bool SetValue(T value, bool markDirty = true, bool suppressRefersh = false, bool suppressEvent = false);
        bool GetValue(ref T value);
    }

    public abstract class UIControl<T> : UIElementInteractable, IControl<T>
    {
        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        public bool IsDirty => _isDirty;
        public virtual string ValueStr => _formatter.Invoke(ref _value);

        public Action<T> OnValueChange;

        public TypeHelpers.StringFormat<T> Formatter
        {
            set => _formatter = value ?? FromatValue;
        }

        protected T _value;
        protected bool _isDirty;
        protected bool _interactable;
        protected TypeHelpers.StringFormat<T> _formatter;

        public UIControl() { }
        public UIControl(T value, TypeHelpers.StringFormat<T> formatter = null)
        {
            _value = value;
            _formatter = formatter ?? FromatValue;
        }

        public virtual bool SetValue(T value, bool markDirty = true, bool suppressRefersh = false, bool suppressEvent = false)
        {
            LimitValue(ref value);
            bool willChange = !EqualityComparer<T>.Default.Equals(_value, value);
            _isDirty = markDirty && (_isDirty | willChange);
            _value = value;

            if (willChange && !suppressEvent)
            {
                OnValueChange?.Invoke(_value);
            }
            if (willChange && !suppressRefersh)
            {
                RefreshUI();
            }
            return willChange;
        }
        public virtual bool GetValue(ref T value)
        {
            if (!_isDirty) { return false; }
            value = _value;
            _isDirty = false;
            return true;
        }

        public virtual void RefreshUI() { }
        protected virtual void LimitValue(ref T value) { }

        protected virtual string FromatValue(ref T value)
        {
            return value.ToString();
        }
    }

    public class UISliderElement<T> : UIControl<T> where T: unmanaged, IComparable<T>
    {
        public T min;
        public T max;
        public T step;

        public bool HideInputField
        {
            get => _hideInputField;
            set
            {
                _textBoxBackground.IgnoresMouseInteraction = value;
                _inputField.IgnoresMouseInteraction = value;
                _hideInputField = value;
            }
        }
        public bool AllowWrapping
        {
            get => _allowWrapping;
            set => _allowWrapping = value;
        }

        public Vector2 ButtonSize 
        {
            get => _buttonSize;
            set
            {
                _buttonSize.X = value.X <= 0 ? _buttonDec.DefaultWidth : value.X;
                _buttonSize.Y = value.Y <= 0 ? _buttonDec.DefaultHeight : value.Y;

                _buttonDec.Width.Set(_buttonSize.X, 0);
                _buttonDec.Height.Set(_buttonSize.Y, 0);

                _buttonInc.Width.Set(_buttonSize.X, 0);
                _buttonInc.Height.Set(_buttonSize.Y, 0);
            }
        }

        public bool IsHovered
        {
            get => (!_hideInputField && _inputField.IsMouseHovering) || (!_hideButtons && (_buttonDec.IsMouseHovering | _buttonInc.IsMouseHovering));
        }
        public bool HideButtons
        {
            get => _hideButtons;
            set
            {

                _hideButtons = value;
                _buttonInc.IgnoresMouseInteraction = value;
                _buttonDec.IgnoresMouseInteraction = value;
            }
        }

        private Vector2 _buttonSize;

        private bool _allowWrapping;
        private bool _hideInputField;
        private bool _hideButtons;
        protected UIInputField _inputField;
        private UIPanel _textBoxBackground;
        private UITexturedButton _buttonInc;
        private UITexturedButton _buttonDec;

        public UISliderElement(T min, T max, T step) : this(default, min, max, step, null)
        {

        }
        public UISliderElement(T value, T min, T max, T step, TypeHelpers.StringFormat<T> formatter) : base(value, formatter)
        {
            this.min = min;
            this.max = max;
            this.step = step;
            LimitValue(ref _value);
            _textBoxBackground = new UIPanel();
            _inputField = new UIInputField("Type here");
            _buttonInc = new UITexturedButton(ModContent.Request<Texture2D>("Projections/Content/UI/Button_Right"));
            _buttonDec = new UITexturedButton(ModContent.Request<Texture2D>("Projections/Content/UI/Button_Left"));

            Append(_textBoxBackground);
            Append(_inputField);
            Append(_buttonInc);
            Append(_buttonDec);

            ButtonSize = Vector2.Zero;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            Recalculate();

            _textBoxBackground.HAlign = 0.5f;
            _textBoxBackground.Width.Set(0, 0.70f);
            _textBoxBackground.Height.Set(0, 1);

            _inputField.SetText(ValueStr);
            _inputField.OnTextChange += (a, b) =>
            {
                OnTextUpdated(_inputField.CurrentString);
            };

            _inputField.OnUnfocus += (a, b) => _inputField.SetText(ValueStr);
            _inputField.HAlign = 0.5f;
            _inputField.VAlign = 0.5f;
            _inputField.Top.Set(0, 0.125f);
            _inputField.Width.Set(0, 0.625f);
            _inputField.Height.Set(0, 1f);

            _buttonInc.HAlign = 1.0f;
            _buttonInc.OnLeftClick += (_, _) =>
            {
                DoIncrement(step);
            };
            _buttonDec.HAlign = 0.0f;
            _buttonDec.OnLeftClick += (_, _) =>
            {
                DoDecrement(step);
            };
            Recalculate();
        }

        public override void RefreshUI()
            => _inputField.SetText(ValueStr, false);

        protected override void LimitValue(ref T value)
        {
            bool minValid = value.CompareTo(min) >= 0;
            bool maxValid = value.CompareTo(max) <= 0;

            // Check if both are valid or if both are invalid
            // In both cases we just don't do anything.
            if ((minValid & maxValid) |
                !(minValid | maxValid))
            {
                return;
            }

            // Because of the if statement above, when maxValid is true, then that can only mean that value is 
            // less than the minimum value, and when maxValid is false then value is greater than maximum.
            // Hence we can just do a XOR with _allowWrapping and maxValid to get the correct limited value.
            // We're not using modulo with wrapping, just checking if the value is out of range and setting
            // the value to the minimum or maximum accordingly.
            // ---------------------------------------------------------------------------------------
            // Visualizations for XOR values with format [_allowWrapping, maxValid => min/max]
            // 0,0 => 0 => max
            // 0,1 => 1 => min
            // 1,0 => 1 => min
            // 1,1 => 0 => max
            value = (_allowWrapping ^ maxValid) ? min : max;
        }

        protected virtual void OnTextUpdated(string text)
        {
            if (text.TryParse(out T valueOut))
            {
                SetValue(valueOut, true, true);
            }
        }

        protected virtual void DoDecrement(T step)
        {
            SetValue(GenericOps<T>.Sub(_value, step), true, true);
            _inputField.SetText(ValueStr, false);
        }

        protected virtual void DoIncrement(T step)
        {
            SetValue(GenericOps<T>.Add(_value, step), true, true);
            _inputField.SetText(ValueStr, false);
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            float tint = CurrentTint;
            if (!_hideInputField)
            {
                _textBoxBackground.BorderColor = !_inputField.IsReadonly && base.IsMouseHovering ? Color.Yellow : Color.Black;
                _textBoxBackground.DrawTinted(spriteBatch, tint);
                _inputField.Draw(spriteBatch);
            }

            if (!_hideButtons)
            {
                _buttonInc.Draw(spriteBatch);
                _buttonDec.Draw(spriteBatch);
            }

            for (int i = 4; i < Elements.Count; i++)
            {
                Elements[i].Draw(spriteBatch);
            }
        }
    }

    public class UIEnum<T> : UISliderElement<int> where T : Enum
    {
        private static T[] _enums;
        private static string[] _enumsNames;

        public T Current
        {
            get => _enums[_value];
            set
            {
                SetValue(value);
            }
        }

        static UIEnum()
        {
            _enums = Enum.GetValues(typeof(T)) as T[];
            _enumsNames = Enum.GetNames(typeof(T));
        }

        public UIEnum() : this(default(T)) { }
        public UIEnum(T enumV, int? maxValue = null) : base(0, maxValue != null ? maxValue.Value - 2 : _enums.Length - 1, 1)
        {
            AllowWrapping = true;
            SetValue(enumV, false);
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            _inputField.IsReadonly = true;
            _inputField.CenterText = true;
        }

        public int IndexOf(T value)
        {
            for (int i = 0; i < _enums.Length; i++)
            {
                if (_enums[i].Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool SetValue(T value, bool markDirty = true, bool suppressRefersh = false)
        {
            for (int i = 0; i < _enums.Length; i++)
            {
                if (_enums[i].Equals(value))
                {
                    return SetValue(i, markDirty, suppressRefersh);
                }
            }
            return false;
        }

        public bool GetValue(ref T value)
        {
            int ind = 0;
            if (GetValue(ref ind))
            {
                value = Current;
                return true;
            }
            return false;
        }

        protected override void OnTextUpdated(string text) { }
        protected override string FromatValue(ref int value)
        {
            return value >= _enumsNames.Length || value < 0 ? "" : _enumsNames[value];
        }
    }

    public class UIInputField : UIElementInteractable
    {
        internal bool Focused = false;
        internal string CurrentString
        {
            get => _currentStr;
            set => _currentStr = value;
        }

        public bool CenterText
        {
            get => _centerText;
            set => _centerText = value;
        }

        public float TextScale
        {
            get => _textScale;
            set => _textScale = value;
        }

        private readonly string _hintText;
        private int _textBlinkerCount;
        private int _textBlinkerState;

        public delegate void EventHandler(object sender, EventArgs e);

        public event EventHandler OnTextChange;
        public event EventHandler OnUnfocus;
        private string _currentStr;
        private string _prevStr;
        private bool _centerText;
        private bool _readonly;
        private float _textScale = 1.0f;

        public bool IsReadonly { get => _readonly; set => _readonly = value; }

        public UIInputField(string hintText)
        {
            _readonly = false;
            _hintText = hintText;
        }

        public void SetText(string text, bool notify = true)
        {
            text ??= "";
            if (CurrentString != text)
            {
                CurrentString = text;
                if (notify)
                {
                    OnTextChange?.Invoke(this, new EventArgs());
                }
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (!_readonly && !Focused)
            {
                Main.clrInput();
                Focused = true;
                _prevStr = CurrentString;
            }
            base.LeftClick(evt);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (!_readonly && !Focused)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            if (Focused)
            {
                Focused = false;
                OnUnfocus?.Invoke(this, new EventArgs());
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsEnabled) { return; }

            Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
            if (!ContainsPoint(MousePosition) && Main.mouseLeft)
            {
                Focused = false;
                OnUnfocus?.Invoke(this, new EventArgs());
            }
            else if (Focused)
            {
                Main.CurrentInputTextTakerOverride = this;
            }
            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float tint = CurrentTint;
            if (Focused)
            {
                Main.CurrentInputTextTakerOverride = this;
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newString = Main.GetInputText(CurrentString);
                if (!newString.Equals(CurrentString))
                {
                    CurrentString = newString;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    OnTextChange?.Invoke(this, new EventArgs());
                }

                if (Main.inputTextEnter)
                {
                    Focused = false;
                    OnUnfocus?.Invoke(this, new EventArgs());
                }
                else if (Main.inputTextEscape)
                {
                    CurrentString = _prevStr;
                    Focused = false;
                    OnUnfocus?.Invoke(this, new EventArgs());
                    OnTextChange?.Invoke(this, new EventArgs());
                }
                if (++_textBlinkerCount >= 20)
                {
                    _textBlinkerState = (_textBlinkerState + 1) % 2;
                    _textBlinkerCount = 0;
                }
            }
            string displayString = CurrentString;
            if (_textBlinkerState == 1 && Focused)
            {
                displayString += "|";
            }
            CalculatedStyle space = GetDimensions();
            CalculatedStyle inner = GetInnerDimensions();
            var pos = new Vector2(inner.X, space.Y);

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 stringSize = ChatManager.GetStringSize(font, CurrentString, new Vector2(1f));
            var textSize = new Vector2(stringSize.X, 16f) * _textScale;

            pos.X += (inner.Width - textSize.X) * (_centerText ? 0.5f : 0.0f);
            if (CurrentString.Length <= 0 && !Focused)
            {
                Utils.DrawBorderString(spriteBatch, _hintText, pos, Color.Gray * tint, _textScale, 0.0f, 0);
            }
            else
            {
                Utils.DrawBorderString(spriteBatch, displayString, pos, Color.White * tint, _textScale, 0.0f, 0);
            }
        }
    }

    public class UIColor : UIControl<Color32>
    {
        private UIImage _image;
        private UISliderElement<short> _r;
        private UISliderElement<short> _g;
        private UISliderElement<short> _b;
        private UISliderElement<short> _a;

        public UIColor(Color32 value) : base(value)
        {
            _image = new UIImage(TextureAssets.MagicPixel)
            {
                ScaleToFit = true,
                AllowResizingDimensions = false
            };
            const float COLOR_S = 0.1f;
            const float SIZE = (1.0f - COLOR_S * 2) / 4.0f;

            _image.Width.Set(0, 0.2f);
            _image.Height.Set(0, 1.0f);

            Append(_image);
            _r = new UISliderElement<short>(0, 255, 1)
            {
                HideButtons = true,
                AllowWrapping = false,            
            };
            _r.OnValueChange += (short v) =>
            {
                SetValue(new Color32((byte)v, _value.g, _value.b, _value.a), true, false, true);
            };
            _r.Width.Set(0, SIZE);
            _r.Left.Set(0, COLOR_S * 2);
            _r.Height.Set(0, 1.0f);
            Append(_r);

            _g = new UISliderElement<short>(0, 255, 1)
            {
                HideButtons = true,
                AllowWrapping = false,
            };
            _g.OnValueChange += (short v) =>
            {
                SetValue(new Color32(_value.r, (byte)v, _value.b, _value.a), true, false, true);
            };
            _g.Width.Set(0, SIZE);
            _g.Left.Set(0, COLOR_S * 2 + SIZE);
            _g.Height.Set(0, 1.0f);
            Append(_g);

            _b = new UISliderElement<short>(0, 255, 1)
            {
                HideButtons = true,
                AllowWrapping = false,
            };
            _b.OnValueChange += (short v) =>
            {
                SetValue(new Color32(_value.r, _value.g, (byte)v, _value.a), true, false, true);
            };
            _b.Width.Set(0, SIZE);
            _b.Left.Set(0, COLOR_S * 2 + SIZE * 2);
            _b.Height.Set(0, 1.0f);
            Append(_b);

            _a = new UISliderElement<short>(0, 255, 1)
            {
                HideButtons = true,
                AllowWrapping = false,
            };
            _a.OnValueChange += (short v) =>
            {
                SetValue(new Color32(_value.r, _value.b, _value.b, (byte)v), true, false, true);
            };
            _a.Width.Set(0, SIZE);
            _a.Left.Set(0, COLOR_S * 2 + SIZE * 3);
            _a.Height.Set(0, 1.0f);
            Append(_a);
        }

        public override bool SetValue(Color32 value, bool markDirty = true, bool suppressRefersh = false, bool suppressEvent = false)
        {
            bool retVal = base.SetValue(value, markDirty, suppressRefersh, suppressEvent);
            _r.SetValue(value.r, true, false, true);
            _g.SetValue(value.g, true, false, true);
            _b.SetValue(value.b, true, false, true);
            _a.SetValue(value.a, true, false, true);
            return retVal;
        }

        public override void RefreshUI()
        {
            base.RefreshUI();
            _image.Color = new Color(
                PMath.MultUI8LUT(_value.r, _value.a),
                PMath.MultUI8LUT(_value.g, _value.a),
                PMath.MultUI8LUT(_value.b, _value.a),
                _value.a);
        }
    }

    public class UIBool : UIButton, IControl<bool>
    {
        public bool Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _isDirty |= true;
                    _value = value;
                    RefreshUI();
                }
            }
        }
        public string ValueStr => string.IsNullOrWhiteSpace(overrideText) ? (_value ? _onTxt : _offTxt) : overrideText;

        public string overrideText;

        TypeHelpers.StringFormat<bool> IControl<bool>.Formatter { set => throw new NotImplementedException(); }

        private bool _isDirty;
        private bool _value;
        private string _offTxt;
        private string _onTxt;

        public UIBool(string offText = "Off", string onText = "On") : base(offText)
        {
            Width.Set(0, 1.0f);
            Height.Set(0, 1.0f);
            _offTxt = offText;
            _onTxt = onText;
            _value = false;
            RefreshUI();
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            Value = !Value;
            base.LeftClick(evt);
        }

        public void RefreshUI()
        {
            this.SetText(ValueStr);
        }

        public bool SetValue(bool value, bool markDirty = true, bool suppressRefersh = false, bool suppressEvent = false)
        {
            bool willChange = value != _value;
            _isDirty = markDirty && (_isDirty | willChange);
            _value = value;
            if (willChange && !suppressRefersh)
            {
                RefreshUI();
            }
            return willChange;
        }

        public bool GetValue(ref bool value)
        {
            if (_isDirty)
            {
                value = Value;
                _isDirty = false;
                return true;
            }
            return false;
        }
    }

    public class UIStringSelect : UISliderElement<int>
    {
        public string Current
        {
            get => this.FromatValue(ref _value);
            set => SetValue(value);
        }

        private int _count;
        private string[] _buffer;

        public UIStringSelect(bool allowWrap, int capacity, float textScale = 1.0f) : base(0, capacity - 1, 1)
        {
            _buffer = new string[capacity];
            this.AllowWrapping = allowWrap;
            _inputField.IsReadonly = true;
            _inputField.CenterText = true;
            _inputField.TextScale = textScale;
        }

        public void Setup(string[] strings, int count)
        {
            _count = Math.Min(Math.Min(count, _buffer.Length), _buffer.Length);
            for (int i = 0; i < _count; i++)
            {
                _buffer[i] = strings[i];
            }
            max = _count - 1;
            ValidateIndex();
            RefreshUI();
        }

        public void ValidateIndex()
        {
            int prev = _value;
            while (_value >= _count && _value > 0)
            {
                _value--;
            }
            if (prev != _value)
            {
                RefreshUI();
            }
        }

        public bool SetValue(string value, bool markDirty = true, bool suppressRefersh = false)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_buffer[i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return SetValue(i, markDirty, suppressRefersh);
                }
            }
            return false;
        }

        public bool GetValue(ref string value)
        {
            int ind = 0;
            if (GetValue(ref ind))
            {
                value = Current;
                return true;
            }
            return false;
        }

        protected override void OnTextUpdated(string text) { }
        protected override string FromatValue(ref int value)
        {
            return value >= _count || value < 0 ? "" : _buffer[value];
        }
    }

    public class UIToggle : UIControl<bool>
    {
        public Action<bool> OnValueChanged;
        private Asset<Texture2D> _texture;

        private Point _size;
        private Point _offsetOn;
        private Point _offsetOff;

        public UIToggle(bool initVal, float? scale = null) :
            this(initVal, null, new Point(14, 14), new Point(0, 0), new Point(16, 0), scale)
        { }

        public UIToggle(bool initVal, Asset<Texture2D> texture, Point size, Point offsetOn, Point offsetOff, float? scale = null) :
            base(initVal)
        {
            _texture = texture ?? Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
            _size = size;
            _offsetOn = offsetOn;
            _offsetOff = offsetOff;

            if (scale != null)
            {
                this.Width.Set(scale.Value, 0);
                this.Height.Set(scale.Value, 0);
            }
            else
            {
                this.Width.Set(_size.X, 0);
                this.Height.Set(_size.Y, 0);
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            SetValue(!_value, true, true);
            base.LeftClick(evt);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Texture2D value;
            Point point;
            if (_value)
            {
                value = _texture.Value;
                point = _offsetOn;
            }
            else
            {
                value = _texture.Value;
                point = _offsetOff;
            }

            Color color = (base.IsMouseHovering ? Color.White : Color.Silver) * CurrentTint;
            spriteBatch.Draw(value, new Rectangle((int)dimensions.X, (int)dimensions.Y, _size.X, _size.Y), new Rectangle(point.X, point.Y, _size.X, _size.Y), color);
        }
    }

    public class UIButton : UIElementInteractable
    {
        public bool Selected { get; set; } = false;

        private UIPanel _bg;
        private UIText _text;

        public UIButton(string title)
        {
            _bg = new UIPanel();
            _bg.Width.Set(0, 1);
            _bg.Height.Set(0, 1);
            Append(_bg);

            _text = new UIText(title);
            _text.Width.Set(0, 1);
            _text.Height.Set(0, 1);
            _text.HAlign = 0.5f;
            _text.VAlign = 0.5f;
            _text.TextOriginX = 0.5f;
            _text.TextOriginY = 0.5f;
            _text.IgnoresMouseInteraction = true;
            Append(_text);
        }

        public void SetText(string text)
        {
            _text.SetText(text);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            float tint = CurrentTint;
            _bg.BorderColor = base.IsMouseHovering ? Color.Yellow : Selected ? Color.Silver : Color.Black;

            _bg.DrawTinted(spriteBatch, tint);
            _text.DrawTinted(spriteBatch, tint);
            for (int i = 2; i < Elements.Count; i++)
            {
                Elements[i].Draw(spriteBatch);
            }
        }

        public string GetText() => _text.Text;
    }

    public class UITexturedButton : UIElementInteractable
    {
        public float DefaultWidth => _texture?.Width() ?? 18;
        public float DefaultHeight => _texture?.Height() ?? 18;

        private Asset<Texture2D> _texture;

        private float _visibilityActive = 1f;
        private float _visibilityInactive = 0.5f;

        private Asset<Texture2D> _hoverTexture;

        public UITexturedButton(Asset<Texture2D> texture, Asset<Texture2D> hoverImage = null)
        {
            _texture = texture;
            _hoverTexture = hoverImage;
            Width.Set(_texture.Width(), 0f);
            Height.Set(_texture.Height(), 0f);
        }

        public void SetHoverImage(Asset<Texture2D> texture)
        {
            _hoverTexture = texture;
        }

        public void SetImage(Asset<Texture2D> texture)
        {
            _texture = texture;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float tint = CurrentTint;
            CalculatedStyle dimensions = GetDimensions();
            var destRect = new Rectangle((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height);
            spriteBatch.Draw(_texture.Value, destRect, Color.White * (base.IsMouseHovering ? _visibilityActive : _visibilityInactive) * tint);
            if (_hoverTexture != null && base.IsMouseHovering)
            {
                spriteBatch.Draw(_hoverTexture.Value, destRect, Color.White * tint);
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public void SetVisibility(float whenActive, float whenInactive)
        {
            _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
            _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
        }
    }

    public class NamedUIElement<T> : UIElementInteractable where T : UIElement
    {
        public T Element => _element;

        private UIText _name;
        private T _element;
        private float _ratio;
        private bool _fillElement;

        public NamedUIElement(string name, T element, float ratio = 0.5f, float textScale = 1.0f, bool fillElementSide = true)
        {
            _name = new UIText(name, textScale);
            _element = element;
            _ratio = ratio;
            _fillElement = fillElementSide;
            Append(_name);
        }

        public override void OnInitialize()
        {
            float ratInv = 1.0f - _ratio;

            base.OnInitialize();
            _name.Width.Set(0, _ratio);
            _name.Height.Set(0, 1.0f);
            _name.HAlign = 0.0f;
            _name.VAlign = 0.0f;
            _name.IgnoresMouseInteraction = true;
            _name.TextOriginX = 0.0f;
            _name.TextOriginY = 0.5f;

            UIElement elementToAppend = _element;
            if (_fillElement)
            {
                _element.Width.Set(0, ratInv);
                _element.Height.Set(0, 1.0f);
                _element.HAlign = 1.0f;
                _element.VAlign = 0.0f;
            }
            else
            {
                elementToAppend = new UIElement();
                elementToAppend.Width.Set(0, ratInv);
                elementToAppend.Height.Set(0, 1.0f);
                elementToAppend.HAlign = 1.0f;
                elementToAppend.VAlign = 0.0f;
                elementToAppend.Append(_element);
                _element.HAlign = 0;
            }
            Append(elementToAppend);
            Recalculate();
        }
        public void SetTitle(string title) => _name.SetText(title);

        protected override void DrawChildren(SpriteBatch batch)
        {
            _name.DrawTinted(batch, CurrentTint);
            for (int i = 1; i < Elements.Count; i++)
            {
                Elements[i].Draw(batch);
            }
        }
    }
}
