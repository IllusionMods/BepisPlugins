using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Sideloader
{
    internal static class BundleManager
    {
        internal static Dictionary<string, List<LazyCustom<AssetBundle>>> Bundles = new Dictionary<string, List<LazyCustom<AssetBundle>>>();

        private static long CABCounter;

        // Only ASCII chars or we'll explode
        internal static string GenerateCAB() => "CAB-" + Interlocked.Increment(ref CABCounter).ToString("x32");

        internal static void RandomizeCAB(byte[] assetBundleData)
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

        internal static void AddBundleLoader(Func<AssetBundle> func, string path)
        {
            if (!Bundles.TryGetValue(path, out var lazyList))
            {
                lazyList = new List<LazyCustom<AssetBundle>>();
                Bundles.Add(path, lazyList);
            }
            lazyList.Add(LazyCustom<AssetBundle>.Create(func));
        }

        internal static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            var result = TryGetObjectFromName(name, assetBundle, typeof(T), out var tObj);

            obj = (T)tObj;

            return result;
        }

        internal static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out var lazyBundleList))
            {
                var found = -1;
                for (int i = 0; i < lazyBundleList.Count; i++)
                {
                    AssetBundle bundle = lazyBundleList[i];
                    if (bundle.Contains(name))
                    {
                        // If using debug logging, check all override bundles for this asset and warn if multiple copies exist.
                        // This will force all override bundles to load so it's slower.
                        if (Sideloader.DebugLoggingModLoading.Value)
                        {
                            if (found >= 0)
                            {
                                Sideloader.Logger.LogWarning($"Asset [{name}] in bundle [{assetBundle}] is overriden by multiple zipmods! " +
                                                             $"Only asset from override #{found + 1} will be used! It also exists in override #{i + 1}.");
                            }
                            else
                            {
                                found = i;
                                obj = bundle.LoadAsset(name, type);
                            }
                            continue;
                        }

                        obj = bundle.LoadAsset(name, type);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
