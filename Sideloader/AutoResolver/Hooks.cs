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
		    UniversalAutoResolver.ResolveStructure(StructReference.ChaFileBodyProperties, lastLoadedInstance.body, file);
		    UniversalAutoResolver.ResolveStructure(StructReference.ChaFileHairProperties, lastLoadedInstance.hair, file);
		}

		private static void ExtendedCardSave(ChaFile file)
		{
			List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

		    void IterateStruct(object obj, Dictionary<CategoryProperty, StructValue<int>> dict)
		    {
		        foreach (var kv in dict)
		        {
		            int slot = kv.Value.GetMethod(obj);

		            var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString() &&
		                                                                                      x.LocalSlot == slot);

		            if (info != null)
		            {
		                kv.Value.SetMethod(obj, info.Slot);

		                resolutionInfo.Add(info);
		            }
		        }
		    }
            
		    IterateStruct(file.custom.face, StructReference.ChaFileFaceProperties);
		    IterateStruct(file.custom.body, StructReference.ChaFileBodyProperties);
		    IterateStruct(file.custom.hair, StructReference.ChaFileHairProperties);

		    //foreach (var coordinate in file.coordinate)
		    //{
		    //    IterateStruct(file.coordinate., StructReference.ChaFileFaceProperties);
		    //    IterateStruct(file.custom.face, StructReference.ChaFileFaceProperties);
		    //}

			ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
			{
				data = new Dictionary<string, object>
				{
					{"info", resolutionInfo.Select(x => x.Serialize()).ToList()}
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