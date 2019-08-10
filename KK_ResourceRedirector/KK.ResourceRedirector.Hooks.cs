using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResourceRedirector
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static bool LoadAssetPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result == null)
            {
                if (!ResourceRedirector.AssetBundleExists(assetBundleName))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    ResourceRedirector.Logger.Log(LogLevel.Debug, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }
            else
                return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), "LoadAssetAsync", new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static bool LoadAssetAsyncPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result == null)
            {
                if (!ResourceRedirector.AssetBundleExists(assetBundleName))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    ResourceRedirector.Logger.Log(LogLevel.Debug, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BaseMap), "LoadMapInfo")]
        public static void LoadMapInfo(BaseMap __instance)
        {
            foreach (var mapInfo in ListLoader.ExternalMapList)
                foreach (var param in mapInfo.param)
                    __instance.infoDic[param.No] = param;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.AssetBundleCheck), nameof(Studio.AssetBundleCheck.GetAllFileName))]
        public static bool GetAllFileName(string _assetBundleName, bool _WithExtension, ref string[] __result)
        {
            var list = ListLoader.ExternalStudioDataList.Where(x => x.AssetBundleName == _assetBundleName).Select(y => y.FileNameWithoutExtension.ToLower()).ToArray();
            if (list.Count() > 0)
            {
                __result = list;
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.Info), "FindAllAssetName")]
        public static bool FindAllAssetNamePrefix(string _bundlePath, string _regex, ref string[] __result)
        {
            var list = ListLoader.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath).Select(x => x.FileNameWithoutExtension).ToList();
            if (list.Count() > 0)
            {
                __result = list.Where(x => Regex.Match(x, _regex, RegexOptions.IgnoreCase).Success).ToArray();
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CommonLib), nameof(CommonLib.GetAssetBundleNameListFromPath))]
        public static void GetAssetBundleNameListFromPath(string path, List<string> __result)
        {
            if (path == "studio/info/")
            {
                foreach (string assetBundleName in ListLoader.ExternalStudioDataList.Select(x => x.AssetBundleName).Distinct())
                    if (!__result.Contains(assetBundleName))
                        __result.Add(assetBundleName);
            }
        }
    }
}