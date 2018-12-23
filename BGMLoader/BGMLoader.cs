using System;
using System.IO;
using BepisPlugins;
using BepInEx;
using BepInEx.Logging;
using Illusion.Game;
using Logger = BepInEx.Logger;

namespace BGMLoader
{
	[BepInPlugin(GUID, "BGM Loader", Version)]
    public class BGMLoader : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bgmloader";
        public const string Version = Metadata.PluginsVersion;

        public BGMLoader()
	    {
	        Hooks.InstallHooks();

            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(HandleAsset);
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
