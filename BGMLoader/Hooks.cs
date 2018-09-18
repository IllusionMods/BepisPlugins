using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Harmony;
using UnityEngine;

namespace BGMLoader
{
    static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.resourceredirector");
            harmony.PatchAll(typeof(Hooks));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), "LoadAllAsset", new[] { typeof(string), typeof(Type), typeof(string) })]
        public static void LoadAllAssetPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, Type type, string manifestAssetBundleName = null)
        {
            //BepInLogger.Log($"{assetBundleName} : {type.FullName} : {manifestAssetBundleName ?? ""}");

            if (assetBundleName == "sound/data/systemse/brandcall/00.unity3d" ||
                assetBundleName == "sound/data/systemse/titlecall/00.unity3d")
            {
                string dir = $"{Paths.PluginPath}\\introclips";

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var files = Directory.GetFiles(dir, "*.wav");

                if (files.Length == 0)
                    return;

                List<UnityEngine.Object> loadedClips = new List<UnityEngine.Object>();

                foreach (string path in files)
                    loadedClips.Add(ResourceRedirector.AssetLoader.LoadAudioClip(path, AudioType.WAV));

                __result = new AssetBundleLoadAssetOperationSimulation(loadedClips.ToArray());
            }
        }
    }
}