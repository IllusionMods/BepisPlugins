using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

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

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Info), "LoadExcelData")]
        public static void LoadExcelDataPostfix(string _bundlePath, string _fileName, ref ExcelData __result)
        {
            var studioList = ResourceRedirector.ListLoader.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath && x.FileNameWithoutExtension == _fileName).ToList();

            if (studioList.Count > 0)
            {
                bool didHeader = false;
                int HeaderRows = studioList[0].Headers.Count;

                if (__result == null) //Create a new ExcelData
                    __result = (ExcelData)ScriptableObject.CreateInstance(typeof(ExcelData));
                else //Adding to an existing ExcelData
                    didHeader = true;

                foreach (var studioListData in studioList)
                {
                    if (!didHeader) //Write the headers. I think it's pointless and will be skipped when the ExcelData is read, but it's expected to be there.
                    {
                        foreach (var header in studioListData.Headers)
                        {
                            var headerParam = new ExcelData.Param();
                            headerParam.list = header;
                            __result.list.Add(headerParam);
                        }
                        didHeader = true;
                    }
                    foreach (var entry in studioListData.Entries)
                    {
                        var param = new ExcelData.Param();
                        param.list = entry;
                        __result.list.Add(param);
                    }
                }

                //Once the game code hits a blank row it skips everything after, all blank rows must be removed for sideloader data to display.
                for (int i = 0; i < __result.list.Count;)
                {
                    if (i <= HeaderRows - 1)
                        i += 1; //Skip header rows
                    else if (__result.list[i].list.Count == 0)
                        __result.list.RemoveAt(i); //Null data row
                    else if (!int.TryParse(__result.list[i].list[0], out int x))
                        __result.list.RemoveAt(i); //Remove anything that isn't a number, most likely a blank row
                    else
                        i += 1;
                }
            }
        }

        public static bool MapLoading = false;
        public static string MapABName = "";

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.Map), nameof(Studio.Map.LoadMapCoroutine))]
        public static void LoadMapCoroutinePrefix() => MapLoading = true;
        [HarmonyPrefix, HarmonyPatch(typeof(Manager.Scene), nameof(Manager.Scene.LoadBaseScene))]
        public static void LoadBaseScenePrefix(Manager.Scene.Data data) => MapABName = data.assetBundleName;
    }
}
