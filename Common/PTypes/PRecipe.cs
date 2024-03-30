using Microsoft.Xna.Framework;
using Projections.Common.Items;
using Projections.Common.Netcode;
using Projections.Content.Items;
using Projections.Core.Collections;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;

namespace Projections.Common.PTypes
{
    public class PRecipe
    {
        public bool IsValid
        {
            get
            {
                for (int i = 0; i < _recipe.Count; i += _alts)
                {
                    ref var rec = ref _recipe[i];
                    switch (rec.type)
                    {
                        case RecipeType.Vanilla:
                        case RecipeType.Modded:
                            if (rec.itemID != 0) { return true; }
                            break;
                        case RecipeType.Projection:
                        case RecipeType.ProjectionMaterial:
                            if (Projections.IsValidIndex(rec.index, rec.type == RecipeType.ProjectionMaterial ? PType.TPMaterial : PType.TProjection)) { return true; }
                            break;
                    }
                }
                return false;
            }
        }
        public int Length => _recipe.Count / _alts;

        public RecipeItem this[int i]
        {
            get
            {
                if (CheckNotFinalized())
                {
                    return RecipeItem.FromNone();
                }

                int j = i * _alts;
                if (j < 0 || j >= _recipe.Count)
                {
                    return RecipeItem.FromNone();
                }

                if (TryGetFirstValid(j, out int v))
                {
                    return _recipe[v];
                }
                return RecipeItem.FromNone();
            }
        }

        private RefList<RecipeItem> _recipe = new RefList<RecipeItem>(16);
        private bool _finalized = false;
        private int _alts = 0;

        internal PRecipe() => _alts = 1;
        internal PRecipe(int alts) => _alts = alts;

        public static PRecipe Create(int alternates = 0)
        {
            PRecipe recipe = new PRecipe(Math.Max(alternates, 0) + 1);
            return recipe;
        }
        public PRecipe AddIngredient(RecipeItem main)
        {
            int len = Length;
            int shouldBe = _alts * len;
            for (int i = len; i < shouldBe; i++)
            {
                _recipe.Add(RecipeItem.FromNone());
            }
            _recipe.Add(main);
            return this;
        }
        public PRecipe AddAlt(RecipeItem main)
        {
            if (_recipe.Count <= 0)
            {
                Projections.Log(LogType.Warning, "P-Recipe must have at least one ingridient in order to add alts!");
                return this;
            }

            if (_recipe.Count % _alts == 0)
            {
                Projections.Log(LogType.Warning, $"P-Recipe ingridient already has max number of alts! ({_alts - 1} alternates allowed for this P-Recipe)");
                return this;
            }

            _recipe.Add(main);
            return this;
        }
        public void Finish()
        {
            PadWithNone();
            _finalized = true;
        }

        public void SpawnAsItems(Vector2 position, Action<Item> onSpawn = null)
        {
            if (CheckNotFinalized())
            {
                return;
            }

            for (int i = 0; i < _recipe.Count; i += _alts)
            {
                if (TryGetFirstValid(i, out int j))
                {
                    ref var item = ref _recipe[j];
                    switch (item.type)
                    {
                        case RecipeType.Vanilla:
                        case RecipeType.Modded:
                            ProjectionNetUtils.SpawnItem(item.itemID, position, item.count, onSpawn);
                            break;
                        case RecipeType.ProjectionMaterial:
                            ProjectionNetUtils.SpawnProjectionItem<ProjectionMaterial>(item.index, position, item.count, onSpawn);
                            break;
                        case RecipeType.Projection:
                            ProjectionNetUtils.SpawnProjectionItem<ProjectionItem>(item.index, position, item.count, onSpawn);
                            break;
                    }
                }
            }
        }

