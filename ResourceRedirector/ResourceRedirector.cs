using BepInEx;
using BepInEx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResourceRedirector
{
    [BepInPlugin(GUID: "com.bepis.bepinex.resourceredirector", Name: "Asset Emulator", Version: "1.3")]
    public class ResourceRedirector : BaseUnityPlugin
    {
        public static string EmulatedDir => Path.Combine(Utility.ExecutingDirectory, "abdata-emulated");

        public static bool EmulationEnabled;



        public delegate bool AssetHandler(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result);

        public static List<AssetHandler> AssetResolvers = new List<AssetHandler>();



        public ResourceRedirector()
        {
            Hooks.InstallHooks();

            EmulationEnabled = Directory.Exists(EmulatedDir);

            AssetResolvers.Add(BGMLoader.HandleAsset);
        }

        
        public static AssetBundleLoadAssetOperation HandleAsset(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, ref AssetBundleLoadAssetOperation __result)
        {
            foreach (var handler in AssetResolvers)
            {
                try
                {
                    if (handler.Invoke(assetBundleName, assetName, type, manifestAssetBundleName, out AssetBundleLoadAssetOperation result))
                        return result;
                }
                catch { }
            }

            //emulate asset load
            string dir = Path.Combine(EmulatedDir, assetBundleName.Replace('/', '\\').Replace(".unity3d", ""));

            if (Directory.Exists(dir))
            {
                if (type == typeof(Texture2D))
                {
                    string path = Path.Combine(dir, $"{assetName}.png");

                    if (!File.Exists(path))
                        return __result;

                    BepInLogger.Log($"Loading emulated asset {path}");

                    var tex = AssetLoader.LoadTexture(path);

                    if (path.Contains("clamp"))
                        tex.wrapMode = TextureWrapMode.Clamp;
                    else if (path.Contains("repeat"))
                        tex.wrapMode = TextureWrapMode.Repeat;


                    return new AssetBundleLoadAssetOperationSimulation(tex);
                }
                else if (type == typeof(AudioClip))
                {
                    string path = Path.Combine(dir, $"{assetName}.wav");

                    if (!File.Exists(path))
                        return __result;

                    BepInLogger.Log($"Loading emulated asset {path}");

                    return new AssetBundleLoadAssetOperationSimulation(AssetLoader.LoadAudioClip(path, AudioType.WAV));
                }
            }

            //otherwise return normal asset
            return __result;
        }
    }
}
