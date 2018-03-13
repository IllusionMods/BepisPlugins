using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceRedirector
{
    public static class ReflectionHelper
    {
        public static T GetValue<T>(this FieldInfo info, object instance)
        {
            return (T)info.GetValue(instance);
        }
    }
}
