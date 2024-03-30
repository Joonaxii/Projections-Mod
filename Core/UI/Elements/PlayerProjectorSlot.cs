using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.Netcode;
using Projections.Common.Players;
using Projections.Common.ProjectorTypes;
using Projections.Content.Items;
using Projections.Content.Items.Projectors;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Projections.Core.UI.Elements
{
    public class PlayerProjectorSlot : CustomItemSlot
    {
        public bool IsVisible
        {
            get => _isVisible;
        }
        private bool _isVisible;
        private int _slotIndex;
        private ProjectionsPlayer _pPlayer;
        private Asset<Texture2D> _gearIcon;

        public PlayerProjectorSlot(int slotIndex, float scale = 1.0f) : 
            base(ItemSlot.Context.BankItem, scale, ItemSlot.Context.EquipAccessoryVanity) 
        {
            _slotIndex = slotIndex;
        }

        public void Setup(ProjectionsPlayer pPlayer)
        {
            _gearIcon ??= Main.Assets.Request<Texture2D>("Images/UI/Creative/Research_GearA");
            _pPlayer = pPlayer;
            _isVisible = _pPlayer.IsProjectorVisible(_slotIndex);

            Projector proj = null;
            bool foundO = _pPlayer?.TryGetProjector(_slotIndex, out proj) ?? false;
            if ((Item?.IsAir ?? true))
            {
                Item = foundO ? ProjectionUtils.NewPlayerProjector(proj)?.Item : null;
            }
            else if (Item?.ModItem is PlayerProjectorItem projI && projI.Projector != proj)
            {
                projI.Setup(proj);
            }
        }

        protected override bool CheckHeldItemSelf(Item item)
        {
            return (item == null || item.IsAir) || ((item.ModItem is PlayerProjectorItem));
        }

        protected override void OnItemUpdateSelf()
        {
            if(_pPlayer == null) { return; }

            if (Item?.IsAir ?? true)
            {
                if(_pPlayer.TryGetProjector(_slotIndex, out var proj) && _pPlayer.TryFindProjectorItem(_slotIndex, out var item, out int iSlot))
                {
                    item.Pack(proj, _pPlayer.Player.whoAmI, _slotIndex);
                    if (proj != null && UISystem.Instance.CurrentProjector == proj)
                    {
                        UISystem.CloseProjectorUI(true);
                    }

                    _pPlayer.SetProjector(null, _slotIndex);
            
                    if(Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ProjectionNetUtils.SendKillProjector(proj, false, Main.myPlayer);
                        if(iSlot > -1)
                        {
                            NetMessage.SendData(MessageID.SyncEquipment, -1, Main.myPlayer, number: Main.myPlayer, number2: iSlot);
                        }
                    }
                }
            }
            else
            {
                PlayerProjectorItem pProj = Item?.ModItem as PlayerProjectorItem;
                pProj ??= ProjectionUtils.NewModItem<PlayerProjectorItem>(1);
                if (pProj.Projector == null)
                {
                    _pPlayer.TryGetProjector(_slotIndex, out var proj);
                    pProj.Unpack(_pPlayer.Player.whoAmI, _slotIndex, ref proj);
                    _pPlayer.SetProjector(proj, _slotIndex);

                    if (Main.playerInventory)
                    {
                        Main.EquipPage = Main.EquipPageSelected = 2;
                    }
                }
            }
        }

        protected override bool OverrideRightClick()
        {
            if(_pPlayer == null || Item == null || Item.IsAir) { return false; }
            if(Item.ModItem is PlayerProjectorItem proj)
            {
                if (Main.keyState.PressingShift())
                {
                    proj.Unpack(_pPlayer.Player);
                    return true;
                }
                else if (_pPlayer.MoveToEmptySlotInventory(Item))
                {
                    if (proj.Projector != null && UISystem.Instance.CurrentProjector == proj.Projector)
                    {
                        UISystem.CloseProjectorUI(true);
                    }
                    Item = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                    return true;
                }
            }
            return false;
        }

        private static bool DrawButton(SpriteBatch batch, Texture2D texture, Rectangle rectangle, bool isEnabled, string hoverText, out bool isHovering, Texture2D hoverImg = null)
        {
            isHovering = isEnabled && rectangle.Contains(new Point(Main.mouseX, Main.mouseY)) && !PlayerInput.IgnoreMouseInterface;
            batch.Draw(isHovering ? hoverImg ?? texture : texture, rectangle, Color.White * (isEnabled ? 1.0f : 0.65f));
            bool clicked = false;
            if (isHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick, null);
                    clicked = true;
                }
            }
            if (isHovering && !string.IsNullOrWhiteSpace(hoverText))
            {
                Main.HoverItem ??= new Item();
                Main.hoverItemName = hoverText;
            }
            return clicked;
        }

        protected override void DrawSlot(SpriteBatch batch, Item item, Vector2 position)
        {
            base.DrawSlot(batch, item, position);
            if(_pPlayer == null) { return; }

            if(_isVisible != _pPlayer.IsProjectorVisible(_slotIndex))
            {
                _pPlayer.SetProjectorVisible(_slotIndex, _isVisible);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    _pPlayer.SendProjectorFlags(_slotIndex);
                }
            }

            bool foundO = _pPlayer.TryGetProjector(_slotIndex, out var proj);
            if ((Item?.IsAir ?? true))
            {
                Item = foundO ? ProjectionUtils.NewPlayerProjector(proj)?.Item : null;
            }
            else if(Item?.ModItem is PlayerProjectorItem projI && projI.Projector != proj)
            {
                projI.Setup(proj);
            }

            var tex = _isVisible ? TextureAssets.InventoryTickOn.Value : TextureAssets.InventoryTickOff.Value;
            Rectangle rectangle = new Rectangle
                ((int)(position.X - 58 + 64 + 28),
                (int)(position.Y - 2), tex.Width, tex.Height);

            if(DrawButton(batch, tex, rectangle, Interactable, Lang.inter[_isVisible ? 60 : 59].Value, out bool hovered0))
            {
                _isVisible = !_isVisible;
                _pPlayer.SetProjectorVisible(_slotIndex, _isVisible);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    _pPlayer.SendProjectorFlags(_slotIndex);
                }
            }

            rectangle = new Rectangle
                ((int)(position.X - 4),
                (int)(position.Y - 2), tex.Width, tex.Width);

            tex = _gearIcon.Value;
            bool enabled = _pPlayer.TryGetProjector(_slotIndex, out proj) && Interactable;
            if (DrawButton(batch, tex, rectangle, enabled, "Projector Settings", out bool hovered1) && enabled)
            {
                UISystem.OpenProjectorUI(proj);
            }

            this.IsLocked = hovered0 | hovered1;
        }
    }
}
