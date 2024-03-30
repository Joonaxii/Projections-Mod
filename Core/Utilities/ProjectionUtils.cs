using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.Players;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Content.Items;
using Projections.Content.Items.Consumables;
using Projections.Content.Items.Projectors;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Core.Utilities
{
    public delegate void OnIntern(ReadOnlySpan<char> str, uint hash);
    public static class ProjectionUtils
    {
        public static int GetProjectionItemID(this PType type)
        {
            return type switch
            {
                PType.TProjection => ModContent.ItemType<ProjectionItem>(),
                PType.TPMaterial => ModContent.ItemType<ProjectionMaterial>(),
                PType.TPBundle => ModContent.ItemType<ProjectionBundle>(),
                _ => 0,
            };
        }

        public static Item NewItem(int type, int stack = 1)
        {
            stack = Math.Max(stack, 1);

            Item item = new Item();
            item.SetDefaults(type);
            item.stack = stack;
            return item;
        }
        public static T NewModItem<T>(int stack = 1) where T : ModItem
        {
            return NewItem(ModContent.ItemType<T>(), stack)?.ModItem as T;
        }

        public static IProjectionItemBase NewProjectionItem(int type, ProjectionIndex index, ProjectionSource? source = null, int stack = 1)
        {
            Item item = NewItem(type, stack);
            if (item.ModItem is IProjectionItemBase pBase)
            {
                pBase.SetIndex(index, source);
                return pBase;
            }
            return null;
        }
        public static T NewProjectionItem<T>(ProjectionIndex index, ProjectionSource? source = null, int stack = 1) where T : ModItem, IProjectionItemBase
        {
            return NewProjectionItem(ModContent.ItemType<T>(), index, source, stack) as T;
        }

        public static PlayerProjectorItem NewPlayerProjector(Projector projector)
        {
            PlayerProjectorItem newItem = NewModItem<PlayerProjectorItem>();
            newItem.Setup(projector);
            return newItem;
        }

        public static void RemoveExpertMaster(this List<TooltipLine> tooltips, PRarity rarity)
        {
            switch (rarity)
            {
                case PRarity.Expert:
                case PRarity.Master:
                    tooltips.RemoveAll((TooltipLine tip) => tip.Name == "Master" || tip.Name == "Expert");
                    break;
            }
        }

        public static uint ToHeaderIdentifier(this string str)
        {
            ReadOnlySpan<char> span = str;
            return span.ToHeaderIdentifier();
        }

        public static uint ToHeaderIdentifier(this Span<char> str)
        {
            ReadOnlySpan<char> span = str;
            return span.ToHeaderIdentifier();
        }

        public static uint ToHeaderIdentifier(this ReadOnlySpan<char> str)
        {
            uint value = 0;
            for (int i = 0, j = 0; i < str.Length && i < 4; i++, j += 8)
            {
                value |= ((uint)(str[i] & 0xFF)) << j;
            }
            return value;
        }

        public static int IndexOfItem(this Player player, Item item)
        {
            if(item == null || item.IsAir) { return -1; }

            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i] == item) { return i; }
            }
            return -1;
        }

        public static bool HasItem<T>(this Chest chest) where T : ModItem
        {
            return (chest?.item).HasItem(ModContent.ItemType<T>());
        }
        public static bool HasItem<T>(this IList<Item> items) where T : ModItem
        {
            return items.HasItem(ModContent.ItemType<T>());
        }

        public static bool HasItem(this Chest chest, int type)
        {
            return (chest?.item).HasItem(type);
        }
        public static bool HasItem(this IList<Item> items, int type)
        {
            if (type <= 0 || items == null) { return false; }
            for (int i = 0; i < items.Count; i++)
            {
                if ((items[i]?.type ?? 0) == type) { return true; }
            }
            return false;
        }

        public static bool ValidateProjections(this Player player, bool inventoryOnly = false)
        {
            if(player == null || !player.active) { return false; }

            bool validatedAny = false;
            validatedAny |= ValidateProjections(player.inventory);
            if (inventoryOnly) { return validatedAny; }

            validatedAny |= ValidateProjections(player.bank);
            validatedAny |= ValidateProjections(player.bank2);
            validatedAny |= ValidateProjections(player.bank3);
            validatedAny |= ValidateProjections(player.bank4);
            return validatedAny;
        }

        public static bool ValidateProjections(this Chest items)
            => ValidateProjections(items?.item);

        public static bool ValidateProjections(this IList<Item> items)
        {
            if(items == null) { return false; }
            bool validatedAny = false;
            foreach (var item in items)
            {
                validatedAny |= item.ValidateProjection();
            }
            return validatedAny;
        }

        public static bool ValidateProjection(this Item item)
        {
            if (item == null || item.IsAir) { return false; }
            if (item.ModItem is IProjectionItemBase proj)
            {
                proj.Validate();
                return true;
            }
            return false;
        }

        public static void GetTraderPoolSizes(out int pCount, out int mCount)
        {
            pCount = 0;
            mCount = 0;

            // There might be a better way to do this, but this is how we determine how many projections/materials the
            // ProjectionTrader sells based on current progression in the world
            if (NPC.downedBoss1 | NPC.downedBoss2 | NPC.downedSlimeKing)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedQueenBee | NPC.downedBoss3)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedQueenBee & NPC.downedBoss3)
            {
                pCount++;
                mCount++;
            }

            if (Main.hardMode)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedMechBoss1)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedMechBoss2)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedMechBoss3)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedPlantBoss)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedGolemBoss)
            {
                pCount++;
                mCount++;
            }

            if (NPC.downedFishron)
            {
                pCount++;
                mCount++;
            }
        }

        public static Color ToColor(this PRarity rarity)
        {
            if (rarity < 0) { return Colors.RarityTrash; }
            return rarity switch
            {
                PRarity.Intermediate => Colors.RarityGreen,
                PRarity.Advanced => Colors.RarityDarkPurple,
                PRarity.Expert => Main.DiscoColor,
                PRarity.Master => ProjectorSystem.MasterColor,
                _ => Colors.RarityBlue,
            };
        }
        public static int ToTerrariaRarity(this PRarity rarity)
        {
            return rarity switch
            {
                PRarity.Intermediate => ItemRarityID.Green,
                PRarity.Advanced => ItemRarityID.Purple,
                PRarity.Expert => ItemRarityID.Expert,
                PRarity.Master => ItemRarityID.Master,
                _ => ItemRarityID.Blue,
            };
        }
        public static int ToIndex(this PRarity rarity)
        {
            int rarityI = rarity < 0 ? 0 : (int)rarity;
            return rarityI > 3 ? 3 : rarityI;
        }

        public static int ToCopperValue(this PRarity rarity)
        {
            switch (rarity)
            {
                default: return 0;
                case PRarity.Basic:
                    return 10000;
                case PRarity.Intermediate:
                    return 25000;
                case PRarity.Advanced:
                    return 50000;
                case PRarity.Expert:
                    return 100000;
                case PRarity.Master:
                    return 150000;
            }
        }

        public static string ToDurationString(this TimeSpan tSpan)
        {
            return $"{(int)tSpan.TotalHours:D2}:{tSpan.Minutes:D2}:{tSpan.Seconds:D2}";
        }

        public static ProjectionsPlayer PPlayer(this Player plr) => plr?.GetModPlayer<ProjectionsPlayer>();

        public static void DrawInWorld(SpriteBatch spriteBatch, Vector2 position, float width, float height, float refScale, Texture2D texture, Color color, float scale, Rectangle? rect = null, float rotation = 0)
        {
            position -= Main.screenPosition - new Vector2(width * 0.5f, height * 0.5f);
            if (rect != null)
            {
                var rct = rect.Value;
                spriteBatch.Draw(texture, position, rect, color, rotation, new Vector2(rct.Width * 0.5f, rct.Height * 0.5f), scale * (refScale / MathF.Max(rct.Width, rct.Height)), SpriteEffects.None, 0);
                return;
            }
            spriteBatch.Draw(texture, position, null, color, rotation, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), scale * (refScale / MathF.Max(texture.Width, texture.Height)), SpriteEffects.None, 0);
        }
        public static void DrawInGUI(SpriteBatch spriteBatch, Vector2 position, float refScale, Texture2D texture, Color color, float scale, Rectangle? rect = null, Vector2? origin = null)
        {
            if (rect != null)
            {
                var rct = rect.Value;
                origin = origin == null ? new Vector2(rct.Width * 0.5f, rct.Height * 0.5f) : origin;
                spriteBatch.Draw(texture, position, rect, color, 0, origin.Value, scale * (refScale / MathF.Max(rct.Width, rct.Height)), SpriteEffects.None, 0);
                return;
            }
            origin = origin == null ? new Vector2(texture.Width * 0.5f, texture.Height * 0.5f) : origin;
            spriteBatch.Draw(texture, position, null, color, 0, origin.Value, scale * (refScale / MathF.Max(texture.Width, texture.Height)), SpriteEffects.None, 0);
        }

        public static bool IsContentPath(this ReadOnlySpan<char> input) =>
            input.StartsWith("tmod:", StringComparison.InvariantCultureIgnoreCase);
        public static bool IsContentPath(this Span<char> input)
        {
            ReadOnlySpan<char> sp = input;
            return sp.IsContentPath();
        } 
        public static bool IsContentPath(this string input) =>
            input.StartsWith("tmod:", StringComparison.InvariantCultureIgnoreCase);

        public static uint GetProjectionHash(this string input)
        {
            ReadOnlySpan<char> rSp = input;
            return rSp.GetProjectionHash();
        }
        public static uint GetProjectionHash(this Span<char> input)
        {
            ReadOnlySpan<char> rSp = input;
            return rSp.GetProjectionHash();
        }
        public static uint GetProjectionHash(this ReadOnlySpan<char> input)
        {
            if (input.Length < 1) { return 0; }

            uint value = CRC32.Calculate(input);
            return value == 0 ? 1 : value;
        }

        public static void BuildIgnoreList(this IList<Item> items, HashSet<ProjectionIndex> ignoreList, PType type, bool requireFavorite)
        {
            foreach(var item in items)
            {
                if(item?.ModItem is IProjectionItemBase pItem && type == pItem.PType && 
                    (!requireFavorite || item.favorited) && pItem.Index.IsValidID())
                {
                    ignoreList.Add(pItem.Index);
                }
            }
        }

        public static ProjectionIndex ParseProjection(this Span<char> input, OnIntern intern = null)
        {
            ReadOnlySpan<char> rSp = input;
            return rSp.ParseProjection(intern);
        }
        public static ProjectionIndex ParseProjectionID(this string input, OnIntern intern = null)
        {
            ReadOnlySpan<char> rSp = input;
            return rSp.ParseProjection(intern);
        }
        public static ProjectionIndex ParseProjection(this ReadOnlySpan<char> input, OnIntern intern = null)
        {
            int ind = input.IndexOf(':');
            if (ind > -1)
            {
                var group = input.Slice(0, ind);
                var groupHash = group.GetProjectionHash();
                intern?.Invoke(group, groupHash);
                return new ProjectionIndex(groupHash, input.Slice(ind + 1).GetProjectionHash());
            }
            return ProjectionIndex.Zero;
        }
 
        private const float DEFAULT_SEARCH_RADIUS = 40.0f;
        public static void GetNearByProjectors(this Vector2 position, Action<Projector> onValid, float tileRadius, int? plrToIgnore, bool requireActive)
        {
            if (ProjectorSystem.Instance == null || onValid == null) { return; }

            tileRadius *= 16.0f;
            tileRadius *= tileRadius;

            foreach (var proj in ProjectorSystem.Instance.TileProjectors)
            {
                if ((!requireActive || proj.IsActive) && Vector2.DistanceSquared(position, proj.Position) <= tileRadius)
                {
                    onValid.Invoke(proj);
                }
            }

            foreach (var proj in ProjectorSystem.Instance.CustomProjectors)
            {
                if ((!requireActive || proj.IsActive) && Vector2.DistanceSquared(position, proj.Position) <= tileRadius)
                {
                    onValid.Invoke(proj);
                }
            }

            if (plrToIgnore != null)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    var plr = Main.player[i];
                    if (plr.active && plrToIgnore != i)
                    {
                        var pPlayer = plr.PPlayer();
                        if (pPlayer?.CanProject ?? false)
                        {
                            if (Vector2.DistanceSquared(position, plr.Center) <= tileRadius)
                            {
                                for (int j = 0; j < pPlayer.ProjectorCount; j++)
                                {
                                    if (pPlayer.TryGetProjector(j, out var proj) && (!requireActive || proj.IsActive))
                                    {
                                        onValid.Invoke(proj);
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }

        public static bool GetNearByProjectors(this Vector2 position, List<Projector> projectors, float tileRadius = DEFAULT_SEARCH_RADIUS, int? plrToIgnore = null, bool requireActive = true)
        {
            if (ProjectorSystem.Instance == null || projectors == null) { return false; }
            int og = projectors.Count;
            position.GetNearByProjectors(projectors.Add, tileRadius, plrToIgnore, requireActive);
            return og != projectors.Count;
        }

        public static int CountNearByProjectors(this Vector2 position, float tileRadius = DEFAULT_SEARCH_RADIUS, int? plrToIgnore = null, bool requireActive = true)
        {
            if (ProjectorSystem.Instance == null) { return 0; }
            int count = 0;
            position.GetNearByProjectors((_) => { count++; }, tileRadius, plrToIgnore, requireActive);
            return count;
        }
    }
}
