using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using Logger = BepInEx.Logger;

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

        #region Studio List Loading 
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
        #endregion

        #region Asset Loading
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
        public static bool LoadAssetAsyncPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result == null)
            {
                if (!ResourceRedirector.AssetBundleExists(assetBundleName))
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

        [HarmonyTranspiler, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetBundleInternal))]
        public static IEnumerable<CodeInstruction> LoadAssetBundleInternalTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            MethodInfo LoadMethod = typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadFromFile), AccessTools.all, null, new[] { typeof(string) }, null);

            int IndexLoadFromFile = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Call && instruction.operand == LoadMethod);

            //Switch out a LoadFromFile call
            if (IndexLoadFromFile > 0)
                instructionsList[IndexLoadFromFile].operand = typeof(ResourceRedirector).GetMethod(nameof(ResourceRedirector.HandleAssetBundle), AccessTools.all);

            return instructions;
        }
    }
}