using BepInEx;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResourceRedirector
{
	static class Hooks
	{
		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.resourceredirector");
			harmony.PatchAll(typeof(Hooks));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), "LoadListInfoAll")]
		public static void LoadListInfoAllPostHook(ChaListControl __instance)
		{
			string listPath = Path.Combine(ResourceRedirector.EmulatedDir, @"list\characustom");

			//BepInLogger.Log($"List directory exists? {Directory.Exists(listPath).ToString()}");

			if (Directory.Exists(listPath))
				foreach (string csvPath in Directory.GetFiles(listPath, "*.csv", SearchOption.AllDirectories))
				{
					//BepInLogger.Log($"Attempting load of: {csvPath}");

					var chaListData = ListLoader.LoadCSV(File.OpenRead(csvPath));
					ListLoader.ExternalDataList.Add(chaListData);

					//BepInLogger.Log($"Finished load of: {csvPath}");
				}

			ListLoader.LoadAllLists(__instance);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), "LoadAsset", new[] {typeof(string), typeof(string), typeof(Type), typeof(string)})]
		public static void LoadAssetPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, string assetName, Type type, string manifestAssetBundleName)
		{
			//BepInLogger.Log($"{assetBundleName} : {assetName} : {type.FullName} : {manifestAssetBundleName ?? ""}");

			__result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), "LoadAssetBundle", new[] {typeof(string), typeof(bool), typeof(string)})]
		public static void LoadAssetBundlePostHook(string assetBundleName, bool isAsync, string manifestAssetBundleName)
		{
			//BepInLogger.Log($"{assetBundleName} : {manifestAssetBundleName} : {isAsync}");
		}

		[HarmonyPostfix,
		 HarmonyPatch(typeof(AssetBundleManager), "LoadAssetAsync", new[] {typeof(string), typeof(string), typeof(Type), typeof(string)})]
		public static void LoadAssetAsyncPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, string assetName, Type type, string manifestAssetBundleName)
		{
			//BepInLogger.Log($"{assetBundleName} : {assetName} : {type.FullName} : {manifestAssetBundleName ?? ""}", true);

			__result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AssetBundleManager), "LoadAllAsset", new[] {typeof(string), typeof(Type), typeof(string)})]
		public static void LoadAllAssetPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, Type type, string manifestAssetBundleName = null)
		{
			//BepInLogger.Log($"{assetBundleName} : {type.FullName} : {manifestAssetBundleName ?? ""}");

			if (assetBundleName == "sound/data/systemse/brandcall/00.unity3d" ||
			    assetBundleName == "sound/data/systemse/titlecall/00.unity3d")
			{
				string dir = $"{BepInEx.Common.Utility.PluginsDirectory}\\introclips";

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				var files = Directory.GetFiles(dir, "*.wav");

				if (files.Length == 0)
					return;

				List<UnityEngine.Object> loadedClips = new List<UnityEngine.Object>();

				foreach (string path in files)
					loadedClips.Add(AssetLoader.LoadAudioClip(path, AudioType.WAV));

				__result = new AssetBundleLoadAssetOperationSimulation(loadedClips.ToArray());
			}
		}
	}
}