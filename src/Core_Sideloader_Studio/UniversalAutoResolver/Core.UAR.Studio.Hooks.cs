using ExtensibleSaveFormat;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using static Sideloader.AutoResolver.StudioObjectSearch;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            internal static void ExtendedSceneLoad(string path)
            {
                PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UARExtID);

                ResolveStudioObjects(ExtendedData, ResolveType.Load);
                ResolveStudioMap(ExtendedData, ResolveType.Load);
                ResolveStudioFilter(ExtendedData, ResolveType.Load);
                ResolveStudioRamp(ExtendedData, ResolveType.Load);
                ResolveStudioBGM(ExtendedData, ResolveType.Load);
            }

            internal static void ExtendedSceneImport(string path)
            {
                PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UARExtID);
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
                            ResolveStudioObject(extResolve, Item);
                            ObjectList.Remove(NewDicKey);
                        }
                        else
                        {
                            NewDicKey = LightImportOrder.Where(x => x.Value == extResolve.ObjectOrder).Select(x => x.Key).FirstOrDefault();
                            if (ObjectList[extResolve.DicKey] is OILightInfo Light)
                            {
                                ResolveStudioObject(extResolve, Light);
                                ObjectList.Remove(NewDicKey);
                            }
                        }
                    }
                }

                //Resolve every item without extended data in case of hard mods
                foreach (ObjectInfo OI in ObjectList.Where(x => x.Value is OIItemInfo || x.Value is OILightInfo).Select(x => x.Value))
                {
                    if (OI is OIItemInfo Item)
                        ResolveStudioObject(Item);
                    else if (OI is OILightInfo Light)
                        ResolveStudioObject(Light);
                }

                //Maps and filters are not imported
                //UniversalAutoResolver.ResolveStudioMap(extData);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(OICharInfo), nameof(OICharInfo.Save))]
            internal static void OICharInfoSavePrefix(OICharInfo __instance, ref int __state)
            {
                var animationData = new PluginData();
                animationData.data = new Dictionary<string, object>();

                //Save the resolved ID to the passthrough state for use in postfix
                __state = __instance.animeInfo.no;

                if (__instance.animeInfo.no >= BaseSlotID)
                {
                    StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == __instance.animeInfo.no).FirstOrDefault();

                    animationData.data["GUID"] = extResolve.GUID;
                    animationData.data["Group"] = __instance.animeInfo.group;
                    animationData.data["Category"] = __instance.animeInfo.category;

                    ExtendedSave.SetExtendedDataById(__instance.charFile, UARExtIDStudioAnimation, animationData);

                    //Set the ID back to the original ID
                    __instance.animeInfo.no = extResolve.Slot;
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(OICharInfo), nameof(OICharInfo.Save))]
            internal static void OICharInfoSavePostfix(OICharInfo __instance, ref int __state)
            {
                //Set the ID back to the resolved ID
                if (__state >= BaseSlotID)
                    __instance.animeInfo.no = __state;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AddObjectAssist), nameof(AddObjectAssist.LoadChild), typeof(ObjectInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject))]
            internal static void AddObjectAssistLoadChild(ObjectInfo _child)
            {
                if (_child.kind == 0)//character
                {
                    OICharInfo oICharInfo = _child as OICharInfo;

                    var extData = ExtendedSave.GetExtendedDataById(oICharInfo.charFile, UARExtIDStudioAnimation);

                    if (extData == null || !extData.data.ContainsKey("GUID") || !extData.data.ContainsKey("Group") || !extData.data.ContainsKey("Category"))
                    {
                        //Sideloader.Logger.LogDebug("No sideloader animation marker found");
                    }
                    else
                    {
                        string GUID = extData.data["GUID"].ToString();
                        int Group = int.Parse(extData.data["Group"].ToString());
                        int Category = int.Parse(extData.data["Category"].ToString());

                        StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == oICharInfo.animeInfo.no && x.GUID == GUID && x.Group == Group && x.Category == Category);

                        if (intResolve == null)
                        {
                            ShowGUIDError(GUID);

                            //Set animation to T-pose
                            oICharInfo.animeInfo.no = 0;
                            oICharInfo.animeInfo.group = 0;
                            oICharInfo.animeInfo.category = 0;
                        }
                        else
                            oICharInfo.animeInfo.no = intResolve.LocalSlot;
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), "Save", typeof(string))]
            internal static void SavePrefix()
            {
                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();
                List<StudioResolveInfo> ObjectResolutionInfo = new List<StudioResolveInfo>();
                Dictionary<int, ObjectInfo> ObjectList = FindObjectInfoAndOrder(SearchType.All, typeof(OIItemInfo), out Dictionary<int, int> ItemOrder);
                Dictionary<int, int> LightOrder = FindObjectInfoOrder(SearchType.All, typeof(OILightInfo));

                foreach (ObjectInfo oi in ObjectList.Where(x => x.Value is OIItemInfo || x.Value is OILightInfo).Select(x => x.Value))
                {
                    if (oi is OIItemInfo Item && Item.no >= BaseSlotID)
                    {
                        StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == Item.no).FirstOrDefault();
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
                                Sideloader.Logger.LogDebug($"Setting [{Item.dicKey}] ID:{Item.no}->{extResolve.Slot}");
                            Traverse.Create(Item).Property("no").SetValue(extResolve.Slot);
                        }
                    }
                    else if (oi is OILightInfo Light && Light.no >= BaseSlotID)
                    {
                        StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == Light.no).FirstOrDefault();
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
                                Sideloader.Logger.LogDebug($"Setting [{Light.dicKey}] ID:{Light.no}->{extResolve.Slot}");
                            Traverse.Create(Light).Property("no").SetValue(extResolve.Slot);
                        }
                    }
                }

                //Add the extended data for items and lights, if any
                if (!ObjectResolutionInfo.IsNullOrEmpty())
                    ExtendedData.Add("itemInfo", ObjectResolutionInfo.Select(x => x.Serialize()).ToList());

                //Add the extended data for the map, if any
                int mapID = Studio.Studio.Instance.sceneInfo.map;
                if (mapID > BaseSlotID)
                {
                    StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == mapID).FirstOrDefault();
                    if (extResolve != null)
                    {
                        ExtendedData.Add("mapInfoGUID", extResolve.GUID);

                        //Set map ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Setting Map ID:{mapID}->{extResolve.Slot}");
                        Studio.Studio.Instance.sceneInfo.map = extResolve.Slot;
                    }
                }

