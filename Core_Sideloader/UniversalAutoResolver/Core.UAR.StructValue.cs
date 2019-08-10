using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Sideloader.AutoResolver
{
    public class StructValue<TValue>
    {
        public SetterDelegate SetMethod { get; protected set; }
        public GetterDelegate GetMethod { get; protected set; }
        public Type StructType { get; protected set; }

        public StructValue(PropertyInfo info)
        {
            var getter = CreateGetter(info);
            var setter = CreateSetter(info);

            SetMethod = setter;
            GetMethod = getter;
        }

        public StructValue(FieldInfo info)
        {
            SetMethod = (obj, value) => info.SetValue(obj, value);
            GetMethod = (obj) => (TValue)info.GetValue(obj);
        }

        public StructValue(SetterDelegate setMethod, GetterDelegate getMethod)
        {
            SetMethod = setMethod;
            GetMethod = getMethod;
        }

        #region Dynamics

        public delegate TValue GetterDelegate(object obj);

        public delegate void SetterDelegate(object obj, TValue value);

        protected static GetterDelegate CreateGetter(PropertyInfo property)
        {
            DynamicMethod method = new DynamicMethod($"{property} Getter", typeof(TValue), new[] { typeof(object) }, true);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, property.GetGetMethod(true));
            il.Emit(OpCodes.Ret);

            return (GetterDelegate)method.CreateDelegate(typeof(GetterDelegate));
        }

        protected static SetterDelegate CreateSetter(PropertyInfo property)
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