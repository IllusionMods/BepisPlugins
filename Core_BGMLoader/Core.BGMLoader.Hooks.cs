using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace BGMLoader
{
    internal static class Hooks
    {
        public static void InstallHooks() => HarmonyWrapper.PatchAll(typeof(Hooks));

        [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), "LoadAllAsset", new[] { typeof(string), typeof(Type), typeof(string) })]
        public static void LoadAllAssetPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, Type type, string manifestAssetBundleName = null)
        {
            if (assetBundleName != null)
            {
                if (assetBundleName.StartsWith("sound/data/systemse/brandcall/") ||
                    assetBundleName.StartsWith("sound/data/systemse/titlecall/"))
                {
                    string dir = $@"{Paths.PluginPath}\introclips";
                    if (!Directory.Exists(dir)) return;

                    var files = Directory.GetFiles(dir, "*.wav");

                    if (files.Length == 0)
                        return;

                    var path = files[UnityEngine.Random.Range(0, files.Length - 1)];

                    var audioClip = ResourceRedirector.AssetLoader.LoadAudioClip(path, AudioType.WAV);

                    __result = new AssetBundleLoadAssetOperationSimulation(audioClip);
                }
            }
        }
    }
}