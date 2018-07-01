using System.Reflection;

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
