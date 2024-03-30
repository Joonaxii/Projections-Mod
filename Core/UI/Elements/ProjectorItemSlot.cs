using Terraria.UI;
using Terraria;
using Projections.Content.Items;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.UI.Gamepad;
using Terraria.UI.Chat;
using Terraria.GameInput;
using Terraria.GameContent;
using static Terraria.UI.ItemSlot;
using Projections.Core.Data.Structures;
using Projections.Common.ProjectorTypes;

namespace Projections.Core.UI.Elements
{
    public class ProjectorItemSlot : CustomItemSlot
    {
        private Projector _projector;
        private bool _overrideDraw;
        public ProjectorItemSlot(Projector projector, int context = ItemSlot.Context.BankItem, float scale = 1f, int visualContext = -1, bool overrideDraw = false) : base(context, scale, visualContext)
        {
            _projector = projector;
            RefreshFromProjector();
            _overrideDraw = overrideDraw;
        }

        protected override bool CheckHeldItemSelf(Item item)
        {
            return (item == null || item.IsAir) || ((item.ModItem is ProjectionItem proj) && !proj.IsEmpty);
        }

        public void SetProjector(Projector projector)
        {
            _projector = projector;
            RefreshFromProjector();
        }

        public override void OnActivate()
        {
            base.OnActivate();
            RefreshFromProjector();
        }

        protected override void DrawSlot(SpriteBatch spriteBatch, Item item, Vector2 position)
        {
            if (!_overrideDraw)
            {
                base.DrawSlot(spriteBatch, item, position);
                return; 
            }

            Player player = Main.player[Main.myPlayer];
            float inventoryScale = Main.inventoryScale;
            Color color = Color.White;

            bool isSelected = false;
            int drawMode = 0;
            int gamepadPointForSlot = 0;
            if (PlayerInput.UsingGamepadUI)
            {
                isSelected = UILinkPointNavigator.CurrentPoint == gamepadPointForSlot;
                if (PlayerInput.SettingsForUI.PreventHighlightsForGamepad)
                {
                    isSelected = false;
                }

                drawMode = player.DpadRadial.GetDrawMode(0);
                if (drawMode > 0 && !PlayerInput.CurrentProfile.UsingDpadHotbar())
                {
                    drawMode = 0;
                }
            }

            Texture2D bgTexture = TextureAssets.InventoryBack.Value;
            Color bgColor = Main.inventoryBack;
            bool highlightThingsForMouse = PlayerInput.SettingsForUI.HighlightThingsForMouse;
            if (item.type > ItemID.None && item.stack > 0 && item.favorited)
            {
                bgTexture = TextureAssets.InventoryBack10.Value;
            }
            else if (item.type > ItemID.None && item.stack > 0 && Options.HighlightNewItems && item.newAndShiny)
            {
                bgTexture = TextureAssets.InventoryBack15.Value;
                float mouseColorMod = (float)Main.mouseTextColor / 255f;
                mouseColorMod = mouseColorMod * 0.2f + 0.8f;
                bgColor = bgColor.MultiplyRGBA(new Color(mouseColorMod, mouseColorMod, mouseColorMod));
            }
            else if (!highlightThingsForMouse && item.type > ItemID.None && item.stack > 0 && drawMode != 0)
            {
                bgTexture = TextureAssets.InventoryBack15.Value;
                float mouseColorMod = (float)Main.mouseTextColor / 255f;
                mouseColorMod = mouseColorMod * 0.2f + 0.8f;
                bgColor = ((drawMode != 1) ? bgColor.MultiplyRGBA(new Color(mouseColorMod / 2f, mouseColorMod, mouseColorMod / 2f)) : bgColor.MultiplyRGBA(new Color(mouseColorMod, mouseColorMod / 2f, mouseColorMod / 2f)));
            }

            if (isSelected)
            {
                bgTexture = TextureAssets.InventoryBack14.Value;
                bgColor = Color.White;
                if (item.favorited)
                {
                    bgTexture = TextureAssets.InventoryBack17.Value;
                }
            }
            spriteBatch.Draw(bgTexture, position, null, bgColor, 0f, default(Vector2), inventoryScale, SpriteEffects.None, 0f);

            Vector2 vector = bgTexture.Size() * inventoryScale;
            if (item.type > ItemID.None && item.stack > 0)
            {
                DrawItemIcon(item, 0, spriteBatch, position + vector / 2f, inventoryScale * 1.4f, 32f, color);
                if (item.stack > 1)
                {
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, item.stack.ToString(), position + new Vector2(10f, 26f) * inventoryScale * 1.6f, color, 0f, Vector2.Zero, new Vector2(inventoryScale * 0.6f), -1f, inventoryScale);
                }
            }
            if (gamepadPointForSlot != -1)
            {
                UILinkPointNavigator.SetPosition(gamepadPointForSlot, position + vector * 0.75f);
            }
        }

        protected override void OnItemUpdateSelf()
        {
            if(_projector != null)
            {
                ref var slot = ref _projector.ActiveSlot;
                var pItem = Item.ModItem as ProjectionItem;
                if (pItem == null || IsEmpty())
                {
                    if (!slot.IsEmpty) 
                    {
                        slot.Setup(ProjectionIndex.Zero, 0);
                        slot.Reset();
                    }
                    return;
                }
                if(slot.Setup(pItem.Index, Item.stack))
                {
                    slot.Reset();
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshFromProjector();
        }

        public void RefreshFromProjector()
        {
            if(_projector != null)
            {
                ref var slot = ref _projector.ActiveSlot;
                if (slot.IsEmpty) 
                {
                    MakeAir();
                    return;
                }

                if(Item.ModItem is ProjectionItem proj)
                {
                    if(proj.Index != slot.Index)
                    {
                        proj.SetIndex(slot.Index);
                    }
                }
                else
                {
                    Item.SetDefaults(ModContent.ItemType<ProjectionItem>());
                    (Item.ModItem as ProjectionItem).SetIndex(slot.Index);
                }
                Item.stack = slot.Stack;
                return;
            }
            MakeAir();
        }
    }
}
