using System;
using System.Collections.Generic;
using Terraria;

namespace Projections.Core.Utilities
{
    public static class RNG
    {
        public static float GetTotalWeight<T>(this ReadOnlySpan<T> values) where T : IWeighted
        {
            float w = 0;
            for (int i = 0; i < values.Length; i++)
            {
                w += values[i].Weight;
            }
            return w;
        }

        public static T SelectRandom<T>(this ReadOnlySpan<WeightedType<T>> values, T defaultValue) => values.SelectRandom(values.Length, values.GetTotalWeight(), defaultValue);
        public static T SelectRandom<T>(this ReadOnlySpan<WeightedType<T>> values, int count, T defaultValue) => values.SelectRandom(count, values.GetTotalWeight(), defaultValue);

        public static T SelectRandom<T>(this ReadOnlySpan<WeightedType<T>> values, float totalWeight, T defaultValue) => values.SelectRandom(values.Length, totalWeight, defaultValue);
        public static T SelectRandom<T>(this ReadOnlySpan<WeightedType<T>> values, int count, float totalWeight, T defaultValue)
        {
            var ind = values.SelectRandomIndex(count, totalWeight);
            return ind < 0 ? defaultValue : values[ind].value;
        }

        public static T SelectRandom<T>(this ReadOnlySpan<T> values) where T : IWeighted => values.SelectRandom(values.Length, values.GetTotalWeight());
        public static T SelectRandom<T>(this ReadOnlySpan<T> values, int count) where T : IWeighted => values.SelectRandom(count, values.GetTotalWeight());

        public static T SelectRandom<T>(this ReadOnlySpan<T> values, float totalWeight) where T : IWeighted => values.SelectRandom(values.Length, totalWeight);
        public static T SelectRandom<T>(this ReadOnlySpan<T> values, int count, float totalWeight) where T : IWeighted
        {
            if (count < 1) { return default; }
            var ind = values.SelectRandomIndex(count, totalWeight);
            return values[ind];
        }

        public static int SelectRandomIndex<T>(this ReadOnlySpan<T> values) where T : IWeighted => values.SelectRandomIndex(values.Length, values.GetTotalWeight());
        public static int SelectRandomIndex<T>(this ReadOnlySpan<T> values, int count) where T : IWeighted => values.SelectRandomIndex(count, values.GetTotalWeight());

        public static int SelectRandomIndex<T>(this ReadOnlySpan<T> values, float totalWeight) where T : IWeighted => values.SelectRandomIndex(values.Length, totalWeight);
        public static int SelectRandomIndex<T>(this ReadOnlySpan<T> values, int count, float totalWeight) where T : IWeighted
        {
            count = Math.Min(count, values.Length);

            float rnd = Main.rand.NextFloat() * totalWeight;

            float prevAcc;
            float currAcc = 0;
            int selected = 0;
            float select = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                prevAcc = currAcc;
                currAcc += values[i].Weight;

                if (rnd <= currAcc && rnd >= prevAcc)
                {
                    float d = MathF.Abs(currAcc - rnd);
                    if (d < select)
                    {
                        select = d;
                        selected = i;
                    }
                }
            }
            return selected;
        }

        public static float GetTotalWeight<T>(this Span<T> values) where T : IWeighted => values.AsReadOnlySpan().GetTotalWeight();

        public static T SelectRandom<T>(this Span<WeightedType<T>> values, T defaultValue) => values.AsReadOnlySpan().SelectRandom(values.Length, values.GetTotalWeight(), defaultValue);
        public static T SelectRandom<T>(this Span<WeightedType<T>> values, int count, T defaultValue) => values.AsReadOnlySpan().SelectRandom(count, values.GetTotalWeight(), defaultValue);

        public static T SelectRandom<T>(this Span<WeightedType<T>> values, float totalWeight, T defaultValue) => values.AsReadOnlySpan().SelectRandom(values.Length, totalWeight, defaultValue);
        public static T SelectRandom<T>(this Span<WeightedType<T>> values, int count, float totalWeight, T defaultValue) => values.AsReadOnlySpan().SelectRandom(count, totalWeight, defaultValue);

        public static T SelectRandom<T>(this Span<T> values) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(values.Length, values.GetTotalWeight());
        public static T SelectRandom<T>(this Span<T> values, int count) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(count, values.GetTotalWeight());

        public static T SelectRandom<T>(this Span<T> values, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(values.Length, totalWeight);
        public static T SelectRandom<T>(this Span<T> values, int count, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(count, totalWeight);

        public static int SelectRandomIndex<T>(this Span<T> values) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(values.Length, values.GetTotalWeight());
        public static int SelectRandomIndex<T>(this Span<T> values, int count) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(count, values.GetTotalWeight());

        public static int SelectRandomIndex<T>(this Span<T> values, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(values.Length, totalWeight);
        public static int SelectRandomIndex<T>(this Span<T> values, int count, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(count, totalWeight);

        public static float GetTotalWeight<T>(this IList<T> values) where T : IWeighted => values.AsReadOnlySpan().GetTotalWeight();

        public static T SelectRandom<T>(this IList<WeightedType<T>> values, T defaultValue) => values.AsReadOnlySpan().SelectRandom(values.Count, values.GetTotalWeight(), defaultValue);
        public static T SelectRandom<T>(this IList<WeightedType<T>> values, int count, T defaultValue) => values.AsReadOnlySpan().SelectRandom(count, values.GetTotalWeight(), defaultValue);
                                             
        public static T SelectRandom<T>(this IList<WeightedType<T>> values, float totalWeight, T defaultValue) => values.AsReadOnlySpan().SelectRandom(values.Count, totalWeight, defaultValue);
        public static T SelectRandom<T>(this IList<WeightedType<T>> values, int count, float totalWeight, T defaultValue) => values.AsReadOnlySpan().SelectRandom(count, totalWeight, defaultValue);

        public static T SelectRandom<T>(this IList<T> values) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(values.Count, values.GetTotalWeight());
        public static T SelectRandom<T>(this IList<T> values, int count) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(count, values.GetTotalWeight());
                                             
        public static T SelectRandom<T>(this IList<T> values, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(values.Count, totalWeight);
        public static T SelectRandom<T>(this IList<T> values, int count, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandom(count, totalWeight);

        public static int SelectRandomIndex<T>(this IList<T> values) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(values.Count, values.GetTotalWeight());
        public static int SelectRandomIndex<T>(this IList<T> values, int count) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(count, values.GetTotalWeight());
                                                    
        public static int SelectRandomIndex<T>(this IList<T> values, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(values.Count, totalWeight);
        public static int SelectRandomIndex<T>(this IList<T> values, int count, float totalWeight) where T : IWeighted => values.AsReadOnlySpan().SelectRandomIndex(count, totalWeight);
    }
}