using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sideloader.ListLoader;
#if KK || EC
using ChaCustom;
#elif AI || HS2
using CharaCustom;
#endif

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            internal static void InstallHooks()
            {
                var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
#if KK
                harmony.Patch(typeof(GlobalMethod).GetMethod(nameof(GlobalMethod.LoadAllFolder), AccessTools.all).MakeGenericMethod(typeof(Object)),
                              null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(LoadAllFolderPostfix), AccessTools.all)));
#endif

#if !EC
                harmony.Patch(typeof(Studio.Info).GetNestedType("FileCheck", AccessTools.all).GetMethod("Check", AccessTools.all), null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(FileCheck), AccessTools.all)));
#endif

#if HS2
                var add50M = AccessTools.Method(typeof(Manager.GameSystem), "IsPathAdd50");
                if (add50M != null)
                    harmony.Patch(add50M, postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.IsPathAdd50Hook)));
#endif
            }

#if AI || HS2
            [HarmonyPostfix, HarmonyPatch(typeof(GlobalMethod), nameof(GlobalMethod.AssetFileExist))]
            private static void AssetFileExist(string path, string targetName, ref bool __result)
            {
                if (TryGetExcelData(path, targetName, out _))
                    __result = true;
            }
#endif

#if HS2
            private static void IsPathAdd50Hook(string _path, ref bool __result)
            {
                if (!__result)
                    __result = IsSideloaderAB(_path);
            }
#endif

            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
            private static void IsFileHook(string assetBundleName, ref bool __result)
            {
                if (!__result)
                    __result = IsSideloaderAB(assetBundleName);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleData), nameof(AssetBundleData.isFile), MethodType.Getter)]
            private static void IsFileHook2(ref bool __result, AssetBundleData __instance)
            {
                if (!__result)
                    __result = IsSideloaderAB(__instance.bundle);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CommonLib), nameof(CommonLib.GetAssetBundleNameListFromPath))]
            private static void GetAssetBundleNameListFromPath(string path, List<string> __result)
            {
                if (path == "h/list/" || path == "map/list/mapinfo/")
                {
                    foreach (var assetBundleName in BundleManager.Bundles.Keys.Where(x => x.StartsWith(path)))
                        if (!__result.Contains(assetBundleName))
                            __result.Add(assetBundleName);
                }
#if AI || HS2
                else if (path == "list/map/")
                {
                    foreach (var assetBundleName in Lists.ExternalExcelData.Keys.Where(x => x.StartsWith(path)))
                        if (!__result.Contains(assetBundleName))
                            __result.Add(assetBundleName);
                }
#endif
#if HS2
                else if (path == "adv/eventcg/")
                {
                    foreach (var assetBundleName in BundleManager.Bundles.Keys.Where(x => x.StartsWith(path)))
                        if (!__result.Contains(assetBundleName))
                            __result.Add(assetBundleName);
                }
#endif
            }

#if KK || EC
            /// <summary>
            /// The game gets from a list by index which will cause errors. Get them safely for sideloader items
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomFacePaintLayoutPreset), nameof(CustomFacePaintLayoutPreset.OnPush))]
            private static bool FacePaintOnPush(int index, CustomFacePaintLayoutPreset __instance)
            {
                if (index >= UniversalAutoResolver.BaseSlotID)
                {
                    List<CustomFacePaintLayoutPreset.FacePaintPreset> lstPreset = Traverse.Create(__instance).Field("lstPreset").GetValue() as List<CustomFacePaintLayoutPreset.FacePaintPreset>;
                    CvsMakeup cvsMakeup = Traverse.Create(__instance).Field("cvsMakeup").GetValue() as CvsMakeup;

                    var preset = lstPreset.FirstOrDefault(p => p.index == index);
                    if (preset == null)
                        return false;

                    cvsMakeup.UpdatePushFacePaintLayout(new Vector4(preset.x, preset.y, preset.r, preset.s));
                    return false;
                }

                return true;
            }
            /// <summary>
            /// The game gets from a list by index which will cause errors. Get them safely for sideloader items
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomMoleLayoutPreset), nameof(CustomMoleLayoutPreset.OnPush))]
            private static bool OnPush(int index, CustomMoleLayoutPreset __instance)
            {
                if (index >= UniversalAutoResolver.BaseSlotID)
                {
                    List<CustomMoleLayoutPreset.MolePreset> lstPreset = Traverse.Create(__instance).Field("lstPreset").GetValue() as List<CustomMoleLayoutPreset.MolePreset>;
                    CvsMole cvsMole = Traverse.Create(__instance).Field("cvsMole").GetValue() as CvsMole;

                    var preset = lstPreset.FirstOrDefault(p => p.index == index);
                    if (preset == null)
                        return false;

                    ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
                    chaCtrl.chaFile.custom.face.moleLayout = new Vector4(preset.x, preset.y, 0f, preset.w);
                    cvsMole.FuncUpdateMoleLayout();
                    cvsMole.UpdateCustomUI();
                    return false;
                }

                return true;
            }
#endif
        }
    }
}