using System.Reflection;

namespace Sideloader
{
    internal static class ReflectionHelper
    {
        internal static T GetValue<T>(this FieldInfo info, object instance) => (T)info.GetValue(instance);
    }
}
