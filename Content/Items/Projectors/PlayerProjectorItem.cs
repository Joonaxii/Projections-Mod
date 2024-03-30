using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.Configs;
using Projections.Common.Netcode;
using Projections.Common.ProjectorTypes;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using rail;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Projections.Content.Items.Projectors
{
    public class PlayerProjectorItem : OverrideShimmerFX, IProjector
    {
        public override string Texture => "Projections/Content/Items/Projectors/PlayerProjectorItem";

        public delegate int OnIterateSlot(int index, in ProjectorSlot slot, bool isActive);
        public Projector Projector => _curProjector;

        public virtual bool CanBeUsedInRecipe => false;
        public bool IsInitalized => _curProjector != null || _pData.slotCount > 0;

        public ProjectorType ProjectorType => ProjectorType.Player;
        public virtual int CopperValue => 2500;

        public virtual int DefaultSlotCount => 1;
        public virtual int SlotCount => _curProjector?.SlotCount ?? _pData.slotCount;

        public virtual Vector2 DefaultHotspot => default;
        public virtual Vector2 Hotspot => _curProjector?.Hotspot ?? _pData.hotspot;

        public virtual uint DefaultCreatorTag => Projections.DEFAULT_PROJECTOR_ID;
        public virtual uint CreatorTag => _curProjector?.CreatorTag ?? _pData.creatorTag;

        public bool IsActualItem
        {
            get => _isActualItem;
            set => _isActualItem = value;
        }
        public string NameExtension => _nameExt;

        private string _nameExt;
        private ProjectorData _pData;
        private ProjectorSettings _settings;
        private ProjectorSlot[] _slots = new ProjectorSlot[0];
        private bool _isActualItem = true;

        private Projector _curProjector;
        private static Asset<Texture2D> _main;
        private static Asset<Texture2D> _glow;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
        }

        public override void SetDefaults()
        {
            _main ??= ModContent.Request<Texture2D>(Texture);
            _glow ??= ModContent.Request<Texture2D>("Projections/Content/Items/Projectors/PlayerProjectorItem_Glow");

            base.SetDefaults();

            Item.maxStack = 1;
            Item.value = 0;
            Item.width = 30;
            Item.height = 40;
            Item.value = CopperValue;
            Item.rare = ItemRarityID.Pink;
        }

        public void Setup(Projector projector)
        {
            if(projector != null && projector.Type != ProjectorType) { return; }
            _curProjector = projector;
            UpdateStats();
        }


        public bool IterateSlots(OnIterateSlot onIterate, out bool shouldSync)
        {
            shouldSync = false;
            if (onIterate == null) { return false; }

            bool isPlaying = false;
            if(_curProjector != null)
            {
                for (int i = 0; i < _curProjector.SlotCount; i++)
                {
                    switch(onIterate.Invoke(i, in _curProjector.GetSlot(i), i == _curProjector.ActiveSlotIndex))
                    {
                        case 1: return _curProjector.IsPlaying;
                        case 2:
                            _curProjector.GetSlot(i).Setup(ProjectionIndex.Zero, 0);
                            shouldSync = true;
                            break;
                    }
                }
                return _curProjector.IsPlaying;
            }
            else if(IsInitalized) 
            {
                isPlaying = _settings.IsPlaying;
                for (int i = 0; i < _pData.slotCount; i++)
                {
                    bool isActive = i == _settings.activeSlot;
                    int ret = onIterate.Invoke(i, in _slots[i], isActive);

                    if (isActive && _slots[i].IsEmpty)
                    {
                        isPlaying = false;
                    }

                    if (ret == 1) { break; }
                    else if (ret == 2)
                    {
                        _slots[i].Setup(ProjectionIndex.Zero, 0);
                        if (isActive)
                        {
                            isPlaying = false;
                        }
                    }
                }
            }
            return isPlaying;
        }

        public virtual Asset<Texture2D> GetMainTexture() => _main;
        public virtual Asset<Texture2D> GetGlowTexture() => _glow;

        public ProjectionIndex GetActiveIndex(out bool isPlaying)
        {
            if(_curProjector != null)
            {
                isPlaying = _curProjector.IsPlaying;
                return _curProjector.ActiveSlot.Index;
            }
            else if (IsInitalized)
            {
                isPlaying = _settings.IsPlaying && !_slots[_settings.activeSlot].IsEmpty;
                return _slots[_settings.activeSlot].Index;
            }

            isPlaying = false;
            return ProjectionIndex.Zero;
        }

        public override bool CanRightClick() => !Item.favorited;

        private static float _time;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {      
            if (IsInitalized)
            {
                ref readonly ProjectorData pData = ref (_curProjector != null ?  ref _curProjector.Data : ref _pData);
                int totalSlotC = _curProjector?.SlotCount ?? _pData.slotCount;
                int active = _curProjector?.ActiveSlotIndex ?? _settings.activeSlot;

                bool found = ProjectorSystem.TryGetProjectorTypeName(pData.creatorTag, out var tagName);
                tooltips.Add(new TooltipLine(Mod, "P-Type", $"Type: {tagName}") { OverrideColor = Colors.RarityTrash });
                tooltips.Add(new TooltipLine(Mod, "P-Slots", $"Slots: {totalSlotC}"){ OverrideColor = Colors.RarityTrash});
                tooltips.Add(new TooltipLine(Mod, "P-Time", $"Time Projected: {TimeSpan.FromSeconds(pData.timeInUse).ToDurationString()}"){OverrideColor = Colors.RarityTrash});

                if (Main.keyState.PressingShift())
                {
                    if(!Item.favorited && IsActualItem)
                    {
                        tooltips.Add(new TooltipLine(Mod, "P-Extra", "Right Click to unpack..."));
                    }
                    const int MAX_SLOTS_VISIBLE = 8;
                    float time = _time * 2.0f * (float)(1.0f / MathF.Max(totalSlotC - MAX_SLOTS_VISIBLE, 1));
                    time %= 3.0f;

                    bool reverse = false;

                    if(time >= 1.0f && time < 1.5f)
                    {
                        time = MathF.Min(time, 1.0f);
                    }
                    else if (time >= 1.5f)
                    {
                        reverse = true;
                        time = MathF.Min(time - 1.5f, 1.0f);
                    }

                    int curI = totalSlotC <= MAX_SLOTS_VISIBLE ? 0 : (int)((reverse ? 1.0f - time : time) * Math.Max((int)(totalSlotC - MAX_SLOTS_VISIBLE), 0));
                    for (int j = 0, i = curI; j < Math.Min(totalSlotC, MAX_SLOTS_VISIBLE); j++, i++)
                    {
                        ref readonly var slot = ref (_curProjector != null ? ref _curProjector.GetSlot(i) : ref _slots[i]);
                        if (slot.IsEmpty)
                        {
                            tooltips.Add(new TooltipLine(Mod, "P-Slot", $" - Slot: {i + 1} -> Empty") { OverrideColor = active == i ? Colors.CoinGold : Colors.RarityTrash });
                        }
                        else
                        {
                            if (slot.Projection != null)
                            {
                                tooltips.Add(new TooltipLine(Mod, "P-Slot", $" - Slot: {i + 1} -> {slot.Projection.Name} [{slot.Stack}]") { OverrideColor = slot.Projection.Rarity.ToColor().MultiplyRGBA(active == i ? Colors.CoinGold : Color.White) });
                            }
                            else
                            {
                                tooltips.Add(new TooltipLine(Mod, "P-Slot", $" - Slot: {i + 1} -> Unknown [{slot.Stack}]") { OverrideColor = active == i ? Colors.CoinGold : Colors.RarityTrash });
                            }
                        }
                    }
                    _time += ProjectorSystem.Instance?.FrameDelta ?? (1.0f / 60.0f);
                }
                else
                {
                    _time = 0.0f;
                    tooltips.Add(new TooltipLine(Mod, "P-Extra", "Hold Shift for details..."));
                } 
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "P-NoData", "An empty projector...") { OverrideColor = Colors.RarityTrash });
            }
        }
        public override ModItem Clone(Item newEntity)
        {
            var pItm = base.Clone(newEntity) as PlayerProjectorItem;

            pItm._nameExt = _nameExt;
            pItm._isActualItem = _isActualItem;
            pItm._curProjector = _curProjector;
            if (_curProjector != null)
            {
                pItm.Pack(_curProjector);
                Item.value = CopperValue;
                pItm.UpdateStats();
            }
            else
            {
                pItm._pData = _pData;
                pItm._settings = _settings;

                Array.Resize(ref pItm._slots, _pData.slotCount);
                _slots.CopyTo(pItm._slots, 0);
            }
            pItm.UpdateStats();
            return pItm;
        }

        public override bool ConsumeItem(Player player) => false;
        public override void RightClick(Player player)
        {
            var pPlayer = player.PPlayer();          
            if(IsActualItem)
            {
                if (Main.keyState.PressingShift())
                {
                    Unpack(player);
                }
                else if(pPlayer.CanProject && !Item.favorited)
                {
                    if (pPlayer.TryPushPlayerProjector(this))
                    {
                        int ind = player.IndexOfItem(Item);
                        Main.EquipPage = 2;
                        Main.EquipPageSelected = 2;
                        SoundEngine.PlaySound(SoundID.Grab);
                        Item.TurnToAir(true);
                        if (ind > -1 && Main.netMode != NetmodeID.SinglePlayer)
                        {
                            NetMessage.SendData(MessageID.SyncEquipment, -1, player.whoAmI, number: player.whoAmI, number2: ind);
                        }
                    }
                } 
            }
        }

        public override bool CanStack(Item source) => false;
        public override bool CanStackInWorld(Item source) => false;

        public override void NetReceive(BinaryReader reader)
        {
            bool noRead = reader.ReadBoolean();
            
            if(noRead) { return; }
            _pData.Deserialize(reader);
            _settings.Deserialize(reader);
            Array.Resize(ref _slots, _pData.slotCount);
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Deserialize(reader);
            }
            _curProjector = null;
            UpdateStats();
        }
        public override void NetSend(BinaryWriter writer)
        {
            bool noRead = _curProjector != null;
            writer.Write(noRead);

            if(noRead) { return; }

            _pData.Serialize(writer);
            _settings.Serialize(writer);
            for (int i = 0; i < _pData.slotCount; i++)
            {
                _slots[i].Serialize(writer);
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGetSafe<TagCompound>("P-Data", out var data))
            {
                _pData.Load(data);
            } else { _pData = default; }
            Array.Resize(ref _slots, _pData.slotCount);

            if (tag.TryGetSafe<TagCompound>("P-Settings", out data))
            {
                _settings.Load(data);
            }
            else { _settings = default; }

            if (tag.TryGetSafe<IList<TagCompound>>("P-Slots", out var slotData))
            {
                int count = Math.Min(slotData.Count, _pData.slotCount);
                for (int i = 0; i < count; i++)
                {
                    _slots[i].Load(slotData[i]);
                }
                for (int i = count; i < _pData.slotCount; i++)
                {
                    _slots[i].Setup(ProjectionIndex.Zero, 0);
                    _slots[i].Reset();
                }
            }
            _isActualItem = true;
            UpdateStats();
        }
        public override void SaveData(TagCompound tag)
        {
            TagCompound pData = new TagCompound();
            _pData.Save(pData);
            tag.Assign("P-Data", pData);
            _settings.Save("P-Settings", tag);
            List<TagCompound> slotTags = new List<TagCompound>();
            for (int i = 0; i < _pData.slotCount; i++)
            {
                TagCompound tagS = new TagCompound();
                _slots[i].Save(tagS);
                slotTags.Add(tagS);
            }
            tag.Assign("P-Slots", slotTags);
        }

        public bool Pack(Projector projector, int? player = null, int? slot = null)
        {
            if(projector == null) { return false; }
            {
                ref var pData = ref projector.Data;
                if(player != null)
                {
                    pData.id.owner = (short)player.Value;
                }

                if(slot != null)
                {
                    pData.id.projectorIndex = (short)slot.Value;
                }
                _pData = pData;
                _settings = projector.Settings;
                Array.Resize(ref _slots, _pData.slotCount);
                ReadOnlySpan<ProjectorSlot> slots = MemoryMarshal.CreateSpan(ref projector.GetSlot(0), _pData.slotCount);
                slots.CopyTo(_slots);
            }
            _curProjector = null;
            _isActualItem = true;
            UpdateStats();
            return false;
        }

        public void CreateEmptyData(uint creatorTag, Vector2 hotspt, int slotCount, string nameExt = null)
        {
            _pData = ProjectorData.NewPlayer(creatorTag, hotspt, slotCount, 0, 0);
            _settings.Reset();
            _nameExt = nameExt ?? "";

            Array.Resize(ref _slots, _pData.slotCount);
            for (int i = 0; i < _pData.slotCount; i++)
            {
                _slots[i].Setup(ProjectionIndex.Zero, 0);
                _slots[i].Reset();
            }
            UpdateStats();
        }

        public bool Unpack(int player, int slot, ref Projector projector)
        {
            _isActualItem = true;
            if (_curProjector != null)
            {
                projector = _curProjector;
                UpdateStats();
                return true;
            }

            if (!IsInitalized)
            {
                CreateEmptyData(DefaultCreatorTag, DefaultHotspot, DefaultSlotCount);
            }

            {
                _pData.id.owner = (short)player;
                _pData.id.projectorIndex = (short)slot;

                if (projector != null && _pData.MatchType(in projector.Data))
                {
                    projector.Data = _pData;
                }
                else
                {
                    projector = ProjectorSystem.GetNewProjector(in _pData);
                }

                if (projector != null)
                {
                    projector.Settings = _settings;
                    projector.ValidateSlotSize();
                    Span<ProjectorSlot> slots = MemoryMarshal.CreateSpan(ref projector.GetSlot(0), _pData.slotCount);
                    _slots.AsSpan().CopyTo(slots);
                }
                _curProjector = projector;
                UpdateStats();
                return projector != null;
            }
        }
        public bool Unpack(Player player)
        {
            if(_curProjector == null && !IsInitalized) { return false; }

            _isActualItem = true;
            bool unpacked = false;
            IterateSlots((int index, in ProjectorSlot slot, bool isActive) =>
            {
                if (!slot.IsEmpty)
                {
                    ProjectionNetUtils.SpawnProjectionItem<ProjectionItem>(slot.Index, player.Center, slot.Stack, null);
                    if(_curProjector != null)
                    {
                        _curProjector.GetSlot(index).Setup(ProjectionIndex.Zero, 0);
                    }
                    unpacked = true;
                }
                return 2;
            }, out bool shouldSync);
            UpdateStats();

            if(UISystem.Instance.CurrentProjector == _curProjector)
            {
                UISystem.RefreshProjectorUI();
            }

            if(_curProjector != null && shouldSync && Main.netMode != NetmodeID.SinglePlayer)
            {
                ProjectionNetUtils.SendProjectorUpdate(_curProjector, SerializeType.Full, player.whoAmI);
            }
            return unpacked;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            byte alpha = GetShimmerAlpha(alphaColor);
            var alphaC = new Color(alpha, alpha, alpha, alpha);

            var main = GetMainTexture();
            var glow = GetGlowTexture();

            if(main != null)
            {
                var uvs = GetUVs(true, true) ?? new Rectangle(0, 0, main.Width(), main.Height());
                Color color = lightColor.MultiplyRGBA(alphaC);
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, Math.Max(uvs.Width, uvs.Height), main.Value, color, scale, uvs);
            }

            if (glow != null)
            {
                var idx = GetActiveIndex(out bool isPlaying);
                var proj = Projections.GetMaterial(idx, PType.TProjection);

                var uvs = GetUVs(false, isPlaying) ?? new Rectangle(0, 0, glow.Width(), glow.Height());
                Color color = (proj != null ? proj.Rarity : 0).ToColor() * Main.essScale;
                ProjectionUtils.DrawInWorld(spriteBatch, Item.position, Item.width, Item.height, Math.Max(uvs.Width, uvs.Height), glow.Value, color.MultiplyRGBA(alphaC), scale, uvs);
            }
            return false;
        }
        private const float UI_SCALE = 2.5f;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {      
            var main = GetMainTexture();
            var glow = GetGlowTexture();

            scale *= UI_SCALE;
            if (main != null)
            {
                var uvs = GetUVs(true, true);
                ProjectionUtils.DrawInGUI(spriteBatch, position, 48.0f, main.Value, drawColor, scale, uvs);
            }

            if (glow != null)
            {
                var idx = GetActiveIndex(out bool isPlaying);
                var proj = Projections.GetMaterial(idx, PType.TProjection);

                var uvs = GetUVs(false, isPlaying);
                Color color = (proj != null ? proj.Rarity : 0).ToColor() * Main.essScale;
                ProjectionUtils.DrawInGUI(spriteBatch, position, 48.0f, glow.Value, color.MultiplyRGBA(drawColor), scale, uvs);
            }
            return false;
        }

        public virtual Rectangle? GetUVs(bool main, bool isActive)
        {
            var tex = main ? GetMainTexture() : GetGlowTexture();
            int width = ((tex.Width() - 2) >> 1);
            return new Rectangle(isActive ? width + 2 : 0, 0, width, tex.Height());
        }

        public override void PostUpdate()
        {
            PRarity rarity = PRarity.Basic;
            float power = 0.25f;
            if (Projections.TryGetProjection(GetActiveIndex(out bool isPlaying), out var proj))
            {
                rarity = proj.Rarity;
                power = isPlaying ? 0.75f : 0.5f;
            }
            Lighting.AddLight(Item.Center, rarity.ToColor().ToVector3() * power * Main.essScale);
            base.PostUpdate();
        }
        public override void AddRecipes()
        {
            var recG = ProjectorSystem.ProjectorRecipeGroup;
            foreach(var itemID in recG.ValidItems)
            {
                var recipe = Recipe.Create(Type);
                if(recipe.createItem.ModItem is PlayerProjectorItem plr)
                {
                    var item = ItemLoader.GetItem(itemID);
                    IProjector iProj = item as IProjector;
                    if (iProj == null && item is IProjectorItem pItem)
                    {
                        var mTile = ModContent.GetModTile(pItem.PlacementTileID);
                        iProj = mTile as IProjector;
                    }
                    
                    if (iProj != null)
                    {
                        plr.CreateEmptyData(iProj.CreatorTag, DefaultHotspot, iProj.SlotCount, iProj.NameExtension);
                    }
                    plr._isActualItem = false;
                }

                recipe.AddIngredient(itemID, 1)
                .AddIngredient(ItemID.Wire, 10)
                .AddIngredient(ItemID.Lens, 2)
                .AddTile(TileID.Anvils);

                recipe.Register();
            }
        }

        protected override void OnShimmer()
        {
            base.OnShimmer();
            var shimmerEV = (Item itm) =>
            {
                itm.shimmered = true;
                itm.shimmerTime = 1f;
                itm.wet = true;
                itm.shimmerWet = true;
            };

            if (IsInitalized && _curProjector == null)
            {
                IterateSlots((int index, in ProjectorSlot slot, bool isActive) =>
                {
                    if (!slot.IsEmpty)
                    {
                        ProjectionNetUtils.SpawnProjectionItem<ProjectionItem>(slot.Index, Item.Center, slot.Stack, shimmerEV);
                    }
                    return 0;
                }, out _);
            }
        }

        protected void UpdateStats()
        {
            long valueP = CopperValue;
            var config = ProjectionsServerConfig.Instance;
            IterateSlots((int index, in ProjectorSlot slot, bool isActive) =>
            {
                if (!slot.IsEmpty)
                {
                    int value = Projections.TryGetProjection(slot.Index, out var proj) ? proj.Value : ProjectionItem.DEFAULT_VALUE;
                    if (config.DisablePrices)
                    {
                        value = 0;
                    }
                    else if(config.MaxProjectionValue > -1)
                    {
                        value = Math.Min(config.MaxProjectionValue, value);
                    }
                    valueP += slot.Stack * value;
                }
                return 0;
            }, out _);
            Item.value = (int)Math.Min(valueP, int.MaxValue);
            Item.SetNameOverride($"{Lang.GetItemNameValue(Type)}{(string.IsNullOrWhiteSpace(_nameExt) ? "" : $" [{_nameExt}]")}");
        }
    }
}
