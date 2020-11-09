using BepInEx;
using HarmonyLib;
using System;
using System.IO;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
            private static void IsFileHookEC(string assetBundleName, string fileName, ref bool __result)
            {
                //Redirect KK vanilla assets to EC vanilla assets
                if (__result == false && assetBundleName.EndsWith(".unity3d") && assetBundleName.StartsWith("chara/"))
                {
                    string temp = assetBundleName.Replace(".unity3d", "");
                    if (temp.Length >= 2)
                    {
                        temp = temp.Substring(temp.Length - 2, 2);
                        if (int.TryParse(temp, out _))
                        {
                            if (assetBundleName.StartsWith("chara/thumb/") && !assetBundleName.StartsWith($"chara/thumb/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/thumb/", $"chara/thumb/{temp}/");
                                __result = AssetBundleCheck.IsFile(temp, fileName);
                            }
                            else if (!assetBundleName.StartsWith($"chara/{temp}/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/", $"chara/{temp}/");
                                __result = AssetBundleCheck.IsFile(temp, fileName);
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleData), nameof(AssetBundleData.isFile), MethodType.Getter)]
            private static void IsFileHookEC2(ref bool __result, AssetBundleData __instance)
            {
                //Redirect KK vanilla assets to EC vanilla assets
                if (__result == false && __instance.bundle.EndsWith(".unity3d") && __instance.bundle.StartsWith("chara/"))
                {
                    string temp = __instance.bundle.Replace(".unity3d", "");
                    if (temp.Length >= 2)
                    {
                        temp = temp.Substring(temp.Length - 2, 2);
                        if (int.TryParse(temp, out _))
                        {
                            if (__instance.bundle.StartsWith("chara/thumb/") && !__instance.bundle.StartsWith($"chara/thumb/{temp}/"))
                            {
                                temp = __instance.bundle.Replace("chara/thumb/", $"chara/thumb/{temp}/");
                                __result = AssetBundleCheck.IsFile(temp);
                            }
                            else if (!__instance.bundle.StartsWith($"chara/{temp}/") && !__instance.bundle.StartsWith($"chara/{temp}/"))
                            {
                                temp = __instance.bundle.Replace("chara/", $"chara/{temp}/");
                                __result = AssetBundleCheck.IsFile(temp);
                            }
                        }
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetPreHook(ref string assetBundleName)
            {
                //Redirect KK vanilla assets to EC vanilla assets
                if (!File.Exists($"{Paths.GameRootPath}/abdata/{assetBundleName}") && assetBundleName.EndsWith(".unity3d") && assetBundleName.StartsWith("chara/"))
                {
                    string temp = assetBundleName.Replace(".unity3d", "");
                    if (temp.Length >= 2)
                    {
                        temp = temp.Substring(temp.Length - 2, 2);
                        if (int.TryParse(temp, out _))
                        {
                            if (assetBundleName.StartsWith("chara/thumb/") && !assetBundleName.StartsWith($"chara/thumb/{temp}/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/thumb/", $"chara/thumb/{temp}/");
                                if (File.Exists($"{Paths.GameRootPath}/abdata/{temp}"))
                                    assetBundleName = temp;
                            }
                            else if (assetBundleName.StartsWith("chara/") && !assetBundleName.StartsWith($"chara/thumb/{temp}/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/", $"chara/{temp}/");
                                if (File.Exists($"{Paths.GameRootPath}/abdata/{temp}"))
                                    assetBundleName = temp;
                            }
                        }
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), "LoadAssetAsync", typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetAsyncPreHook(ref string assetBundleName)
            {
                //Redirect KK vanilla assets to EC vanilla assets
                if (!File.Exists($"{Paths.GameRootPath}/abdata/{assetBundleName}") && assetBundleName.EndsWith(".unity3d") && assetBundleName.StartsWith("chara/"))
                {
                    string temp = assetBundleName.Replace(".unity3d", "");
                    if (temp.Length >= 2)
                    {
                        temp = temp.Substring(temp.Length - 2, 2);
                        if (int.TryParse(temp, out _))
                        {
                            if (assetBundleName.StartsWith("chara/thumb/") && !assetBundleName.StartsWith($"chara/thumb/{temp}/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/thumb/", $"chara/thumb/{temp}/");
                                if (File.Exists($"{Paths.GameRootPath}/abdata/{temp}"))
                                    assetBundleName = temp;
                            }
                            else if (assetBundleName.StartsWith("chara/") && !assetBundleName.StartsWith($"chara/thumb/{temp}/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                            {
                                temp = assetBundleName.Replace("chara/", $"chara/{temp}/");
                                if (File.Exists($"{Paths.GameRootPath}/abdata/{temp}"))
                                    assetBundleName = temp;
                            }
                        }
                    }
                }
            }
        }
    }
}