using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;

namespace Sideloader.AutoResolver
{
    public class StructValue<TValue>
    {
        //could potentially get faster by bypassing the generic actions but it's probably too much effort for too little gain, reflection is already a big impact
        //public PropertyInfo PropertyInfo { get; protected set; }
        //public FieldInfo FieldInfo { get; protected set; }
        
        public Action<object, TValue> SetMethod { get; protected set; }
        public Func<object, TValue> GetMethod { get; protected set; }

        public Type StructType { get; protected set; }

        public StructValue(PropertyInfo info)
        {
            SetMethod = (obj, value) => info.SetValue(obj, value, null);
            GetMethod = (obj) => (TValue)info.GetValue(obj, null);
        }

        public StructValue(FieldInfo info)
        {
            SetMethod = (obj, value) => info.SetValue(obj, value);
            GetMethod = (obj) => (TValue)info.GetValue(obj);
        }

        public StructValue(Action<object, TValue> setMethod, Func<object, TValue> getMethod)
        {
            SetMethod = setMethod;
            GetMethod = getMethod;
        }
    }
}
