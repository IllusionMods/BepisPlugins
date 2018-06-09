// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace ConfigurationManager.Utilities
{
    public static class Utils
    {
        /// <summary>
        /// Return items with browsable attribute same as expectedBrowsable, and optionally items with no browsable attribute
        /// </summary>
        public static IEnumerable<T> FilterBrowsable<T>(this T[] props, bool expectedBrowsable, bool includeNotSet = false) where T : MemberInfo
        {
            if (includeNotSet)
                return props.Where(p => p.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>().All(x => x.Browsable == expectedBrowsable));
            else
                return props.Where(p => p.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>().Any(x => x.Browsable != expectedBrowsable));
        }

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
