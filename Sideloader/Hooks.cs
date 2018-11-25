using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using Harmony;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using Shared;
using UnityEngine;

namespace Sideloader
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader");
            harmony.PatchAll(typeof(Hooks));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
        public static void IsFileHook(string assetBundleName, ref bool __result)
        {
            if (!__result)
            {
                if (BundleManager.Bundles.ContainsKey(assetBundleName))
                    __result = true;
                if (Sideloader.IsPngFolderOnly(assetBundleName))
                    __result = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundleData))]
        [HarmonyPatch(nameof(AssetBundleData.isFile), PropertyMethod.Getter)]
        public static void IsFileHook2(ref bool __result, AssetBundleData __instance)
        {
            if (!__result)
            {
                if (BundleManager.Bundles.ContainsKey(__instance.bundle))
                    __result = true;
                if (Sideloader.IsPngFolderOnly(__instance.bundle))
                    __result = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.ManifestAdd))]
        public static void ManifestAdd(string manifestAssetBundleName)
        {
            //Load all sideloader ABMs after abdata. ManifestAdd prevents duplicates and order doesn't matter.
            if (manifestAssetBundleName == "abdata")
                foreach (var x in BundleManager.Bundles.Where(x => !x.Key.Contains(".") && !x.Key.Contains("/")))
                    AssetBundleManager.ManifestAdd(x.Key);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetBundleInternal))]
        public static bool LoadAssetBundleInternalPrefix(string assetBundleName, bool isAsync, string manifestAssetBundleName, bool __result)
        {
            //Don't load anything but ABMs here or they will get unloaded. ResourceRedirector LoadAsset hook will handle it everything else.
            //It's probably fine if ABMs are unloaded since they are only used once. Probably.
            if (assetBundleName.Contains(".") || manifestAssetBundleName.IsNullOrEmpty())
                return true;

            if (BundleManager.Bundles.TryGetValue(assetBundleName, out List<Lazy<AssetBundle>> lazyList))
            {
                var assetBundle = lazyList[0];

                var m_ManifestBundlePack = Traverse.Create(Singleton<AssetBundleManager>.Instance).Field("m_ManifestBundlePack").GetValue<Dictionary<string, AssetBundleManager.BundlePack>>();
                var bundlePack = m_ManifestBundlePack[manifestAssetBundleName];
                var m_AllLoadedAssetBundleNames = Traverse.Create(Singleton<AssetBundleManager>.Instance).Field("m_AllLoadedAssetBundleNames").GetValue<HashSet<string>>();

                LoadedAssetBundle loadedAssetBundle = null;
                bundlePack.LoadedAssetBundles.TryGetValue(assetBundleName, out loadedAssetBundle);
                if (loadedAssetBundle != null)
                {
                    loadedAssetBundle.m_ReferencedCount += 1u;
                    __result = true;
                    return false;
                }
                AssetBundleCreate assetBundleCreate = null;
                bundlePack.CreateAssetBundles.TryGetValue(assetBundleName, out assetBundleCreate);
                if (assetBundleCreate != null)
                {
                    assetBundleCreate.m_ReferencedCount += 1u;
                    __result = true;
                    return false;
                }

                if (!m_AllLoadedAssetBundleNames.Add(assetBundleName))
                {
                    __result = true;
                    return false;
                }

                bundlePack.LoadedAssetBundles.Add(assetBundleName, new LoadedAssetBundle(assetBundle.Instance));
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Info), "LoadExcelData")]
        public static void LoadExcelDataPostfix(string _bundlePath, string _fileName, ref ExcelData __result)
        {
            var studioList = ResourceRedirector.ListLoader.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath && x.FileNameWithoutExtension == _fileName).ToList();

            if (studioList.Count() > 0)
            {
                bool didHeader = false;
                if (__result == null) //Create a new ExcelData
                    __result = (ExcelData)ScriptableObject.CreateInstance(typeof(ExcelData));
                else //Adding to an existing ExcelData
                    didHeader = true;

                foreach (var studioListData in studioList)
                {
                    if (!didHeader) //Write the header. I think it's pointless and will be skipped when the ExcelData is read, but it's expected to be there.
                    {
                        var param = new ExcelData.Param();
                        param.list = studioListData.Header;
                        __result.list.Add(param);
                        didHeader = true;
                    }
                    foreach (var entry in studioListData.Entries)
                    {
                        var param = new ExcelData.Param();
                        param.list = entry;
                        __result.list.Add(param);
                    }
                }
            }
        }
    }
}
