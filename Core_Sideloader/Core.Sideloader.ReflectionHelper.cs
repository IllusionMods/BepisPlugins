using System.Reflection;

namespace Sideloader
{
    public static class ReflectionHelper
    {
        public static T GetValue<T>(this FieldInfo info, object instance) => (T)info.GetValue(instance);
    }
}
