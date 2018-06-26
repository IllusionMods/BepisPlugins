using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Illusion.Game;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace BGMLoader
{
	[BepInPlugin("com.bepis.bgmloader", "BGM Loader", "1.0")]
    public class BGMLoader : BaseUnityPlugin
    {
	    public BGMLoader()
	    {
			ResourceRedirector.ResourceRedirector.AssetResolvers.Add(HandleAsset);
	    }

	    public static bool HandleAsset(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
	    {
		    if (assetName.StartsWith("bgm") && assetName.Length > 4)
		    {
			    string path;

			    switch ((BGM)int.Parse(assetName.Remove(0, 4)))
			    {
				    case BGM.Title:
				    default:
					    path = $"{BepInEx.Common.Utility.PluginsDirectory}\\title.wav";
					    break;
				    case BGM.Custom:
					    path = $"{BepInEx.Common.Utility.PluginsDirectory}\\custom.wav";
					    break;
			    }

			    if (File.Exists(path))
			    {
				    Logger.Log(LogLevel.Info, $"Loading {path}");

				    result = new AssetBundleLoadAssetOperationSimulation(ResourceRedirector.AssetLoader.LoadAudioClip(path, AudioType.WAV));

				    return true;
			    }
		    }

		    result = null;
		    return false;
	    }
    }
}
