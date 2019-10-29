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
            [HarmonyPostfix, HarmonyPatch(typeof(BaseMap), "LoadMapInfo")]
            internal static void LoadMapInfo(BaseMap __instance)
            {
                foreach (var mapInfo in Lists.ExternalMapList)
                    foreach (var param in mapInfo.param)
                        __instance.infoDic[param.No] = param;
            }

            /// <summary>
            /// Patch for loading h/common/ stuff for Sideloader maps
            /// </summary>
            internal static void LoadAllFolderPostfix(string _findFolder, string _strLoadFile, ref List<Object> __result)
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
}