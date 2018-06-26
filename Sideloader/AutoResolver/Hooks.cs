using System;
using ExtensibleSaveFormat;
using Harmony;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Illusion.Extensions;

namespace Sideloader.AutoResolver
{
	public static class Hooks
	{
		public static void InstallHooks()
		{
			ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
			ExtendedSave.CardBeingSaved += ExtendedCardSave;

			var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader.universalautoresolver");
			harmony.PatchAll(typeof(Hooks));
		}

		private static void IteratePrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ChaFile, string> action, ChaFile file)
		{
			action(StructReference.ChaFileFaceProperties, file.custom.face, file, "");
			action(StructReference.ChaFileBodyProperties, file.custom.body, file, "");
			action(StructReference.ChaFileHairProperties, file.custom.hair, file, "");

			for (int i = 0; i < file.coordinate.Length; i++)
			{
				var coordinate = file.coordinate[i];
				string prefix = $"outfit{i}.";
                
				action(StructReference.ChaFileClothesProperties, coordinate.clothes, file, prefix);
				action(StructReference.ChaFileMakeupProperties, coordinate.makeup, file, prefix);

				for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
				{
					string accPrefix = $"{prefix}accessory{acc}.";

					action(StructReference.ChaFileAccessoryPartsInfoProperties, coordinate.accessory.parts[acc], file, accPrefix);
				}
			}
		}

		private static void ExtendedCardLoad(ChaFile file)
		{
			Logger.Log(LogLevel.Debug, $"Loading card [{file.charaFileName}]");

			var extData = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID);

			if (extData == null || !extData.data.ContainsKey("info"))
			{
				Logger.Log(LogLevel.Debug, "No sideloader marker found");
			}
			else
			{
				var tmpExtInfo = (object[])extData.data["info"];
				var extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x));
				
				Logger.Log(LogLevel.Debug, "Sideloader marker found");
				Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count()}");
				foreach (ResolveInfo info in extInfo)
					Logger.Log(LogLevel.Debug, $"External info: {info.ModID} : {info.Property} : {info.Slot}");
			}

			IteratePrefixes(UniversalAutoResolver.ResolveStructure, file);
        }

		private static void ExtendedCardSave(ChaFile file)
		{
			List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

		    void IterateStruct(Dictionary<CategoryProperty, StructValue<int>> dict, object obj, ChaFile chaFile, string propertyPrefix = "")
		    {
		        foreach (var kv in dict)
		        {
		            int slot = kv.Value.GetMethod(obj);

		            var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString() &&
		                                                                                      x.LocalSlot == slot);

			        if (info == null) 
				        continue;


			        var newInfo = info.DeepCopy();
			        newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

			        kv.Value.SetMethod(obj, newInfo.Slot);

			        resolutionInfo.Add(newInfo);
		        }
		    }

			IteratePrefixes(IterateStruct, file);

            ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
			{
				data = new Dictionary<string, object>
				{
					["info"] = resolutionInfo.Select(x => x.Serialize()).ToList()
				}
			});
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
		public static void ChaFileSaveFilePostHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
		{
			Logger.Log(LogLevel.Debug, $"Reloading card [{__instance.charaFileName}]");

			var extData = ExtendedSave.GetExtendedDataById(__instance, UniversalAutoResolver.UARExtID);

			var tmpExtInfo = (List<byte[]>) extData.data["info"];
			var extInfo = tmpExtInfo.Select(ResolveInfo.Unserialize);

			Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count()}");
			foreach (ResolveInfo info in extInfo)
				Logger.Log(LogLevel.Debug, $"External info: {info.ModID} : {info.Property} : {info.Slot}");

			void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ChaFile file, string propertyPrefix = "")
			{
				foreach (var kv in propertyDict)
				{
					var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

					if (extResolve != null)
					{
						kv.Value.SetMethod(structure, extResolve.LocalSlot);

						Logger.Log(LogLevel.Debug, $"[UAR] Resetting {extResolve.ModID}:{extResolve.Property} to internal slot {extResolve.LocalSlot}");
					}
				}
			}

			IteratePrefixes(ResetStructResolveStructure, __instance);
		}
	}
}