        public bool IsUsed(int itemID)
        {
            if (itemID <= 0) { return false; }
            if (CheckNotFinalized()) { return false; }

            for (int i = 0; i < _recipe.Count; i += _alts)
            {
                if (TryGetFirstValid(i, out int j))
                {
                    ref var itm = ref _recipe[j];
                    if ((itm.type & RecipeType.Vanilla & RecipeType.Modded) != 0 && itm.itemID == itemID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool IsUsed(ProjectionIndex index, bool material)
        {
            if (!index.IsValidID())
            {
                return false;
            }
            if (CheckNotFinalized()) { return false; }

            for (int i = 0; i < _recipe.Count; i += _alts)
            {
                if (TryGetFirstValid(i, out int j))
                {
                    ref var itm = ref _recipe[j];
                    if (material ?
                   itm.type == RecipeType.ProjectionMaterial && itm.index == index
                   : itm.type == RecipeType.Projection && itm.index == index
                   )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasAllItems(List<ItemRef> items, Span<int> current)
        {
            if (CheckNotFinalized()) { return false; }

            Span<int> required = stackalloc int[Length];
            return CheckAllItems(items, required, current);
        }
        public bool ConsumeAllItems(List<ItemRef> items, Span<int> current)
        {
            if (CheckNotFinalized()) { return false; }

            Span<int> required = stackalloc int[Length];
            if (CheckAllItems(items, required, current))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ItemRef refI = items[i];
                    items[i] = refI.UpdateStack(current[i]);
                }
                return true;
            }
            return false;
        }

        internal void MarkItemsAsUsed()
        {
            if (CheckNotFinalized()) { return; }
            for (int i = 0; i < _recipe.Count; i += _alts)
            {
                if (TryGetFirstValid(i, out int j))
                {
                    _recipe[j].MarkItemUsed();
                }
            }
        }

        private bool CheckAllItems(List<ItemRef> items, Span<int> required, Span<int> current)
        {
            for (int i = 0; i < items.Count; i++)
            {
                current[i] = items[i].Item?.stack ?? 0;
            }

            int total = 0;
            for (int i = 0; i < required.Length; i++)
            {
                ref var rec = ref _recipe[i];
                required[i] = rec.CanBeUsed ? rec.count : 0;
                total += required[i];
            }
            if (total <= 0) { return false; }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i].Item;
                for (int j = 0, k = 0; j < required.Length; j++, k = _alts)
                {
                    if (current[i] <= 0) { break; }
                    if (required[j] <= 0) { continue; }
                    if (TryGetFirstValid(k, out int iOut))
                    {
                        ref var rec = ref _recipe[iOut];
                        if (rec.IsSameAs(item))
                        {
                            int size = Math.Min(required[j], current[i]);
                            required[j] -= size;
                            current[i] -= size;
                            total -= size;
                            if (total <= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool TryGetFirstValid(int index, out int indOut)
        {
            for (int i = 0, j = index; i < _alts; i++, j++)
            {
                if (_recipe[j].CanBeUsed)
                {
                    indOut = j;
                    return true;
                }
            }
            indOut = -1;
            return false;
        }

        private void PadWithNone()
        {
            int len = Length;
            int shouldBe = _alts * len;
            for (int i = len; i < shouldBe; i++)
            {
                _recipe.Add(RecipeItem.FromNone());
            }
        }
        private bool CheckNotFinalized()
        {
            if (_finalized) { return false; }
            Projections.Log(LogType.Warning, "P-Recipe has not been finished! (Remember to call 'Finish' after adding ingredients and alts)");
            return true;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RecipeItem
    {
        public bool IsOK
        {
            get
            {
                if (count <= 0) { return false; }
                switch (type)
                {
                    default:
                        return false;

                    case RecipeType.Vanilla:
                    case RecipeType.Modded:
                        return itemID > 0;

                    case RecipeType.Projection:
                    case RecipeType.ProjectionMaterial:
                        return index.IsValidID();
                }
            }
        }
        public bool CanBeUsed
        {
            get
            {
                if (!IsOK) { return false; }
                switch (type)
                {
                    default: return true;
                    case RecipeType.Projection:
                        return Projections.IsValidIndex(index, PType.TProjection);
                    case RecipeType.ProjectionMaterial:
                        return Projections.IsValidIndex(index, PType.TPMaterial);
                }
            }
        }

        [FieldOffset(0)] public RecipeType type;
        [FieldOffset(1)] public int itemID;
        [FieldOffset(1)] public ProjectionIndex index;
        [FieldOffset(9)] public int count;

        public static RecipeItem FromNone()
        {
            return new RecipeItem()
            {
                type = RecipeType.None,
                index = ProjectionIndex.Zero,
                count = 0,
                itemID = 0,
            };
        }

        public static RecipeItem FromID(int itemID, int stack)
        {
            return new RecipeItem()
            {
                type = RecipeType.Vanilla,
                index = ProjectionIndex.Zero,
                count = stack,
                itemID = itemID,
            };
        }
        public static RecipeItem FromModded(ReadOnlySpan<char> itemName, int stack)
        {
            return new RecipeItem()
            {
                type = RecipeType.Modded,
                index = ProjectionIndex.Zero,
                count = stack,
                itemID = Projections.GetItemIndex(itemName),
            };
        }

        public static RecipeItem FromProjection(ReadOnlySpan<char> name, PType type, int stack)
        {
            return FromProjection(name.ParseProjection(), type, stack);
        }
        public static RecipeItem FromProjection(ProjectionIndex index, PType type, int stack)
        {
            return new RecipeItem()
            {
                type = (RecipeType)(type + (int)RecipeType.Projection),
                index = index,
                count = stack,
                itemID = 0
            };
        }

        public bool IsSameAs(Item item)
        {
            if (IsOK) { return false; }
            switch (type)
            {
                case RecipeType.Vanilla:
                case RecipeType.Modded:
                    return item.type == itemID;
                case RecipeType.ProjectionMaterial:
                case RecipeType.Projection:

                    if (item.ModItem is IProjectionItemBase proj && proj.Index == index) {
                        switch (proj.PType)
                        {
                            case PType.TProjection:
                                return type == RecipeType.Projection;
                            case PType.TPMaterial:
                                return type == RecipeType.ProjectionMaterial;
                            case PType.TPBundle:
                                return type == RecipeType.ProjectionBundle;
                        }
                    }
                    return false;
            }
            return false;
        }

        internal void MarkItemUsed()
        {
            if (count <= 0) { return; }
            switch (type)
            {
                case RecipeType.Vanilla:
                case RecipeType.Modded:
                    Projections.MarkItemAsMaterial(itemID);
                    break;
                case RecipeType.ProjectionMaterial:
                    Projections.MarkItemAsMaterial(index, PType.TPMaterial);
                    break;
                    
                case RecipeType.ProjectionBundle:
                    Projections.MarkItemAsMaterial(index, PType.TPBundle);
                    break;

                case RecipeType.Projection:
                    Projections.MarkItemAsMaterial(index, PType.TProjection);
                    break;
            }
        }
    }
}