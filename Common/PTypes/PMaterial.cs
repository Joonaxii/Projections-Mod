using Microsoft.Xna.Framework.Graphics;
using System;
using Projections.Core.Data.Structures;
using rail;
using Projections.Core.Collections;
using Projections.Common.Configs;
using Projections.Core.Data;

namespace Projections.Common.PTypes
{
    public abstract class PMaterial
    {
        public ProjectionIndex Index => _index;
        public string ID => _id;
        public ReadOnlySpan<char> Group
        {
            get
            {
                int ind = _id.IndexOf(':');
                return ind < 0 ? "" : _id.AsSpan().Slice(0, ind);
            }
        }

        public string Name => _name;
        public string Description => _description;

        public int Value
        {
            get
            {
                var serverConf = ProjectionsServerConfig.Instance;
                int max = serverConf?.MaxProjectionValue ?? -1;
                return max < 0 ? _value : Math.Clamp(_value, 0, max);
            }
        }
        public PRarity Rarity => _rarity;

        public int Priority => _priority;
        public abstract Texture2D Icon { get; }

        public Span<DropSource> Sources => _sources.Span;
        public ReadOnlySpan<PRecipe> Recipes => _recipes.Span;

        public PMaterialFlags Flags => _flags;

        protected string _id;
        protected string _name;
        protected string _description;

        protected PRarity _rarity;
        protected int _priority;
        protected PMaterialFlags _flags;
        protected int _value;
        protected RefList<DropSource> _sources = new RefList<DropSource>(8);
        protected RefList<PRecipe> _recipes = new RefList<PRecipe>(1);
        protected ProjectionIndex _index;

        public abstract bool Load();
        public abstract void Unload();

        public bool HasValidRecipe()
        {
            for (int i = 0; i < _recipes.Count; i++)
            {
                if (_recipes[i].IsValid) { return true; }
            }
            return false;
        }
        public bool TryGetValidRecipe(out PRecipe pRecipe, int index = 0)
        {
            pRecipe = null;
            if (_recipes.Count < 1) { return false; }

            index = index < 0 || index >= _recipes.Count ? 0 : index;
            int og = index;

            while (true)
            {
                if (_recipes[index].IsValid)
                {
                    pRecipe = _recipes[index];
                    return true;
                }

                index++;
                index %= _recipes.Count;
                if (index == og) { return false; }
            }
        }
    }
}