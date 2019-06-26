using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using Illusion.Elements.Xml;
using Illusion.Extensions;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.UI;
using static Sideloader.AutoResolver.StudioObjectSearch;

namespace Sideloader.AutoResolver
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
            ExtendedSave.CardBeingSaved += ExtendedCardSave;

            ExtendedSave.CoordinateBeingLoaded += ExtendedCoordinateLoad;
            ExtendedSave.CoordinateBeingSaved += ExtendedCoordinateSave;

            ExtendedSave.SceneBeingLoaded += ExtendedSceneLoad;
            ExtendedSave.SceneBeingImported += ExtendedSceneImport;

            var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader.universalautoresolver");
            harmony.PatchAll(typeof(Hooks));

            harmony.Patch(typeof(SystemButtonCtrl).GetNestedType("AmplifyColorEffectInfo", AccessTools.all).GetMethod("OnValueChangedLut", AccessTools.all),
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(OnValueChangedLutPrefix), AccessTools.all)), null);
            harmony.Patch(typeof(SystemButtonCtrl).GetNestedType("AmplifyColorEffectInfo", AccessTools.all).GetMethod("UpdateInfo", AccessTools.all), null,
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(ACEUpdateInfoPostfix), AccessTools.all)));
            harmony.Patch(typeof(SystemButtonCtrl).GetNestedType("EtcInfo", AccessTools.all).GetMethod("UpdateInfo", AccessTools.all), null,
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(ETCUpdateInfoPostfix), AccessTools.all)));
        }

        #region ChaFile

        private static void IterateCardPrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action, ChaFile file, ICollection<ResolveInfo> extInfo)
        {
            action(StructReference.ChaFileFaceProperties, file.custom.face, extInfo, "");
            action(StructReference.ChaFileBodyProperties, file.custom.body, extInfo, "");
            action(StructReference.ChaFileHairProperties, file.custom.hair, extInfo, "");
            action(StructReference.ChaFileMakeupProperties, file.custom.face.baseMakeup, extInfo, "");

            for (int i = 0; i < file.coordinate.Length; i++)
            {
                var coordinate = file.coordinate[i];
                string prefix = $"outfit{i}.";

                action(StructReference.ChaFileClothesProperties, coordinate.clothes, extInfo, prefix);

                for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
                {
                    string accPrefix = $"{prefix}accessory{acc}.";

                    action(StructReference.ChaFileAccessoryPartsInfoProperties, coordinate.accessory.parts[acc], extInfo, accPrefix);
                }
            }
        }

        private static void ExtendedCardLoad(ChaFile file)
        {
            Logger.Log(LogLevel.Debug, $"Loading card [{file.charaFileName}]");

            var extData = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID);
            List<ResolveInfo> extInfo;

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                Logger.Log(LogLevel.Debug, "No sideloader marker found");
                extInfo = null;
            }
            else
            {
                var tmpExtInfo = (object[])extData.data["info"];
                extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x)).ToList();

                Logger.Log(LogLevel.Debug, $"Sideloader marker found, external info count: {extInfo.Count}");

                if (Sideloader.DebugLogging.Value)
                {
                    foreach (ResolveInfo info in extInfo)
                        Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
                }
            }

            IterateCardPrefixes(UniversalAutoResolver.ResolveStructure, file, extInfo);
        }

        private static void ExtendedCardSave(ChaFile file)
        {
            List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

            void IterateStruct(Dictionary<CategoryProperty, StructValue<int>> dict, object obj, IEnumerable<ResolveInfo> extInfo, string propertyPrefix = "")
            {
                foreach (var kv in dict)
                {
                    int slot = kv.Value.GetMethod(obj);

                    //No need to attempt a resolution info lookup for empty accessory slots and pattern slots
                    if (slot == 0)
                        continue;

                    //Check if it's a vanilla item
                    if (slot < 100000000)
                        if (ResourceRedirector.ListLoader.InternalDataList[kv.Key.Category].ContainsKey(slot))
                            continue;

                    //For accessories, make sure we're checking the appropriate category
                    if (kv.Key.Category.ToString().Contains("ao_"))
                    {
                        ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)obj;

                        if ((int)kv.Key.Category != AccessoryInfo.type)
                        {
                            //If the current category does not match the accessory's category do not attempt a resolution info lookup
                            continue;
                        }
                    }

                    var info = UniversalAutoResolver.TryGetResolutionInfo(kv.Key.ToString(), slot);

                    if (info == null)
                        continue;

                    var newInfo = info.DeepCopy();
                    newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

                    kv.Value.SetMethod(obj, newInfo.Slot);

                    resolutionInfo.Add(newInfo);
                }
            }

            IterateCardPrefixes(IterateStruct, file, null);

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

            var tmpExtInfo = (List<byte[]>)extData.data["info"];
            var extInfo = tmpExtInfo.Select(ResolveInfo.Unserialize).ToList();

            Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count}");

            if (Sideloader.DebugLogging.Value)
            {
                foreach (ResolveInfo info in extInfo)
                    Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
            }

            void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
            {
                foreach (var kv in propertyDict)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                    if (extResolve != null)
                    {
                        kv.Value.SetMethod(structure, extResolve.LocalSlot);

                        if (Sideloader.DebugLogging.Value)
                            Logger.Log(LogLevel.Debug, $"[UAR] Resetting {extResolve.GUID}:{extResolve.Property} to internal slot {extResolve.LocalSlot}");
                    }
                }
            }

            IterateCardPrefixes(ResetStructResolveStructure, __instance, extInfo);
        }

        #endregion

        #region ChaFileCoordinate

        private static void IterateCoordinatePrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action, ChaFileCoordinate coordinate, ICollection<ResolveInfo> extInfo)
        {
            action(StructReference.ChaFileClothesProperties, coordinate.clothes, extInfo, "");

            for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
            {
                string accPrefix = $"accessory{acc}.";

                action(StructReference.ChaFileAccessoryPartsInfoProperties, coordinate.accessory.parts[acc], extInfo, accPrefix);
            }
        }

        private static void ExtendedCoordinateLoad(ChaFileCoordinate file)
        {
            Logger.Log(LogLevel.Debug, $"Loading coordinate [{file.coordinateName}]");

            var extData = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID);
            List<ResolveInfo> extInfo;

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                Logger.Log(LogLevel.Debug, "No sideloader marker found");
                extInfo = null;
            }
            else
            {
                var tmpExtInfo = (object[])extData.data["info"];
                extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x)).ToList();

                Logger.Log(LogLevel.Debug, $"Sideloader marker found, external info count: {extInfo.Count}");

                if (Sideloader.DebugLogging.Value)
                {
                    foreach (ResolveInfo info in extInfo)
                        Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
                }
            }

            IterateCoordinatePrefixes(UniversalAutoResolver.ResolveStructure, file, extInfo);
        }

        private static void ExtendedCoordinateSave(ChaFileCoordinate file)
        {
            List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

            void IterateStruct(Dictionary<CategoryProperty, StructValue<int>> dict, object obj, IEnumerable<ResolveInfo> extInfo, string propertyPrefix = "")
            {
                foreach (var kv in dict)
                {
                    int slot = kv.Value.GetMethod(obj);

                    //No need to attempt a resolution info lookup for empty accessory slots and pattern slots
                    if (slot == 0)
                        continue;

                    //Check if it's a vanilla item
                    if (slot < 100000000)
                        if (ResourceRedirector.ListLoader.InternalDataList[kv.Key.Category].ContainsKey(slot))
                            continue;

                    //For accessories, make sure we're checking the appropriate category
                    if (kv.Key.Category.ToString().Contains("ao_"))
                    {
                        ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)obj;

                        if ((int)kv.Key.Category != AccessoryInfo.type)
                        {
                            //If the current category does not match the accessory's category do not attempt a resolution info lookup
                            continue;
                        }
                    }

                    var info = UniversalAutoResolver.TryGetResolutionInfo(kv.Key.ToString(), slot);

                    if (info == null)
                        continue;

                    var newInfo = info.DeepCopy();
                    newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

                    kv.Value.SetMethod(obj, newInfo.Slot);

                    resolutionInfo.Add(newInfo);
                }
            }

            IterateCoordinatePrefixes(IterateStruct, file, null);

            ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
            {
                data = new Dictionary<string, object>
                {
                    ["info"] = resolutionInfo.Select(x => x.Serialize()).ToList()
                }
            });
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), new[] { typeof(string) })]
        public static void ChaFileCoordinateSaveFilePostHook(ChaFileCoordinate __instance, string path)
        {
            Logger.Log(LogLevel.Debug, $"Reloading coordinate [{path}]");

            var extData = ExtendedSave.GetExtendedDataById(__instance, UniversalAutoResolver.UARExtID);

            var tmpExtInfo = (List<byte[]>)extData.data["info"];
            var extInfo = tmpExtInfo.Select(ResolveInfo.Unserialize).ToList();

            Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count}");

            if (Sideloader.DebugLogging.Value)
            {
                foreach (ResolveInfo info in extInfo)
                    Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
            }

            void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
            {
                foreach (var kv in propertyDict)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                    if (extResolve != null)
                    {
                        kv.Value.SetMethod(structure, extResolve.LocalSlot);

                        if (Sideloader.DebugLogging.Value)
                            Logger.Log(LogLevel.Debug, $"[UAR] Resetting {extResolve.GUID}:{extResolve.Property} to internal slot {extResolve.LocalSlot}");
                    }
                }
            }

            IterateCoordinatePrefixes(ResetStructResolveStructure, __instance, extInfo);
        }

        #endregion

        #region Studio
        private static void ExtendedSceneLoad(string path)
        {
            PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UniversalAutoResolver.UARExtID);

            UniversalAutoResolver.ResolveStudioObjects(ExtendedData, UniversalAutoResolver.ResolveType.Load);
            UniversalAutoResolver.ResolveStudioMap(ExtendedData, UniversalAutoResolver.ResolveType.Load);
            UniversalAutoResolver.ResolveStudioFilter(ExtendedData, UniversalAutoResolver.ResolveType.Load);
            UniversalAutoResolver.ResolveStudioRamp(ExtendedData, UniversalAutoResolver.ResolveType.Load);
        }

        private static void ExtendedSceneImport(string path)
        {
            PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UniversalAutoResolver.UARExtID);
            Dictionary<int, ObjectInfo> ObjectList = FindObjectInfo(SearchType.All);

            if (ExtendedData != null && ExtendedData.data.ContainsKey("itemInfo"))
            {
                object[] tmpExtInfo = (object[])ExtendedData.data["itemInfo"];
                List<StudioResolveInfo> extInfo = tmpExtInfo.Select(x => StudioResolveInfo.Unserialize((byte[])x)).ToList();
                Dictionary<int, int> ItemImportOrder = FindObjectInfoOrder(SearchType.Import, typeof(OIItemInfo));
                Dictionary<int, int> LightImportOrder = FindObjectInfoOrder(SearchType.Import, typeof(OILightInfo));

                //Match objects from the StudioResolveInfo to objects in the scene based on the item order that was generated and saved to the scene data
                foreach (StudioResolveInfo extResolve in extInfo)
                {
                    int NewDicKey = ItemImportOrder.Where(x => x.Value == extResolve.ObjectOrder).Select(x => x.Key).FirstOrDefault();
                    if (ObjectList[NewDicKey] is OIItemInfo Item)
                    {
                        UniversalAutoResolver.ResolveStudioObject(extResolve, Item);
                        ObjectList.Remove(NewDicKey);
                    }
                    else
                    {
                        NewDicKey = LightImportOrder.Where(x => x.Value == extResolve.ObjectOrder).Select(x => x.Key).FirstOrDefault();
                        if (ObjectList[extResolve.DicKey] is OILightInfo Light)
                        {
                            UniversalAutoResolver.ResolveStudioObject(extResolve, Light);
                            ObjectList.Remove(NewDicKey);
                        }
                    }
                }
            }

            //Resolve every item without extended data in case of hard mods
            foreach (ObjectInfo OI in ObjectList.Where(x => x.Value is OIItemInfo || x.Value is OILightInfo).Select(x => x.Value))
            {
                if (OI is OIItemInfo Item)
                    UniversalAutoResolver.ResolveStudioObject(Item);
                else if (OI is OILightInfo Light)
                    UniversalAutoResolver.ResolveStudioObject(Light);
            }

            //Maps and filters are not imported
            //UniversalAutoResolver.ResolveStudioMap(extData);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), "Save", new[] { typeof(string) })]
        public static void SavePrefix()
        {
            Dictionary<string, object> ExtendedData = new Dictionary<string, object>();
            List<StudioResolveInfo> ObjectResolutionInfo = new List<StudioResolveInfo>();
            Dictionary<int, ObjectInfo> ObjectList = FindObjectInfoAndOrder(SearchType.All, typeof(OIItemInfo), out Dictionary<int, int> ItemOrder);
            Dictionary<int, int> LightOrder = FindObjectInfoOrder(SearchType.All, typeof(OILightInfo));

            foreach (ObjectInfo oi in ObjectList.Where(x => x.Value is OIItemInfo || x.Value is OILightInfo).Select(x => x.Value))
            {
                if (oi is OIItemInfo Item && Item.no >= 100000000)
                {
                    StudioResolveInfo extResolve = UniversalAutoResolver.LoadedStudioResolutionInfo.Where(x => x.LocalSlot == Item.no).FirstOrDefault();
                    if (extResolve != null)
                    {
                        StudioResolveInfo intResolve = new StudioResolveInfo
                        {
                            GUID = extResolve.GUID,
                            Slot = extResolve.Slot,
                            LocalSlot = extResolve.LocalSlot,
                            DicKey = Item.dicKey,
                            ObjectOrder = ItemOrder[Item.dicKey]
                        };
                        ObjectResolutionInfo.Add(intResolve);

                        //set item ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Logger.Log(LogLevel.Debug, $"Setting [{Item.dicKey}] ID:{Item.no}->{extResolve.Slot}");
                        Traverse.Create(Item).Property("no").SetValue(extResolve.Slot);
                    }
                }
                else if (oi is OILightInfo Light && Light.no >= 100000000)
                {
                    StudioResolveInfo extResolve = UniversalAutoResolver.LoadedStudioResolutionInfo.Where(x => x.LocalSlot == Light.no).FirstOrDefault();
                    if (extResolve != null)
                    {
                        StudioResolveInfo intResolve = new StudioResolveInfo
                        {
                            GUID = extResolve.GUID,
                            Slot = extResolve.Slot,
                            LocalSlot = extResolve.LocalSlot,
                            DicKey = Light.dicKey,
                            ObjectOrder = LightOrder[Light.dicKey]
                        };
                        ObjectResolutionInfo.Add(intResolve);

                        //Set item ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Logger.Log(LogLevel.Debug, $"Setting [{Light.dicKey}] ID:{Light.no}->{extResolve.Slot}");
                        Traverse.Create(Light).Property("no").SetValue(extResolve.Slot);
                    }
                }
            }

            //Add the extended data for items and lights, if any
            if (!ObjectResolutionInfo.IsNullOrEmpty())
                ExtendedData.Add("itemInfo", ObjectResolutionInfo.Select(x => x.Serialize()).ToList());

            //Add the extended data for the map, if any
            int mapID = Studio.Studio.Instance.sceneInfo.map;
            if (mapID > 100000000)
            {
                StudioResolveInfo extResolve = UniversalAutoResolver.LoadedStudioResolutionInfo.Where(x => x.LocalSlot == mapID).FirstOrDefault();
                if (extResolve != null)
                {
                    ExtendedData.Add("mapInfoGUID", extResolve.GUID);

                    //Set map ID back to default
                    if (Sideloader.DebugLogging.Value)
                        Logger.Log(LogLevel.Debug, $"Setting Map ID:{mapID}->{extResolve.Slot}");
                    Studio.Studio.Instance.sceneInfo.map = extResolve.Slot;
                }
            }

            //Add the extended data for the filter, if any
            int filterID = Studio.Studio.Instance.sceneInfo.aceNo;
            if (filterID > 100000000)
            {
                StudioResolveInfo extResolve = UniversalAutoResolver.LoadedStudioResolutionInfo.Where(x => x.LocalSlot == filterID).FirstOrDefault();
                if (extResolve != null)
                {
                    ExtendedData.Add("filterInfoGUID", extResolve.GUID);

                    //Set filter ID back to default
                    if (Sideloader.DebugLogging.Value)
                        Logger.Log(LogLevel.Debug, $"Setting Filter ID:{filterID}->{extResolve.Slot}");
                    Studio.Studio.Instance.sceneInfo.aceNo = extResolve.Slot;
                }
            }

            //Add the extended data for the ramp, if any
            int rampID = Studio.Studio.Instance.sceneInfo.rampG;
            if (rampID > 100000000)
            {
                ResolveInfo extResolve = UniversalAutoResolver.TryGetResolutionInfo("Ramp", rampID);
                if (extResolve != null)
                {
                    ExtendedData.Add("rampInfoGUID", extResolve.GUID);

                    //Set ramp ID back to default
                    if (Sideloader.DebugLogging.Value)
                        Logger.Log(LogLevel.Debug, $"Setting Ramp ID:{rampID}->{extResolve.Slot}");
                    Studio.Studio.Instance.sceneInfo.rampG = extResolve.Slot;
                }
            }

            if (ExtendedData.Count == 0)
                //Set extended data to null to remove any that may once have existed, for example in the case of deleted objects
                ExtendedSave.SetSceneExtendedDataById(UniversalAutoResolver.UARExtID, null);
            else
                //Set the extended data if any has been added
                ExtendedSave.SetSceneExtendedDataById(UniversalAutoResolver.UARExtID, new PluginData { data = ExtendedData });
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new[] { typeof(string) })]
        public static void SavePostfix()
        {
            //Set item IDs back to the resolved ID
            PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UniversalAutoResolver.UARExtID);

            UniversalAutoResolver.ResolveStudioObjects(ExtendedData, UniversalAutoResolver.ResolveType.Save);
            UniversalAutoResolver.ResolveStudioMap(ExtendedData, UniversalAutoResolver.ResolveType.Save);
            UniversalAutoResolver.ResolveStudioFilter(ExtendedData, UniversalAutoResolver.ResolveType.Save);
            UniversalAutoResolver.ResolveStudioRamp(ExtendedData, UniversalAutoResolver.ResolveType.Save);
        }
        /// <summary>
        /// Translate the value (selected index) to the actual ID of the filter. This allows us to save the ID to the scene.
        /// Without this, the index is saved which will be different depending on installed mods and make it impossible to save and load correctly.
        /// </summary>
        public static void OnValueChangedLutPrefix(ref int _value)
        {
            int counter = 0;
            foreach (var x in Info.Instance.dicFilterLoadInfo)
            {
                if (counter == _value)
                {
                    _value = x.Key;
                    break;
                }
                counter++;
            }
        }
        /// <summary>
        /// Called after a scene load. Find the index of the currrent filter ID and set the dropdown.
        /// </summary>
        public static void ACEUpdateInfoPostfix(object __instance)
        {
            int counter = 0;
            foreach (var x in Info.Instance.dicFilterLoadInfo)
            {
                if (x.Key == Studio.Studio.Instance.sceneInfo.aceNo)
                {
                    Dropdown dropdownLut = (Dropdown)Traverse.Create(__instance).Field("dropdownLut").GetValue();
                    dropdownLut.value = counter;
                    break;
                }
                counter++;
            }
        }
        /// <summary>
        /// Called after a scene load. Find the index of the currrent ramp ID and set the dropdown.
        /// </summary>
        public static void ETCUpdateInfoPostfix(object __instance)
        {
            int counter = 0;
            foreach (var x in ResourceRedirector.ListLoader.InternalDataList[ChaListDefine.CategoryNo.mt_ramp])
            {
                if (x.Key == Studio.Studio.Instance.sceneInfo.rampG)
                {
                    Dropdown dropdownRamp = (Dropdown)Traverse.Create(__instance).Field("dropdownRamp").GetValue();
                    dropdownRamp.value = counter;
                    break;
                }
                counter++;
            }
        }
        #endregion

        #region Ramp
        [HarmonyPrefix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
        public static void XMLWritePrefix(Control __instance, ref int __state)
        {
            __state = -1;
            foreach (Data data in __instance.Datas)
                if (data is Config.EtceteraSystem etceteraSystem)
                    if (etceteraSystem.rampId >= 100000000)
                    {
                        ResolveInfo RampResolveInfo = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == etceteraSystem.rampId);
                        if (RampResolveInfo == null)
                        {
                            //ID is a sideloader ID but no resolve info found, set it to the default
                            __state = 1;
                            etceteraSystem.rampId = 1;
                        }
                        else
                        {
                            //Switch out the resolved ID for the original
                            __state = etceteraSystem.rampId;
                            etceteraSystem.rampId = RampResolveInfo.Slot;
                        }
                    }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
        public static void XMLWritePostfix(Control __instance, ref int __state)
        {
            int rampId = __state;
            if (rampId >= 100000000)
                foreach (Data data in __instance.Datas)
                    if (data is Config.EtceteraSystem etceteraSystem)
                    {
                        ResolveInfo RampResolveInfo = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == rampId);
                        if (RampResolveInfo != null)
                        {
                            //Restore the resolved ID
                            etceteraSystem.rampId = RampResolveInfo.LocalSlot;

                            var xmlDoc = XDocument.Load("UserData/config/system.xml");
                            xmlDoc.Element("System").Element("Etc").Element("rampId").AddAfterSelf(new XElement("rampGUID", RampResolveInfo.GUID));
                            xmlDoc.Save("UserData/config/system.xml");
                        }
                    }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Read))]
        public static void XMLReadPostfix(Control __instance)
        {
            foreach (Data data in __instance.Datas)
                if (data is Config.EtceteraSystem etceteraSystem)
                    if (etceteraSystem.rampId >= 100000000) //Saved with a resolved ID, reset it to default
                        etceteraSystem.rampId = 1;
                    else
                    {
                        var xmlDoc = XDocument.Load("UserData/config/system.xml");
                        string rampGUID = xmlDoc.Element("System").Element("Etc").Element("rampGUID")?.Value;
                        if (!rampGUID.IsNullOrWhiteSpace())
                        {
                            ResolveInfo RampResolveInfo = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == etceteraSystem.rampId);
                            if (RampResolveInfo == null) //Missing mod, reset ID to default
                                etceteraSystem.rampId = 1;
                            else //Restore the resolved ID
                                etceteraSystem.rampId = RampResolveInfo.LocalSlot;
                        }
                    }
        }
        //Studio
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Init))]
        public static void SceneInfoInit(SceneInfo __instance)
        {
            var xmlDoc = XDocument.Load("UserData/config/system.xml");
            string rampGUID = xmlDoc.Element("System").Element("Etc").Element("rampGUID")?.Value;
            string rampIDXML = xmlDoc.Element("System").Element("Etc").Element("rampId")?.Value;
            if (!rampGUID.IsNullOrWhiteSpace() && !rampIDXML.IsNullOrWhiteSpace() && int.TryParse(rampIDXML, out int rampID))
            {
                ResolveInfo RampResolveInfo = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == rampID);
                if (RampResolveInfo == null) //Missing mod, reset ID to default
                    __instance.rampG = 1;
                else //Restore the resolved ID
                    __instance.rampG = RampResolveInfo.LocalSlot;
            }
        }
        #endregion
    }
}