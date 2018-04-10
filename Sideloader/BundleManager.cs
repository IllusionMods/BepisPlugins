using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sideloader
{
    public static class BundleManager
    {
        public static Dictionary<string, Lazy<AssetBundle>> Bundles = new Dictionary<string, Lazy<AssetBundle>>();

        private static System.Random random = new System.Random();

        public static string DummyPath => "list/characustom/00.unity3d";

        public static string GenerateCAB()
        {
            const string validCharacters = "0123456789abcdef";

            StringBuilder sb = new StringBuilder("CAB-", 36);

            for (int i = 0; i < 32; i++)
                sb.Append(validCharacters[random.Next(0, 15)]);

            return sb.ToString();
        }

        public static void AddBundleLoader(Func<AssetBundle> func, string path)
        {
            Bundles.Add(path, Lazy<AssetBundle>.Create(func));
        }

        public static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            bool result =  TryGetObjectFromName(name, assetBundle, typeof(T), out UnityEngine.Object tObj);

            obj = (T)tObj;

            return result;
        }

        public static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out Lazy<AssetBundle> lazyBundle))
            {
                AssetBundle bundle = (AssetBundle)lazyBundle;

                if (bundle.Contains(name))
                {
                    obj = bundle.LoadAsset(name, type);
                    return true;
                }
            }

            return false;
        }
    }
}
