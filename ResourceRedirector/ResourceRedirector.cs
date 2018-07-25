using BepInEx;
using BepInEx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace ResourceRedirector
{
    [BepInPlugin(GUID: "com.bepis.bepinex.resourceredirector", Name: "Asset Emulator", Version: "1.3")]
    public class ResourceRedirector : BaseUnityPlugin
    {
        public static string EmulatedDir => Path.Combine(Paths.GameRootPath, "abdata-emulated");

        public static bool EmulationEnabled;



        public delegate bool AssetHandler(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result);

        public static List<AssetHandler> AssetResolvers = new List<AssetHandler>();

        public static Dictionary<string, AssetBundle> EmulatedAssetBundles = new Dictionary<string, AssetBundle>();



        public ResourceRedirector()
        {
            Hooks.InstallHooks();

            EmulationEnabled = Directory.Exists(EmulatedDir);
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
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
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

                    Logger.Log(LogLevel.Info, $"Loading emulated asset {path}");

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

                    Logger.Log(LogLevel.Info, $"Loading emulated asset {path}");

                    return new AssetBundleLoadAssetOperationSimulation(AssetLoader.LoadAudioClip(path, AudioType.WAV));
                }
            }

            string emulatedPath = Path.Combine(EmulatedDir, assetBundleName.Replace('/', '\\'));

            if (File.Exists(emulatedPath))
            {
                if (!EmulatedAssetBundles.TryGetValue(emulatedPath, out AssetBundle bundle))
                {
                    bundle = AssetBundle.LoadFromFile(emulatedPath);

                    EmulatedAssetBundles[emulatedPath] = bundle;
                }

                return new AssetBundleLoadAssetOperationSimulation(bundle.LoadAsset(assetName));
            }

            //otherwise return normal asset
            return __result;
        }
    }
}
