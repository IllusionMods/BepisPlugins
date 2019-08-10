#if EC
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class Extensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component => gameObject == null ? null : gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

    public static T DeepCopy<T>(this T self)
    {
        if (self == null)
            return default;

        using (var memoryStream = new MemoryStream())
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, self);
            memoryStream.Position = 0L;
            return (T)binaryFormatter.Deserialize(memoryStream);
        }
    }

    public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
}
#endif