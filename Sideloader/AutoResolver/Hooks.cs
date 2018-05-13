using ExtensibleSaveFormat;
using Harmony;
using System.Collections.Generic;
using System.Linq;
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

		private static void ExtendedCardLoad(ChaFile file)
		{
		    UniversalAutoResolver.ResolveStructure(StructReference.ChaFileFaceProperties, file.custom.face, file);
		    UniversalAutoResolver.ResolveStructure(StructReference.ChaFileBodyProperties, file.custom.body, file);
		    UniversalAutoResolver.ResolveStructure(StructReference.ChaFileHairProperties, file.custom.hair, file);

		    for (int i = 0; i < file.coordinate.Length; i++)
		    {
		        var coordinate = file.coordinate[i];
		        string prefix = $"outfit{i}.";
                
		        UniversalAutoResolver.ResolveStructure(StructReference.ChaFileClothesProperties, coordinate.clothes, file, prefix);
		        UniversalAutoResolver.ResolveStructure(StructReference.ChaFileMakeupProperties, coordinate.makeup, file, prefix);

		        for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
		        {
		            string accPrefix = $"{prefix}accessory{acc}.";

		            UniversalAutoResolver.ResolveStructure(StructReference.ChaFileAccessoryPartsInfoProperties, coordinate.accessory.parts[acc], file, accPrefix);
		        }
		    }
        }

		private static void ExtendedCardSave(ChaFile file)
		{
			List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

		    void IterateStruct(object obj, Dictionary<CategoryProperty, StructValue<int>> dict, string propertyPrefix = "")
		    {
		        foreach (var kv in dict)
		        {
		            int slot = kv.Value.GetMethod(obj);

		            var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString() &&
		                                                                                      x.LocalSlot == slot);

		            if (info != null)
		            {
		                var newInfo = info.DeepCopy();
		                newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

		                kv.Value.SetMethod(obj, newInfo.Slot);

		                resolutionInfo.Add(newInfo);
		            }
		        }
		    }
            
		    IterateStruct(file.custom.face, StructReference.ChaFileFaceProperties);
		    IterateStruct(file.custom.body, StructReference.ChaFileBodyProperties);
		    IterateStruct(file.custom.hair, StructReference.ChaFileHairProperties);

            for (int i = 0; i < file.coordinate.Length; i++)
		    {
		        var coordinate = file.coordinate[i];
		        string prefix = $"outfit{i}.";

                IterateStruct(coordinate.clothes, StructReference.ChaFileClothesProperties, prefix);
                IterateStruct(coordinate.makeup, StructReference.ChaFileMakeupProperties, prefix);

		        for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
		        {
		            string accPrefix = $"{prefix}accessory{acc}.";
                    
		            IterateStruct(coordinate.accessory.parts[acc], StructReference.ChaFileAccessoryPartsInfoProperties, accPrefix);
		        }
            }

            ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
			{
				data = new Dictionary<string, object>
				{
					{"info", resolutionInfo.Select(x => x.Serialize()).ToList()}
				}
			});
		}
	}
}