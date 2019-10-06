using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Sideloader.AutoResolver
{
    internal class StructValue<TValue>
    {
        internal SetterDelegate SetMethod { get; set; }
        internal GetterDelegate GetMethod { get; set; }
        internal Type StructType { get; set; }

        internal StructValue(PropertyInfo info)
        {
            var getter = CreateGetter(info);
            var setter = CreateSetter(info);

            SetMethod = setter;
            GetMethod = getter;
        }

        internal StructValue(FieldInfo info)
        {
            SetMethod = (obj, value) => info.SetValue(obj, value);
            GetMethod = (obj) => (TValue)info.GetValue(obj);
        }

        internal StructValue(SetterDelegate setMethod, GetterDelegate getMethod)
        {
            SetMethod = setMethod;
            GetMethod = getMethod;
        }

        #region Dynamics

        internal delegate TValue GetterDelegate(object obj);

        internal delegate void SetterDelegate(object obj, TValue value);

        private static GetterDelegate CreateGetter(PropertyInfo property)
        {
            DynamicMethod method = new DynamicMethod($"{property} Getter", typeof(TValue), new[] { typeof(object) }, true);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, property.GetGetMethod(true));
            il.Emit(OpCodes.Ret);

            return (GetterDelegate)method.CreateDelegate(typeof(GetterDelegate));
        }

        private static SetterDelegate CreateSetter(PropertyInfo property)
        {
            DynamicMethod method = new DynamicMethod($"{property} Setter", typeof(void), new[] { typeof(object), typeof(TValue) }, true);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, property.GetSetMethod(true));
            il.Emit(OpCodes.Ret);

            return (SetterDelegate)method.CreateDelegate(typeof(SetterDelegate));
        }

        #endregion
    }
}