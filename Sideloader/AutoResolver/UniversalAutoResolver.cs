using BepInEx;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx.Logging;

namespace Sideloader.AutoResolver
{
    public static class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";

        public static List<ResolveInfo> LoadedResolutionInfo = new List<ResolveInfo>();

        public static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo, string propertyPrefix = "")
        {
            void CompatibilityResolve(KeyValuePair<CategoryProperty, StructValue<int>> kv)
            {
                //check if it's a vanilla item
                if (!ResourceRedirector.ListLoader.InternalDataList[kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
                {
                    //the property does not have external slot information
                    //check if we have a corrosponding item for backwards compatbility

                    //For accessories, only check the appropriate category
                    if (propertyPrefix.Contains("accessory"))
                    {
                        ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)structure;

                        //This is so fucking bad, please kill me.
                        if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryNone") && ((int)AccessoryInfo.type == 120))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryHair") && ((int)AccessoryInfo.type == 121))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryHead") && ((int)AccessoryInfo.type == 122))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryFace") && ((int)AccessoryInfo.type == 123))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryNeck") && ((int)AccessoryInfo.type == 124))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryBody") && ((int)AccessoryInfo.type == 125))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryWaist") && ((int)AccessoryInfo.type == 126))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryLeg") && ((int)AccessoryInfo.type == 127))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryArm") && ((int)AccessoryInfo.type == 128))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryHand") && ((int)AccessoryInfo.type == 129))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else if ((kv.Key.ToString() == "ChaFileAccessory.PartsInfo.AccessoryKokan") && ((int)AccessoryInfo.type == 130))
                            Logger.Log(LogLevel.Debug, "Matched Accessory");
                        else
                            return;
                    }

                    var intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key.ToString()
                                                                              && x.Slot == kv.Value.GetMethod(structure));

                    if (intResolve != null)
                    {
                        //found a match
                        Logger.Log(LogLevel.Info, $"[UAR] Compatibility resolving {intResolve.Property} from slot {kv.Value.GetMethod(structure)} to slot {intResolve.LocalSlot}");

                        kv.Value.SetMethod(structure, intResolve.LocalSlot);
                    }
                    else
                    {
                        //No match was found
                        Logger.Log(LogLevel.Info, $"Unable to locate mod with ID: {kv.Value.GetMethod(structure)} in slot {kv.Key.ToString()}");
                    }
                }
                else
                {
                    //not resolving since we prioritize vanilla items over modded items
                    //BepInLogger.Log($"[UAR] Not resolving item due to vanilla ID range");
                    //log commented out because it causes too much spam
                }
            }

            HashSet<string> keyHashset = new HashSet<string>();

            foreach (var kv in propertyDict)
            {
                string property = $"{propertyPrefix}{kv.Key}";

                if (keyHashset.Contains(property))
                    continue;

                keyHashset.Add(property);

                if (extInfo != null)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == property);

                    if (extResolve != null)
                    {
                        //the property has external slot information 
                        var intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.AppendPropertyPrefix(propertyPrefix).CanResolve(extResolve));

                        if (intResolve != null)
                        {
                            //found a match to a corrosponding internal mod
                            Logger.Log(LogLevel.Info, $"[UAR] Resolving {extResolve.GUID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.LocalSlot}");

                            kv.Value.SetMethod(structure, intResolve.LocalSlot);
                        }
                        else
                        {
                            //we didn't find a match, check if we have the same GUID loaded

                            if (LoadedResolutionInfo.Any(x => x.GUID == extResolve.GUID))
                            {
                                //we have the GUID loaded, so the user has an outdated mod
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"[UAR] WARNING! Outdated mod detected! [{extResolve.GUID}]");
                            }
                            else
                            {
                                //did not find a match, we don't have the mod
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"[UAR] WARNING! Missing mod detected! [{extResolve.GUID}]");
                            }

                            kv.Value.SetMethod(structure, 999999); //set to an invalid ID
                        }
                    }
                    else //if (UnityEngine.Event.current.alt)
                    {
                        CompatibilityResolve(kv);
                    }
                }
                else
                {
                    CompatibilityResolve(kv);
                }
            }
        }

        private static int CurrentSlotID = 100000000;

        public static void GenerateResolutionInfo(Manifest manifest, ChaListData data)
        {
            var category = (ChaListDefine.CategoryNo)data.categoryNo;

            var propertyKeys = StructReference.CollatedStructValues.Keys.Where(x => x.Category == category).ToList();

            //BepInLogger.Log(category.ToString());
            //BepInLogger.Log(StructReference.CollatedStructValues.Count.ToString());


            foreach (var kv in data.dictList)
            {
                int newSlot = Interlocked.Increment(ref CurrentSlotID);

                //BepInLogger.Log(kv.Value[0] + " | " + newSlot);

                foreach (var propertyKey in propertyKeys)
                {
                    //BepInLogger.Log(property.Key.ToString());

                    LoadedResolutionInfo.Add(new ResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(kv.Value[0]),
                        LocalSlot = newSlot,
                        Property = propertyKey.ToString()
                    });

                    //BepInLogger.Log($"LOADED COUNT {LoadedResolutionInfo.Count}");
                }

                kv.Value[0] = newSlot.ToString();
            }

            //BepInLogger.Log($"Internal info count: {LoadedResolutionInfo.Count}");
            //foreach (ResolveInfo info in LoadedResolutionInfo)
            //    BepInLogger.Log($"Internal info: {info.ModID} : {info.Property} : {info.Slot}");
        }
    }
}
