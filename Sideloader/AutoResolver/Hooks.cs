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

			var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader.universalautoresolver");
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
			    int slot = kv.Value.GetMethod(file.custom.face);

				var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString() &&
				                                                                          x.Slot == slot);

			    resolutionInfo.Add(info);
			}

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
	}
}