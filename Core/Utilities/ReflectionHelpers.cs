using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;


namespace Projections.Core.Utilities
{
    internal class ReflectableProperty<T, U>
    {
        private readonly PropertyInfo _property;
        private readonly Func<T, U> _getter;
        private readonly Action<T, U> _setter;

        public ReflectableProperty(string property, BindingFlags flags)
        {
            _property = typeof(T).GetProperty(property, flags);
            _getter = CreateGetter(_property);
            _setter = CreateSetter(_property);
        }

        public void Set(T type, U fieldVal) => _setter.Invoke(type, fieldVal);
        public U Get(T type) => _getter.Invoke(type);

        internal static Action<T, U> CreateSetter(PropertyInfo propertyInfo)
        {
            ParameterExpression instance = Expression.Parameter(typeof(T), "instance");
            ParameterExpression parameter = Expression.Parameter(typeof(U), "param");

            var body = Expression.Call(instance, propertyInfo.GetSetMethod(), parameter);
            var parameters = new ParameterExpression[] { instance, parameter };

            return Expression.Lambda<Action<T, U>>(body, parameters).Compile();
        }
        internal static Func<T, U> CreateGetter(PropertyInfo propertyInfo)
        {
            ParameterExpression instance = Expression.Parameter(typeof(T), "instance");

            var body = Expression.Call(instance, propertyInfo.GetGetMethod());
            var parameters = new ParameterExpression[] { instance };

            return Expression.Lambda<Func<T, U>>(body, parameters).Compile();
        }
    }

    internal class ReflectableField<T, U>
    {
        private readonly FieldInfo _field;
        private readonly Func<T, U> _getter;
        private readonly Action<T, U> _setter;

        public ReflectableField(string field, BindingFlags flags)
        {
            _field = typeof(T).GetField(field, flags);
            _getter = CreateGetter(_field);
            _setter = CreateSetter(_field);
        }

        public void Set(T type, U fieldVal) => _setter.Invoke(type, fieldVal);
        public U Get(T type) => _getter.Invoke(type);

        internal static Func<T, U> CreateGetter(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(U), new Type[1] { typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<T, U>)setterMethod.CreateDelegate(typeof(Func<T, U>));
        }

        internal static Action<T, U> CreateSetter(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(T), typeof(U) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<T, U>)setterMethod.CreateDelegate(typeof(Action<T, U>));
        }
    }
}
