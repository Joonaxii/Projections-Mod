using Microsoft.Xna.Framework;
using Projections.Common.Netcode;
using Projections.Core.Data;
using Projections.Core.Data.Structures;
using Projections.Core.Maths;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Projections.Content.Items
{
    public interface IProjectionItemBase
    {
        Item Item { get; }

        PType PType { get; }

        bool IsEmpty { get; }
        bool IsValid { get; }

        ProjectionSource Source { get; }
        ProjectionIndex Index { get; }

        void SetIndex(ProjectionIndex index, ProjectionSource? source = null);
        void Validate();
    }

    public abstract class OverrideShimmerFX : ModItem
    {
        private int _shimmerTime = 0;
        private float _shimmerT = 0;

        private const int SHIMMER_STAR = -120;
        private const int SHIMMER_DURATION = 120;
        private const int SHIMMER_FADE = 90;

        public byte GetShimmerAlpha(Color color)
        {
            return (byte)((1.0f - Utils.Clamp(_shimmerT, 0, 1)) * color.A);
        }

        public override void PostUpdate()
        {
            bool canShimmer = AllowDecraft();
            if (Item.shimmerWet && canShimmer && !Item.shimmered)
            {
                if (_shimmerTime == SHIMMER_DURATION)
                {
                    OnShimmer();
                }
                _shimmerTime++;
                _shimmerT = _shimmerTime < 0 ? 0 : _shimmerTime >= SHIMMER_FADE ? 1.0f : _shimmerTime * (1.0f / SHIMMER_FADE);
                _shimmerT = _shimmerT * _shimmerT * _shimmerT;
            }
            else if (_shimmerTime > 0) { _shimmerTime = SHIMMER_STAR; _shimmerT = 0; }
        }
        protected virtual bool AllowDecraft() => true;
        protected virtual void OnShimmer() { }
    }

    public abstract class ProjectionBase<T> : OverrideShimmerFX, IProjectionItemBase, IComparable<ModItem> where T : ProjectionBase<T>
    {
        public abstract PType PType { get; }

        public bool IsEmpty => _index == ProjectionIndex.Zero;
        public abstract bool IsValid { get; }

        public ProjectionSource Source => _source;
        public ProjectionIndex Index => _index;

        protected ProjectionIndex _index;
        protected ProjectionSource _source;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
        }

        public virtual void Clear()
        {
            _index = ProjectionIndex.Zero;
            _source = default;
        }

        public void SetIndex(ProjectionIndex index, ProjectionSource? source = null)
        {
            if(source != null)
            {
                _source = source.Value;
            }
            _index = index;
            Validate();
        }

        public void Validate()
        {
            DoValidate();
        }

        public int CompareTo(ModItem other) => other is ProjectionBase<T> prj && prj._index == _index ? 0 : -1;

        protected override bool AllowDecraft() => IsValid && Projections.CanShimmer(_index, PType);

        public override void SaveData(TagCompound tag)
        {
            tag.Assign("PIndex", _index);
            _source.Save("PSource", tag);
        }

        public override void LoadData(TagCompound tag)
        {
            _index = tag.GetPIndex("PIndex");
            _source.Load("PSource", tag);
            Validate();
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(_index);
            _source.Serialize(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            reader.Read(ref _index);
            _source.Deserialize(reader);
            Validate();
        }

        public override bool CanStack(Item source)
        {
            return source.ModItem is ProjectionBase<T> mat && mat._index == _index;
        }

        public override void OnStack(Item source, int numToTransfer)
        {
            if(source.ModItem is ProjectionBase<T> proj)
            {
                if(proj._source.Source == ProjectionSourceType.Crafted &&
                    _source.Source != ProjectionSourceType.Crafted)
                {
                    _source = proj.Source;
                }
            }
        }

        protected abstract void DoValidate();

        protected override void OnShimmer()
        {
            int stackSize = Item.stack;
            var pos = Item.position + new Vector2(0, 8);
            Item.ShimmerEffect(Item.position);

            var shimmerEV = (Item itm) =>
            {
                itm.shimmered = true;
                itm.shimmerTime = 1f;
                itm.wet = true;
                itm.shimmerWet = true;
            };

            Projections.SpawnRecipeAsItems(_source, _index, pos, stackSize, PType, shimmerEV);
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Item.active = false;
            }
            else
            {
                int ind = Item.IndexOfItem();
                Item.SetDefaults(0);
                if (ind > -1)
                {
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, ind, 0f, 0f, 0f, 0, 0, 0);
                }
            }
        }

        protected static float GetEssModifier()
        {
            return PMath.Lerp(0.85f, 1.15f, ProjectorSystem.EssScaleSquared);
        }

        protected static float GetEssModifier(float min, float max)
        {
            return PMath.Lerp(min, max, ProjectorSystem.EssScaleSquared);
        }
    }
}
