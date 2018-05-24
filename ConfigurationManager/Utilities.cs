using System;

namespace ConfigurationManager
{
    static class Utilities
    {
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static void SetGameCanvasInputsEnabled(bool mouseInputEnabled)
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>())
            {
                c.enabled = mouseInputEnabled;
            }
        }

        public static BepInEx.BaseUnityPlugin[] FindPlugins()
        {
            return UnityEngine.Object.FindObjectsOfType<BepInEx.BaseUnityPlugin>();
        }
    }
}
