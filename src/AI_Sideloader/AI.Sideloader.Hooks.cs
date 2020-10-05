using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Sideloader.ListLoader;
using UnityEngine;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.GetAllAssetName))]
            internal static void GetAllAssetName(ref string[] __result, string assetBundleName, bool _WithExtension = true, string manifestAssetBundleName = null,
                bool isAllCheck = false)
            {
                if (Lists.ExternalExcelData.TryGetValue(assetBundleName, out var infos))
                    __result = __result.Concat(infos.Select(x => x.Key.ToLower())).ToArray();
            }
        }
    }
}