#if KK
                //Add the extended data for the filter, if any
                int filterID = Studio.Studio.Instance.sceneInfo.aceNo;
                if (filterID > BaseSlotID)
                {
                    StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == filterID).FirstOrDefault();
                    if (extResolve != null)
                    {
                        ExtendedData.Add("filterInfoGUID", extResolve.GUID);

                        //Set filter ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Setting Filter ID:{filterID}->{extResolve.Slot}");
                        Studio.Studio.Instance.sceneInfo.aceNo = extResolve.Slot;
                    }
                }

                //Add the extended data for the ramp, if any
                int rampID = Studio.Studio.Instance.sceneInfo.rampG;
                if (rampID > BaseSlotID)
                {
                    ResolveInfo extResolve = TryGetResolutionInfo("Ramp", rampID);
                    if (extResolve != null)
                    {
                        ExtendedData.Add("rampInfoGUID", extResolve.GUID);

                        //Set ramp ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Setting Ramp ID:{rampID}->{extResolve.Slot}");
                        Studio.Studio.Instance.sceneInfo.rampG = extResolve.Slot;
                    }
                }
#endif

                //Add the extended data for the bgm, if any
                int bgmID = Studio.Studio.Instance.sceneInfo.bgmCtrl.no;
                if (bgmID > BaseSlotID)
                {
                    StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == bgmID).FirstOrDefault();
                    if (extResolve != null)
                    {
                        ExtendedData.Add("bgmInfoGUID", extResolve.GUID);

                        //Set bgm ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug("Setting BGM ID:{bgmID}->{extResolve.Slot}");
                        Studio.Studio.Instance.sceneInfo.bgmCtrl.no = extResolve.Slot;
                    }
                }

                if (ExtendedData.Count == 0)
                    //Set extended data to null to remove any that may once have existed, for example in the case of deleted objects
                    ExtendedSave.SetSceneExtendedDataById(UARExtID, null);
                else
                    //Set the extended data if any has been added
                    ExtendedSave.SetSceneExtendedDataById(UARExtID, new PluginData { data = ExtendedData });
            }

            /// <summary>
            /// Set item IDs back to the resolved ID
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new[] { typeof(string) })]
            public static void SavePostfix()
            {
                PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(UARExtID);

                ResolveStudioObjects(ExtendedData, ResolveType.Save);
                ResolveStudioMap(ExtendedData, ResolveType.Save);
                ResolveStudioFilter(ExtendedData, ResolveType.Save);
                ResolveStudioRamp(ExtendedData, ResolveType.Save);
            }
        }
    }
}