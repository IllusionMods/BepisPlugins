using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Sideloader
{
    public static class BundleManager
    {
        public static Dictionary<string, List<LazyCustom<AssetBundle>>> Bundles = new Dictionary<string, List<LazyCustom<AssetBundle>>>();

        public static string DummyPath => "list/characustom/00.unity3d";

        private static long CABCounter;

        // Only ASCII chars or we'll explode
        public static string GenerateCAB() => "CAB-" + Interlocked.Increment(ref CABCounter).ToString("x32");

        public static void RandomizeCAB(byte[] assetBundleData)
        {
            var ascii = Encoding.ASCII.GetString(assetBundleData, 0, Mathf.Min(1024, assetBundleData.Length - 4));

            var origCabIndex = ascii.IndexOf("CAB-", StringComparison.Ordinal);

            if (origCabIndex < 0)
                return;

            var origCabLength = ascii.Substring(origCabIndex).IndexOf('\0');

            if (origCabLength < 0)
                return;

            var CAB = GenerateCAB().Substring(4);
            var cabBytes = Encoding.ASCII.GetBytes(CAB);

            if (origCabLength > 36)
                return;

            Buffer.BlockCopy(cabBytes, 36 - origCabLength, assetBundleData, origCabIndex + 4, origCabLength - 4);
        }

        public static void AddBundleLoader(Func<AssetBundle> func, string path, out string warning)
        {
            warning = "";

            if (Bundles.TryGetValue(path, out var lazyList))
            {
                warning = $"Duplicate asset bundle detected! {path}";
                lazyList.Add(LazyCustom<AssetBundle>.Create(func));
            }
            else
                Bundles.Add(path, new List<LazyCustom<AssetBundle>>
                {
                    LazyCustom<AssetBundle>.Create(func)
                });
        }

        public static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            var result = TryGetObjectFromName(name, assetBundle, typeof(T), out var tObj);

            obj = (T)tObj;

            return result;
        }

        public static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out var lazyBundleList))
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
