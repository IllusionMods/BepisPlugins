using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Sideloader
{
    public static class BundleManager
    {
        public static Dictionary<string, List<Lazy<AssetBundle>>> Bundles = new Dictionary<string, List<Lazy<AssetBundle>>>();

        public static string DummyPath => "list/characustom/00.unity3d";

        private static long CABCounter;

        public static string GenerateCAB()
        {
            // Only ASCII chars or we'll explode
            return "CAB-" + Interlocked.Increment(ref CABCounter).ToString("x32");
        }

        public static void RandomizeCAB(byte[] assetBundleData)
        {
            var startIndex = -1;
            var endIndex = -1;

            var searchLength = Mathf.Min(1024, assetBundleData.Length - 4);
            for (var i = 0; i < searchLength; i++)
            {
                if (startIndex < 0)
                {
                    if (assetBundleData[i + 0] == 'C' &&
                        assetBundleData[i + 1] == 'A' &&
                        assetBundleData[i + 2] == 'B' &&
                        assetBundleData[i + 3] == '-')
                    {
                        startIndex = i;
                        i += 3;
                    }
                }
                else
                {
                    if (assetBundleData[i] == '\0')
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            if (startIndex < 0 || endIndex < 0)
                return;

            var newCab = GenerateCAB().Select(Convert.ToByte).ToArray();

            if (endIndex - startIndex < newCab.Length)
                return;

            Buffer.BlockCopy(newCab, 0, assetBundleData, startIndex, newCab.Length);
        }

        public static void AddBundleLoader(Func<AssetBundle> func, string path, out string warning)
        {
            warning = "";

            if (Bundles.TryGetValue(path, out var lazyList))
            {
                warning = $"Duplicate asset bundle detected! {path}";
                lazyList.Add(Lazy<AssetBundle>.Create(func));
            }
            else
                Bundles.Add(path, new List<Lazy<AssetBundle>>
                {
                    Lazy<AssetBundle>.Create(func)
                });
        }

        public static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            bool result = TryGetObjectFromName(name, assetBundle, typeof(T), out UnityEngine.Object tObj);

            obj = (T)tObj;

            return result;
        }

        public static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out List<Lazy<AssetBundle>> lazyBundleList))
            {
                foreach (AssetBundle bundle in lazyBundleList)
                {
                    if (bundle.Contains(name))
                    {
                        obj = bundle.LoadAsset(name, type);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
