using Microsoft.Xna.Framework;
using Projections.Common.ProjectorTypes;
using Projections.Content.UI;
using Projections.Core.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Projections.Core.Systems
{
    public class UISystem : ModSystem
    {
        public static UISystem Instance { get; private set; }
        public Projector CurrentProjector => _menuOpen == MenuType.Projector ? _projectorUI.Projector : null;

        private enum MenuType
        {
            None,
            Projector,
            Crafting,
        }

        private MenuType _menuOpen;
        private ProjectorState _projectorUI;
        private PlayerInterfaceState _plrProjectorUI;
        private UserInterface _floatingInterface;
        private UserInterface _plrInterface;

        private bool _wasInterfaceActive = false;

        public override void Load()
        {
            if (Main.dedServ) { return; }

            Instance ??= this;
            _floatingInterface ??= new UserInterface();
            _plrInterface ??= new UserInterface();
            _projectorUI ??= new ProjectorState();
            _plrProjectorUI ??= new PlayerInterfaceState();
            _plrInterface.SetState(_plrProjectorUI);
            CloseProjectorUI(false, true);
        }

        public override void Unload()
        {
            CloseProjectorUI(false, true);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ || Main.gameMenu) { return; }

            if (_menuOpen != MenuType.None)
            {
                _floatingInterface?.Update(gameTime);
            }

            var pPlayer = Main.LocalPlayer?.PPlayer();
            if (((pPlayer?.CanProject ?? false)) && Main.playerInventory)
            {
                if (!_wasInterfaceActive)
                {
                    _wasInterfaceActive = true;
                    _plrProjectorUI.Init();
                }
                _plrInterface?.Update(gameTime);
            }else if (_wasInterfaceActive)
            {
                _wasInterfaceActive = false;
            }
        }

        public static void RefreshProjectorUI()
        {
            if (Instance == null || Instance.CurrentProjector == null || Instance._menuOpen == MenuType.None) { return; }
            Instance._projectorUI.RefreshUI();
        }

        public static void OpenProjectorUI(Projector projector)
        {
            if (Instance == null || projector == Instance.CurrentProjector) { return; }
            Instance._floatingInterface.SetState(Instance._projectorUI);
            Instance._projectorUI.SetProjector(projector, true);
            Instance._menuOpen = MenuType.Projector;
            SoundEngine.PlaySound(SoundID.MenuOpen);
            Main.playerInventory = true;
        }

        public static void CloseProjectorUI(bool noFlush, bool silent = false)
        {
            if (Instance == null || Instance._menuOpen == MenuType.None) { return; }
            Instance._floatingInterface.SetState(null);

            if (Instance._menuOpen == MenuType.Projector)
            {
                Instance._projectorUI.SetProjector(null, false);
            }
            if (!silent)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            Instance._menuOpen = MenuType.None;
        }

        public bool DoDrawFloating()
        {
            _floatingInterface?.Draw(Main.spriteBatch, new GameTime());
            return true;
        }

        public bool DoDrawPlayerInterface()
        {
            _plrInterface?.Draw(Main.spriteBatch, new GameTime());
            return true;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            var pPlayer = Main.LocalPlayer?.PPlayer();
            bool hasPlrInterface = (pPlayer?.CanProject ?? false) && Main.playerInventory;
            if (!hasPlrInterface && _menuOpen == MenuType.None) { return; }

            if (hasPlrInterface)
            {
                int ind = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory")) + 1;
                if (ind > 0)
                {
                    layers.Insert(ind, new LegacyGameInterfaceLayer("Projections: Player Projectors", DoDrawPlayerInterface, InterfaceScaleType.UI));
                }
            }

            if (_menuOpen != MenuType.None)
            {
                int ind = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
                if (ind >= 0)
                {
                    layers.Insert(ind, new LegacyGameInterfaceLayer("Projections: Projector Menu", DoDrawFloating, InterfaceScaleType.UI));
                }
            }
        }
    }
}
