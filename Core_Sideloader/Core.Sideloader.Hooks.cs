using BepInEx.Harmony;
using HarmonyLib;
using UnityEngine;

namespace Sideloader
{
    public static partial class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
#if KK
            harmony.Patch(typeof(GlobalMethod).GetMethod(nameof(GlobalMethod.LoadAllFolder), AccessTools.all).MakeGenericMethod(typeof(Object)),
                          null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(LoadAllFolderPostfix), AccessTools.all)));
#endif
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
    }
}
