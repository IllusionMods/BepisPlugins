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

	    #region List Loading
	    [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.CheckItemID), new [] { typeof(int), typeof(int) })]
	    public static bool CheckItemIDHook(int category, int id, ref byte __result, ChaListControl __instance)
	    {
	        int pid = ListLoader.CalculateGlobalID(category, id);

            byte result = __instance.CheckItemID(pid);

	        if (result > 0)
	        {
	            //BepInLogger.Log($"CHECK {category} : {id} : {result}");
	            __result = result;
	            return false;
	        }

	        return true;
	    }

	    [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.AddItemID), new [] { typeof(int), typeof(int), typeof(byte) })]
	    public static bool AddItemIDHook(int category, int id, byte flags, ChaListControl __instance)
	    {
	        int pid = ListLoader.CalculateGlobalID(category, id);

	        byte result = __instance.CheckItemID(pid);

	        if (result > 0)
	        {
                //BepInLogger.Log($"ADD {category} : {id} : {result}");
	            __instance.AddItemID(pid, flags);
	            return false;
	        }

	        return true;
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
	    #endregion

        #region Asset Loading
		[HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), new[] {typeof(string), typeof(string), typeof(Type), typeof(string)})]
		public static bool LoadAssetPostHook(ref AssetBundleLoadAssetOperation __result, string assetBundleName, string assetName, Type type, string manifestAssetBundleName)
		{
		    __result = ResourceRedirector.HandleAsset(assetBundleName, assetName, type, manifestAssetBundleName, ref __result);

            if (__result != null)
		        return false;

		    return true;
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
        #endregion
	}
}