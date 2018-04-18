using BepInEx;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sideloader.AutoResolver
{
    public static class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";

        public static List<ResolveInfo> LoadedResolutionInfo = new List<ResolveInfo>();

        public static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ChaFile save)
        {
            //BepInLogger.Log($"Tried to resolve structure: {structure.GetType().Name}");

            var extData = ExtendedSave.GetExtendedDataById(save, UARExtID);

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                //BepInLogger.Log($"No info to load!");
                return;
            }

            var tmpExtInfo = (object[])extData.data["info"];
            var extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x));

            //BepInLogger.Log($"Internal info count: {LoadedResolutionInfo.Count}");
            //foreach (ResolveInfo info in LoadedResolutionInfo)
            //    BepInLogger.Log($"Internal info: {info.ModID} : {info.Property} : {info.Slot}");

            //BepInLogger.Log($"External info count: {extInfo.Count()}");
            //foreach (ResolveInfo info in extInfo)
            //    BepInLogger.Log($"External info: {info.ModID} : {info.Property} : {info.Slot}");


            foreach (var kv in propertyDict)
            {
                var extResolve = extInfo.FirstOrDefault(x => x.Property == kv.Key.ToString());

                if (extResolve != null)
                {
                    var intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.CanResolve(extResolve));

                    if (intResolve != null)
                    {
                        BepInLogger.Log($"[UAR] Resolving {extResolve.ModID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.LocalSlot}");

                        kv.Value.SetMethod(structure, intResolve.LocalSlot);
                    }
                }
            }
        }
        
        private static int CurrentSlotID = 10000;

        public static void GenerateResolutionInfo(Manifest manifest, ChaListData data)
        {
            var category = (ChaListDefine.CategoryNo)data.categoryNo;

            var properties = StructReference.CollatedStructValues.Where(x => x.Key.Category == category);

            //BepInEx.BepInLogger.Log(category.ToString());
            //BepInEx.BepInLogger.Log(StructReference.CollatedStructValues.Count.ToString());


            foreach (var kv in data.dictList)
            {
                int newSlot = Interlocked.Increment(ref CurrentSlotID);

                // BepInEx.BepInLogger.Log(kv.Value[0] + " | " + newSlot);

                foreach (var property in properties)
                {
                    // BepInEx.BepInLogger.Log(property.Key.ToString());

                    LoadedResolutionInfo.Add(new ResolveInfo
                    {
                        ModID = manifest.GUID,
                        Slot = int.Parse(kv.Value[0]),
                        LocalSlot = newSlot,
                        Property = property.Key.ToString()
                    });
                    
                    // BepInEx.BepInLogger.Log($"LOADED COUNT {LoadedResolutionInfo.Count}");
                }

                kv.Value[0] = newSlot.ToString();
            }
        }
    }
}
