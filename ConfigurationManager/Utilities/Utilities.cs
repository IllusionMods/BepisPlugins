// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ConfigurationManager.Utilities
{
    public static class Utils
    {
        /// <summary>
        ///     Return items with browsable attribute same as expectedBrowsable, and optionally items with no browsable attribute
        /// </summary>
        public static IEnumerable<T> FilterBrowsable<T>(this IEnumerable<T> props, bool expectedBrowsable,
            bool includeNotSet = false) where T : MemberInfo
        {
            if (includeNotSet)
                return props.Where(p =>
                    p.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                        .All(x => x.Browsable == expectedBrowsable));
            return props.Where(p =>
                p.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .Any(x => x.Browsable != expectedBrowsable));
        }

        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static void SetGameCanvasInputsEnabled(bool mouseInputEnabled)
        {
            foreach (var c in Object.FindObjectsOfType<GraphicRaycaster>())
                c.enabled = mouseInputEnabled;
        }

        public static BaseUnityPlugin[] FindPlugins()
        {
            return Object.FindObjectsOfType<BaseUnityPlugin>();
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }
    }
}