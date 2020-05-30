using BepInEx.Harmony;
using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
                var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
#if KK
                harmony.Patch(typeof(GlobalMethod).GetMethod(nameof(GlobalMethod.LoadAllFolder), AccessTools.all).MakeGenericMethod(typeof(Object)),
                              null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(LoadAllFolderPostfix), AccessTools.all)));
#endif

#if !EC
                harmony.Patch(typeof(Studio.Info).GetNestedType("FileCheck", AccessTools.all).GetMethod("Check", AccessTools.all), null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(FileCheck), AccessTools.all)));
#endif
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
            internal static void IsFileHook(string assetBundleName, ref bool __result)
            {
                if (!__result)
                {
                    if (BundleManager.Bundles.ContainsKey(assetBundleName))
                        __result = true;
                    if (IsPngFolderOnly(assetBundleName))
                        __result = true;
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleData), nameof(AssetBundleData.isFile), MethodType.Getter)]
            internal static void IsFileHook2(ref bool __result, AssetBundleData __instance)
            {
                if (!__result)
                {
                    if (BundleManager.Bundles.ContainsKey(__instance.bundle))
                        __result = true;
                    if (IsPngFolderOnly(__instance.bundle))
                        __result = true;
                }
            }
#if KK || EC
            /// <summary>
            /// The game gets from a list by index which will cause errors. Get them safely for sideloader items
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomFacePaintLayoutPreset), nameof(CustomFacePaintLayoutPreset.OnPush))]
            internal static bool FacePaintOnPush(int index, CustomFacePaintLayoutPreset __instance)
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
            internal static bool OnPush(int index, CustomMoleLayoutPreset __instance)
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