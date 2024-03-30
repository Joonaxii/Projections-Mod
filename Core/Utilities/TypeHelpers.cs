using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Projections.Core.Utilities
{
    public static class TypeHelpers
    {
        public delegate string StringFormat<T>(ref T value);

        public static StringFormat<float> FloatFormatter { get; } = (ref float value) =>
        {
            return value.ToString("F4");
        };
        public static StringFormat<double> DoubleFormatter { get; } = (ref double value) =>
        {
            return value.ToString("F4");
        };

        public static bool TryParse<T>(this string input, out T valueOut) where T : unmanaged
        => GenericOps<T>.TryParse(input, out valueOut);
    }

    public static class GenericOps<T>
    {
        private delegate bool ParseOut(string input, ref T outValue);

        private static ParseOut _cachedTryParse;
        private static Func<T, T, T> _cachedAdd;
        private static Func<T, T, T> _cachedSub;
        private static MethodInfo _tryParse;

        static GenericOps()
        {
            _tryParse = typeof(T).GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string), typeof(T).MakeByRefType() });
        }

        public static T Add(T lhs, T rhs)
        {
            if (_cachedAdd == null)
            {
                var exp1 = Expression.Parameter(typeof(T), "lhs");
                var exp2 = Expression.Parameter(typeof(T), "rhs");
                _cachedAdd = Expression.Lambda<Func<T, T, T>>(Expression.Add(exp1, exp2), exp1, exp2).Compile();
            }
            return _cachedAdd.Invoke(lhs, rhs);
        }
        public static T Sub(T lhs, T rhs)
        {
            if (_cachedSub == null)
            {
                var exp1 = Expression.Parameter(typeof(T), "lhs");
                var exp2 = Expression.Parameter(typeof(T), "rhs");
                _cachedSub = Expression.Lambda<Func<T, T, T>>(Expression.Subtract(exp1, exp2), exp1, exp2).Compile();
            }
            return _cachedSub.Invoke(lhs, rhs);
        }

        public static bool TryParse(string input, out T outVal)
        {
            if (_cachedTryParse == null)
            {
                var exp1 = Expression.Parameter(typeof(string), "input");
                var exp2 = Expression.Parameter(typeof(T).MakeByRefType(), "outParam");
                var exp3 = Expression.Parameter(typeof(bool));
                var invoke = Expression.Call(_tryParse, exp1, exp2);
                _cachedTryParse = Expression.Lambda<ParseOut>(invoke, exp1, exp2).Compile();
            }
            outVal = default;
            return _cachedTryParse.Invoke(input, ref outVal);
        }
    }
}
