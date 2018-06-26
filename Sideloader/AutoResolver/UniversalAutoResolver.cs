using System;
using BepInEx;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;

namespace Sideloader.AutoResolver
{
    public static class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";

        public static List<ResolveInfo> LoadedResolutionInfo = new List<ResolveInfo>();

        public static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ChaFile save, string propertyPrefix = "")
        {
            //BepInLogger.Log($"Tried to resolve structure: {structure.GetType().Name}");

            var extData = ExtendedSave.GetExtendedDataById(save, UARExtID);

            IEnumerable<ResolveInfo> extInfo;

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                extInfo = null;
                //BepInLogger.Log("Nothing to load!");
            }
            else
            {
                var tmpExtInfo = (object[])extData.data["info"];
                extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x));
            }

            //BepInLogger.Log($"Internal info count: {LoadedResolutionInfo.Count}");
            //foreach (ResolveInfo info in LoadedResolutionInfo)
            //    BepInLogger.Log($"Internal info: {info.ModID} : {info.Property} : {info.Slot}");

            //if (extInfo.Any())
            //{
            //    BepInLogger.Log($"External info count: {extInfo.Count()}");
            //    foreach (ResolveInfo info in extInfo)
            //        BepInLogger.Log($"External info: {info.ModID} : {info.Property} : {info.Slot}");
            //}

            foreach (var kv in propertyDict)
            {
                if (extInfo != null)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                    if (extResolve != null)
                    {
                        //the property has external slot information 
                        var intResolve = LoadedResolutionInfo.FirstOrDefault(x =>
                            x.AppendPropertyPrefix(propertyPrefix).CanResolve(extResolve));

                        if (intResolve != null)
                        {
                            //found a match to a corrosponding internal mod
                            Logger.Log(LogLevel.Info, $"[UAR] Resolving {extResolve.GUID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.LocalSlot}");

                            kv.Value.SetMethod(structure, intResolve.LocalSlot);
                        }
                        else
                        {
                            //did not find a match, we don't have the mod
                            Logger.Log(LogLevel.Warning | LogLevel.Message, $"[UAR] WARNING! ({save.parameter.lastname} {save.parameter.firstname}) Missing mod detected! [{extResolve.GUID}]");

                            kv.Value.SetMethod(structure, 999999); //set to an invalid ID
                        }
                    }
                }
                else
                {
                    //check if it's a vanilla item
                    if (!ResourceRedirector.ListLoader.InternalDataList[kv.Key.Category]
                        .ContainsKey(kv.Value.GetMethod(structure)))
                    {
                        //the property does not have external slot information
                        //check if we have a corrosponding item for backwards compatbility
                        var intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString()
                                                                                  && x.Slot == kv.Value.GetMethod(structure));

                        if (intResolve != null)
                        {
                            //found a match
                            Logger.Log(LogLevel.Info, $"[UAR] Compatibility resolving {intResolve.Property} from slot {kv.Value.GetMethod(structure)} to slot {intResolve.LocalSlot}");

                            kv.Value.SetMethod(structure, intResolve.LocalSlot);
                        }
                        //otherwise ignore if not found
                    }
                    else
                    {
                        //not resolving since we prioritize vanilla items over modded items
                        //BepInLogger.Log($"[UAR] Not resolving item due to vanilla ID range");
                        //log commented out because it causes too much spam
                    }
                }
            }
        }
        
        private static int CurrentSlotID = 100000000;

        public static void GenerateResolutionInfo(Manifest manifest, ChaListData data)
        {
            var category = (ChaListDefine.CategoryNo)data.categoryNo;

            var properties = StructReference.CollatedStructValues.Where(x => x.Key.Category == category);

            //BepInLogger.Log(category.ToString());
            //BepInLogger.Log(StructReference.CollatedStructValues.Count.ToString());


            foreach (var kv in data.dictList)
            {
                int newSlot = Interlocked.Increment(ref CurrentSlotID);

                //BepInLogger.Log(kv.Value[0] + " | " + newSlot);

                foreach (var property in properties)
                {
                    //BepInLogger.Log(property.Key.ToString());

                    LoadedResolutionInfo.Add(new ResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(kv.Value[0]),
                        LocalSlot = newSlot,
                        Property = property.Key.ToString()
                    });
                    
                    //BepInLogger.Log($"LOADED COUNT {LoadedResolutionInfo.Count}");
                }

                kv.Value[0] = newSlot.ToString();
            }
        }
    }
}
