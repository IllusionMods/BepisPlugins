using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Elements.Xml;
using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.UI;
using static Sideloader.AutoResolver.StudioObjectSearch;

namespace Sideloader.AutoResolver
{
    public static partial class Hooks
    {

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
                List<StudioResolveInfo> extInfo = tmpExtInfo.Select(x => StudioResolveInfo.Deserialize((byte[])x)).ToList();
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

        [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), "Save", typeof(string))]
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
                            Sideloader.Logger.Log(LogLevel.Debug, $"Setting [{Item.dicKey}] ID:{Item.no}->{extResolve.Slot}");
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
                            Sideloader.Logger.Log(LogLevel.Debug, $"Setting [{Light.dicKey}] ID:{Light.no}->{extResolve.Slot}");
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
                        Sideloader.Logger.Log(LogLevel.Debug, $"Setting Map ID:{mapID}->{extResolve.Slot}");
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
                        Sideloader.Logger.Log(LogLevel.Debug, $"Setting Filter ID:{filterID}->{extResolve.Slot}");
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
                        Sideloader.Logger.Log(LogLevel.Debug, $"Setting Ramp ID:{rampID}->{extResolve.Slot}");
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

        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", typeof(string))]
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