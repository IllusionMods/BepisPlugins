using HarmonyLib;
using Sideloader.ListLoader;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Studio.Info), "LoadExcelData")]
            private static void LoadExcelDataPostfix(string _bundlePath, string _fileName, ref ExcelData __result)
            {
                var studioList = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath && x.FileNameWithoutExtension == _fileName).ToList();

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
                                var headerParam = new ExcelData.Param { list = header };
                                __result.list.Add(headerParam);
                            }
                            didHeader = true;
                        }
                        foreach (var entry in studioListData.Entries)
                        {
                            var param = new ExcelData.Param { list = entry };
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

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.AssetBundleCheck), nameof(Studio.AssetBundleCheck.GetAllFileName))]
            private static bool GetAllFileName(string _assetBundleName, ref string[] __result)
            {
                var list = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _assetBundleName).Select(y => y.FileNameWithoutExtension.ToLower()).ToArray();
                if (list.Count() > 0)
                {
                    __result = list;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Info), "FindAllAssetName")]
            private static bool FindAllAssetNamePrefix(string _bundlePath, string _regex, ref string[] __result)
            {
                var list = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath).Select(x => x.FileNameWithoutExtension).ToList();
                if (list.Count() > 0)
                {
                    __result = list.Where(x => Regex.Match(x, _regex, RegexOptions.IgnoreCase).Success).ToArray();
                    return false;
                }
                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CommonLib), nameof(CommonLib.GetAssetBundleNameListFromPath))]
            private static void GetAssetBundleNameListFromPathStudio(string path, List<string> __result)
            {
                if (path == "studio/info/")
                {
                    foreach (string assetBundleName in Lists.ExternalStudioDataList.Select(x => x.AssetBundleName).Distinct())
                        if (!__result.Contains(assetBundleName))
                            __result.Add(assetBundleName);
                }
            }

            // Override the FileCheck to search asset bundles in zipmods as well
            internal static void FileCheck(string _path, ref bool __result, Dictionary<string, bool> ___dicConfirmed)
            {
                if (__result == false)
                {
                    if (BundleManager.Bundles.TryGetValue(_path, out _))
                    {
                        __result = true;
                        ___dicConfirmed[_path] = true;
                    }
                }
            }
        }
    }
}