using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using ReLogic.Content;
using Projections.Core.Data;
using Projections.Core.Utilities;

namespace Projections.Common.PTypes.InMemory
{
    public class InMemoryPMaterial : PMaterial
    {
        public override Texture2D Icon => _icon?.Value;
        private Asset<Texture2D> _icon;

        internal InMemoryPMaterial() { }

        public override bool Load() => true;
        public override void Unload() => _icon = null;

        public static InMemoryPMaterial Create(
            ReadOnlySpan<char> id,
            ReadOnlySpan<char> name,
            PRarity rarity, int priority = 0, PMaterialFlags flags = PMaterialFlags.AllowShimmer)
        {
            if (Projections.Instance == null)
            {
                Projections.Log(LogType.Error, "Projections is not initialized! Cannot create PMaterial!");
                return null;
            }

            InMemoryPMaterial material = new InMemoryPMaterial();
            material._id = id.ToString();
            material._name = name.ToString();
            material._rarity = rarity;
            material._priority = priority;
            material._value = rarity.ToCopperValue();
            material._index = id.ParseProjection();
            material._flags = flags;
            return material;
        }

        public PMaterial SetDescription(ReadOnlySpan<char> description)
        {
            _description = description.ToString();
            return this;
        }
        public PMaterial SetIcon(Asset<Texture2D> icon)
        {
            _icon = icon;
            return this;
        }
        public PMaterial SetValue(int copper, int silver, int gold, int platinum)
        {
            copper = Utils.Clamp(copper, 0, 100);
            silver = Utils.Clamp(silver, 0, 100);
            gold = Utils.Clamp(gold, 0, 100);
            platinum = Utils.Clamp(platinum, 0, 999);
            _value = copper + silver * 100 + gold * 10000 + platinum * 1000000;
            return this;
        }
        public PMaterial AddRecipe(PRecipe recipe)
        {
            _recipes.Add(recipe);
            return this;
        }
    }
}