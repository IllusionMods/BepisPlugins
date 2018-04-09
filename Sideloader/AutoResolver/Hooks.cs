using System;
using ExtensibleSaveFormat;
using Harmony;
using System.Collections.Generic;
using System.Linq;

namespace Sideloader.AutoResolver
{
	public static class Hooks
	{
		private static ChaFileCustom lastLoadedInstance = null;

		public static void InstallHooks()
		{
			ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
			ExtendedSave.CardBeingSaved += ExtendedCardSave;

			var harmony = HarmonyInstance.Create(UniversalAutoResolver.UARExtID);
			harmony.PatchAll(typeof(Hooks));
		}

		private static void ExtendedCardLoad(ChaFile file)
		{
			UniversalAutoResolver.ResolveStructure(StructReference.ChaFileFaceProperties, lastLoadedInstance.face, file);
		}

		private static void ExtendedCardSave(ChaFile file)
		{
			List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

			foreach (var kv in StructReference.ChaFileFaceProperties)
			{
				var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key &&
				                                                                          x.Slot == (int) kv.Value.GetValue(
					                                                                          file.custom.face, null));
			}

			resolutionInfo.Add(UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Pupil1" &&
			                                                                                  x.Slot == file.custom.face.pupil[0]
				                                                                                  .id));
			resolutionInfo.Add(UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Pupil2" &&
			                                                                                  x.Slot == file.custom.face.pupil[1]
				                                                                                  .id));



			ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
			{
				data = new Dictionary<string, object>
				{
					{"info", resolutionInfo.Where(x => x != null).Select(x => x.Serialize()).ToList()}
				}
			});
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaFileCustom), "LoadBytes")]
		public static void LoadBytesPostHook(ChaFileCustom __instance)
		{
			lastLoadedInstance = __instance;
		}

		//[HarmonyPrefix, HarmonyPatch(typeof(UnityEngine.AssetBundle), "LoadFromFile", new[] { typeof(string) })]
		//public static void TestHook(string path)
		//{
		//	BepInEx.BepInLogger.Log(path, true);

		//	Console.WriteLine(Environment.StackTrace);
		//}

		[HarmonyPrefix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
		public static void TestHook(string assetBundleName)
		{
			BepInEx.BepInLogger.Log(assetBundleName, true);

			//Console.WriteLine(Environment.StackTrace);
		}

		//[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "ChangeClothesTopAsync")]
		//public static void TestHook(int id)
		//{
		//	BepInEx.BepInLogger.Log(id.ToString(), true);

		//	Console.WriteLine(Environment.StackTrace);
		//}
	}
}