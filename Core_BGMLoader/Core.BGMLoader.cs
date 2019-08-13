using BepInEx;
using BepInEx.Logging;
using BepisPlugins;
using Illusion.Game;
using System;
using System.IO;

namespace BGMLoader
{
    /// <summary>
    /// Place .ogg files in BepInEx/bgm folder with the same name as BGM tracks to load them.
    /// </summary>
    public partial class BGMLoader
    {
        public const string GUID = "com.bepis.bgmloader";
        public const string PluginName = "BGM Loader";
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;

        public BGMLoader()
        {
            Hooks.InstallHooks();

            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(HandleAsset);
            Logger = base.Logger;
        }

        public static bool HandleAsset(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            if (assetName.StartsWith("bgm") && assetName.Length > 4)
            {
                int bgmTrack = int.Parse(assetName.Remove(0, 4));

                var path = Utility.CombinePaths(Paths.PluginPath, "bgm", $"BGM{bgmTrack:00}.ogg");

                if (File.Exists(path))
                {
                    Logger.Log(LogLevel.Info, $"Loading BGM track \"{(BGM)bgmTrack}\" from {path}");

                    result = new AssetBundleLoadAssetOperationSimulation(AudioLoader.LoadVorbis(path));

                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
