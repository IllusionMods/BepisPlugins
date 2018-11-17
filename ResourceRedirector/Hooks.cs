using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResourceRedirector
{
    static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.resourceredirector");
            harmony.PatchAll(typeof(Hooks));
        }

        #region List Loading
        [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.CheckItemID), new[] { typeof(int), typeof(int) })]
        public static bool CheckItemIDHook(int category, int id, ref byte __result, ChaListControl __instance)
        {
            int pid = ListLoader.CalculateGlobalID(category, id);

            byte result = __instance.CheckItemID(pid);

            if (result > 0)
            {
                //BepInLogger.Log($"CHECK {category} : {id} : {result}");
                __result = result;
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.AddItemID), new[] { typeof(int), typeof(int), typeof(byte) })]
        public static bool AddItemIDHook(int category, int id, byte flags, ChaListControl __instance)
        {
            int pid = ListLoader.CalculateGlobalID(category, id);

            byte result = __instance.CheckItemID(pid);

            if (result > 0)
            {
                //BepInLogger.Log($"ADD {category} : {id} : {result}");
                __instance.AddItemID(pid, flags);
                return false;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), "LoadListInfoAll")]
        public static void LoadListInfoAllPostHook(ChaListControl __instance)
        {
            string listPath = Path.Combine(ResourceRedirector.EmulatedDir, @"list\characustom");

            //BepInLogger.Log($"List directory exists? {Directory.Exists(listPath).ToString()}");

            if (Directory.Exists(listPath))
                foreach (string csvPath in Directory.GetFiles(listPath, "*.csv", SearchOption.AllDirectories))
                {
                    //BepInLogger.Log($"Attempting load of: {csvPath}");

                    var chaListData = ListLoader.LoadCSV(File.OpenRead(csvPath));
                    ListLoader.ExternalDataList.Add(chaListData);

                    //BepInLogger.Log($"Finished load of: {csvPath}");
                }

            ListLoader.LoadAllLists(__instance);
        }
        #endregion

        #region Asset Loading
        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static bool LoadAssetPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result == null)
            {
                if (!File.Exists($"{Application.dataPath}/../abdata/{assetBundleName}"))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    Logger.Log(LogLevel.Warning, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }
            else
                return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), "LoadAssetAsync", new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static bool LoadAssetAsyncPreHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result == null)
            {
                if (!File.Exists($"{Application.dataPath}/../abdata/{assetBundleName}"))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    Logger.Log(LogLevel.Warning, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }
            else
                return false;
        }
        #endregion
    }
}