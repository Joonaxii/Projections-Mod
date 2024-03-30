using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace Projections.Core.UI.Elements
{
    public class CustomItemSlot : UIElementInteractable
    {
        public bool IsLocked { get; set; }

        public Item Item
        {
            get => _item;
            set => _item = value;
        }
        private Item _item;
        protected int _context;
        protected int _contextVisual;
        protected float _scale;
        public Func<Item, bool> CheckHeldItem;
        public Action<Item> OnItemUpdate;

        public CustomItemSlot(int context = ItemSlot.Context.BankItem, float scale = 1f, int visualContext = -1)
        {
            _context = context;
            _scale = scale;
            _contextVisual = visualContext < 0 ? context : visualContext;
            Item = new Item();
            Item.SetDefaults(0);

            var tex = Terraria.GameContent.TextureAssets.InventoryBack9.Value;
            Width.Set(tex.Width * scale, 0f);
            Height.Set(tex.Height * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rectangle = GetDimensions().ToRectangle();
            if (!IsLocked && Interactable)
            {
                bool clicked = (Main.mouseLeftRelease & Main.mouseLeft) | (Main.mouseRight);
                if (!PlayerInput.IgnoreMouseInterface && ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    if ((CheckHeldItemSelf(Main.mouseItem) && (CheckHeldItem == null || CheckHeldItem(Main.mouseItem))))
                    {
                        if (clicked && Main.keyState.PressingShift() && OverrideShift())
                        {
                            return;
                        }

                        if((Main.mouseRightRelease & Main.mouseRight) && OverrideRightClick())
                        {
                            return;
                        }

                        _item ??= new Item();
                        ItemSlot.Handle(ref _item, _context);
                        if (clicked)
                        {
                            OnItemUpdateSelf();
                            OnItemUpdate?.Invoke(Item);
                        }
                    }
                }
            }

            float oldScale = Main.inventoryScale;
            Main.inventoryScale = _scale;
            DrawSlot(spriteBatch, Item, rectangle.TopLeft());
            Main.inventoryScale = oldScale;
        }

        protected virtual bool OverrideShift() => false;
        protected virtual bool OverrideRightClick() => false;

        protected virtual bool CheckHeldItemSelf(Item item) => true;
        protected virtual void DrawSlot(SpriteBatch batch, Item item, Vector2 position)
        {
            _item ??= new Item();
            ItemSlot.Draw(batch, ref _item, _contextVisual, position, Color.White * CurrentTint);
        }

        protected virtual void OnItemUpdateSelf() { }

        internal bool IsEmpty() => Item == null || Item.stack <= 0 || Item.IsAir;

        internal void MakeAir()
        {
            if (IsEmpty()) { return; }
            Item.stack = 0;
            Item.TurnToAir();
        }
    }
}
