using BepInEx;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sideloader.AutoResolver
{
    public static class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";

        public static List<ResolveInfo> LoadedResolutionInfo = new List<ResolveInfo>();

        public static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ChaFile save)
        {
            if (LoadedResolutionInfo.Count == 0)
                GenerateResolutionInfo();

            //BepInLogger.Log($"Tried to resolve structure: {structure.GetType().Name}");

            var extData = ExtendedSave.GetExtendedDataById(save, UARExtID);

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                //BepInLogger.Log($"No info to load!");
                return;
            }

            var obj = extData.data["info"];


            //BepInLogger.Log(obj.GetType().ToString());



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
                        BepInLogger.Log($"[UAR] Resolving {extResolve.ModID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.Slot}");

                        kv.Value.SetMethod(structure, intResolve.Slot);
                    }
                }
            }
        }

        public static void GenerateResolutionInfo()
        {
            LoadedResolutionInfo.Clear();

            foreach (var manifestData in Sideloader.LoadedData)
            {
                foreach (var data in manifestData.Value)
                {
                    var category = (ChaListDefine.CategoryNo)data.categoryNo;

                    foreach (var property in StructReference.CollatedStructValues.Where(x => x.Key.Category == category))
                    {
                        foreach (var kv in data.dictList)
                        {
                            LoadedResolutionInfo.Add(new ResolveInfo
                            {
                                ModID = manifestData.Key.GUID,
                                Slot = int.Parse(kv.Value[0]),
                                Property = property.Key.ToString()
                            });
                        }
                    }
                }
            }
        }
    }
}
