using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;

namespace ResourceRedirector
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
        public static bool LoadAssetPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            //Redirect KK vanilla assets to EC vanilla assets
            if (__result == null && !ResourceRedirector.AssetBundleExists(assetBundleName) && assetBundleName.EndsWith(".unity3d") && assetBundleName.StartsWith("chara/"))
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
                            __result = AssetBundleManager.LoadAsset(temp, assetName, type, manifestAssetBundleName);
                            return false;
                        }
                        else if (assetBundleName.StartsWith("chara/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                        {
                            temp = assetBundleName.Replace("chara/", $"chara/{temp}/");
                            __result = AssetBundleManager.LoadAsset(temp, assetName, type, manifestAssetBundleName);
                            return false;
                        }
                    }
                }
            }

            if (__result == null)
            {
                if (!ResourceRedirector.AssetBundleExists(assetBundleName))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    ResourceRedirector.Logger.Log(LogLevel.Debug, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/00/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), "LoadAssetAsync", typeof(string), typeof(string), typeof(Type), typeof(string))]
        public static bool LoadAssetAsyncPreHook(ref AssetBundleLoadAssetOperation __result, ref string assetBundleName, ref string assetName, Type type, string manifestAssetBundleName)
        {
            __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            //Redirect KK vanilla assets to EC vanilla assets
            if (__result == null && !ResourceRedirector.AssetBundleExists(assetBundleName) && assetBundleName.EndsWith(".unity3d") && assetBundleName.StartsWith("chara/"))
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
                            __result = AssetBundleManager.LoadAssetAsync(temp, assetName, type, manifestAssetBundleName);
                            return false;
                        }
                        else if (assetBundleName.StartsWith("chara/") && !assetBundleName.StartsWith($"chara/{temp}/"))
                        {
                            temp = assetBundleName.Replace("chara/", $"chara/{temp}/");
                            __result = AssetBundleManager.LoadAssetAsync(temp, assetName, type, manifestAssetBundleName);
                            return false;
                        }
                    }
                }
            }

            if (__result == null)
            {
                if (!ResourceRedirector.AssetBundleExists(assetBundleName))
                {
                    //An asset that does not exist is being requested from from an asset bundle that does not exist
                    //Redirect to an asset bundle the does exist so that the game does not attempt to open a non-existant file and cause errors
                    ResourceRedirector.Logger.Log(LogLevel.Debug, $"Asset {assetName} does not exist in asset bundle {assetBundleName}.");
                    assetBundleName = "chara/00/mt_ramp_00.unity3d";
                    assetName = "dummy";
                }
                return true;
            }
            else
                return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
        public static void IsFileHook(string assetBundleName, string fileName, ref bool __result)
        {
            if (ResourceRedirector.EmulationEnabled && __result == false)
            {
                string dir = Path.Combine(ResourceRedirector.EmulatedDir, assetBundleName.Replace('/', '\\').Replace(".unity3d", ""));

                if (Directory.Exists(dir))
                    __result = true;
            }

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundleData))]
        [HarmonyPatch(nameof(AssetBundleData.isFile), MethodType.Getter)]
        public static void IsFileHook2(ref bool __result, AssetBundleData __instance)
        {
            if (ResourceRedirector.EmulationEnabled && __result == false)
            {
                string dir = Path.Combine(ResourceRedirector.EmulatedDir, __instance.bundle.Replace('/', '\\').Replace(".unity3d", ""));

                if (Directory.Exists(dir))
                    __result = true;
            }

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
    }
}
