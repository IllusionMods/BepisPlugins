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
        private static Dictionary<string, Lazy<AssetBundle>> bundles = new Dictionary<string, Lazy<AssetBundle>>();

        private static Dictionary<string, string> bundleIDs = new Dictionary<string, string>();

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

        public static string CreateUniquePath(string uid, string filename)
        {
            return $"{uid}-{filename}";
        }

        public static string CreateAndAddUID(Func<AssetBundle> func, string id = null)
        {
            if (id == null)
                id = GenerateCAB().Substring(4);

            bundles.Add(id, Lazy<AssetBundle>.Create(func));

            return id;
        }

        public static bool TryGetObjectFromName(string name, out UnityEngine.Object obj)
        {
            obj = null;

            if (name.Count(x => x == '-') < 1)
                return false;

            string bundleID = name.Remove(name.IndexOf('-'));
            string itemName = name.Remove(0, name.IndexOf('-') + 1);

            if (bundles.TryGetValue(bundleID, out Lazy<AssetBundle> lazyBundle))
            {
                AssetBundle bundle = (AssetBundle)lazyBundle;

                if (bundle.Contains(itemName))
                {
                    obj = bundle.LoadAsset(itemName);
                    return true;
                }
            }

            return false;
        }
    }
}
