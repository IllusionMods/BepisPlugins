using BepInEx.Harmony;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sideloader
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
            harmony.Patch(typeof(GlobalMethod).GetMethod(nameof(GlobalMethod.LoadAllFolder), AccessTools.all).MakeGenericMethod(typeof(Object)),
                          null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(LoadAllFolderPostfix), AccessTools.all)));
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
        [HarmonyPatch(nameof(AssetBundleData.isFile), MethodType.Getter)]
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
        /// <summary>
        /// Patch for loading h/common/ stuff for Sideloader maps
        /// </summary>
        public static void LoadAllFolderPostfix(string _findFolder, string _strLoadFile, ref List<Object> __result)
        {
            if (__result.Count() == 0 && _findFolder == "h/common/")
                foreach (var kvp in BundleManager.Bundles.Where(x => x.Key.StartsWith(_findFolder)))
                    foreach (var lazyList in kvp.Value)
                        foreach (var assetName in lazyList.Instance.GetAllAssetNames())
                            if (assetName.ToLower().Contains(_strLoadFile.ToLower()))
                            {
                                GameObject go = CommonLib.LoadAsset<GameObject>(kvp.Key, assetName);
                                if (go)
                                    __result.Add(go);
                            }
        }
    }
}
