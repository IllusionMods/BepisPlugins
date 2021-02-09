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

                Dictionary<int, int> ItemImportOrder = FindObjectInfoOrder(SearchType.Import, typeof(OIItemInfo));

                //Resolve patterns on items
                if (ExtendedData != null && ExtendedData.data.ContainsKey("patternInfo"))
                {
                    List<StudioPatternResolveInfo> extPatternInfo = ((object[])ExtendedData.data["patternInfo"]).Select(x => StudioPatternResolveInfo.Deserialize((byte[])x)).ToList();

                    foreach (StudioPatternResolveInfo extPatternResolve in extPatternInfo)
                    {
                        int NewDicKey = ItemImportOrder.Where(x => x.Value == extPatternResolve.ObjectOrder).Select(x => x.Key).FirstOrDefault();
                        if (ObjectList[NewDicKey] is OIItemInfo Item)
                            ResolveStudioObjectPattern(extPatternResolve, Item);
                    }
                }

                if (ExtendedData != null && ExtendedData.data.ContainsKey("itemInfo"))
                {
                    List<StudioResolveInfo> extInfo = ((object[])ExtendedData.data["itemInfo"]).Select(x => StudioResolveInfo.Deserialize((byte[])x)).ToList();
                    Dictionary<int, int> LightImportOrder = FindObjectInfoOrder(SearchType.Import, typeof(OILightInfo));
                    Dictionary<int, int> CharImportOrder = FindObjectInfoOrder(SearchType.Import, typeof(OICharInfo));

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
                            if (ObjectList[NewDicKey] is OILightInfo Light)
                            {
                                ResolveStudioObject(extResolve, Light);
                                ObjectList.Remove(NewDicKey);
                            }
                            else
                            {
                                NewDicKey = CharImportOrder.Where(x => x.Value == extResolve.ObjectOrder).Select(x => x.Key).FirstOrDefault();
                                if (ObjectList[NewDicKey] is OICharInfo CharInfo)
                                {
                                    ResolveStudioObject(extResolve, CharInfo);
                                    ObjectList.Remove(NewDicKey);
                                }
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

            /// <summary>
            /// Before the scene saves, go through every item, map, BGM, etc. in the scene, create extended save data with the GUID and other relevant info,
            /// and restore the IDs back to the original, non-resolved ID for hard mod compatibility
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), "Save", typeof(string))]
            private static void SavePrefix()
            {
                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();
                List<StudioResolveInfo> ObjectResolutionInfo = new List<StudioResolveInfo>();
                List<StudioPatternResolveInfo> PatternResolutionInfo = new List<StudioPatternResolveInfo>();
                Dictionary<int, ObjectInfo> ObjectList = FindObjectInfoAndOrder(SearchType.All, typeof(OIItemInfo), out Dictionary<int, int> ItemOrder);
                Dictionary<int, int> LightOrder = FindObjectInfoOrder(SearchType.All, typeof(OILightInfo));
                Dictionary<int, int> CharOrder = FindObjectInfoOrder(SearchType.All, typeof(OICharInfo));

                foreach (ObjectInfo oi in ObjectList.Select(x => x.Value))
                {
                    if (oi is OIItemInfo Item)
                    {
                        //Resolve the IDs of any patterns applied to the item
                        StudioPatternResolveInfo studioPatternResolveInfo = new StudioPatternResolveInfo
                        {
                            DicKey = Item.dicKey,
                            ObjectOrder = ItemOrder[Item.dicKey],
                            ObjectPatternInfo = new Dictionary<int, StudioPatternResolveInfo.PatternInfo>()
                        };
#if KK
                        for (int i = 0; i < Item.pattern.Length; i++)
                        {
                            if (Item.pattern[i].key >= BaseSlotID)
                            {
                                var intResolve = TryGetResolutionInfo(ChaListDefine.CategoryNo.mt_pattern, Item.pattern[i].key);

                                if (intResolve != null)
                                {
                                    studioPatternResolveInfo.ObjectPatternInfo[i] = new StudioPatternResolveInfo.PatternInfo
                                    {
                                        GUID = intResolve.GUID,
                                        Slot = intResolve.Slot,
                                        LocalSlot = Item.pattern[i].key
                                    };

                                    //Set pattern ID back to original non-resolved ID
                                    if (Sideloader.DebugLogging.Value)
                                        Sideloader.Logger.LogDebug($"Setting [{Item.dicKey}] ID:{Item.pattern[i].key}->{intResolve.Slot}");
                                    Item.pattern[i].key = intResolve.Slot;
                                }
                            }
                        }
#elif AI || HS2
                        for (int i = 0; i < Item.colors.Length; i++)
                        {
                            if (Item.colors[i].pattern.key >= BaseSlotID)
                            {
                                var intResolve = TryGetResolutionInfo(AIChara.ChaListDefine.CategoryNo.st_pattern, Item.colors[i].pattern.key);

                                if (intResolve != null)
                                {
                                    studioPatternResolveInfo.ObjectPatternInfo[i] = new StudioPatternResolveInfo.PatternInfo
                                    {
                                        GUID = intResolve.GUID,
                                        Slot = intResolve.Slot,
                                        LocalSlot = Item.colors[i].pattern.key
                                    };

                                    //Set pattern ID back to original non-resolved ID
                                    if (Sideloader.DebugLogging.Value)
                                        Sideloader.Logger.LogDebug($"Setting [{Item.dicKey}] ID:{Item.colors[i].pattern.key}->{intResolve.Slot}");
                                    Item.colors[i].pattern.key = intResolve.Slot;
                                }
                            }
                        }
#endif
                        if (studioPatternResolveInfo.ObjectPatternInfo.Count > 0)
                            PatternResolutionInfo.Add(studioPatternResolveInfo);

                        if (Item.no >= BaseSlotID)
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

                                //Set item ID back to original non-resolved ID
                                if (Sideloader.DebugLogging.Value)
                                    Sideloader.Logger.LogDebug($"Setting [{Item.dicKey}] ID:{Item.no}->{extResolve.Slot}");
                                Traverse.Create(Item).Property("no").SetValue(extResolve.Slot);
                            }
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

                            //Set item ID back to original non-resolved ID
                            if (Sideloader.DebugLogging.Value)
                                Sideloader.Logger.LogDebug($"Setting [{Light.dicKey}] ID:{Light.no}->{extResolve.Slot}");
                            Traverse.Create(Light).Property("no").SetValue(extResolve.Slot);
                        }
                    }
                    else if (oi is OICharInfo CharInfo && CharInfo.animeInfo.no >= BaseSlotID)
                    {
                        //Save the animation data for the character
                        StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == CharInfo.animeInfo.no).FirstOrDefault();
                        if (extResolve != null)
                        {
                            StudioResolveInfo intResolve = new StudioResolveInfo
                            {
                                GUID = extResolve.GUID,
                                Slot = extResolve.Slot,
                                LocalSlot = extResolve.LocalSlot,
                                Group = extResolve.Group,
                                Category = extResolve.Category,
                                DicKey = CharInfo.dicKey,
                                ObjectOrder = CharOrder[CharInfo.dicKey]
                            };
                            ObjectResolutionInfo.Add(intResolve);

                            //Set animation ID back to original non-resolved ID
                            if (Sideloader.DebugLogging.Value)
                                Sideloader.Logger.LogDebug($"Setting [{CharInfo.dicKey}] AnimationID:{CharInfo.animeInfo.no}->{extResolve.Slot}");
                            CharInfo.animeInfo.no = extResolve.Slot;
                        }
                    }
                }

                //Add the extended data for items and lights, if any
                if (!ObjectResolutionInfo.IsNullOrEmpty())
                    ExtendedData.Add("itemInfo", ObjectResolutionInfo.Select(x => x.Serialize()).ToList());

                //Add the extended data for patterns, if any
                if (!PatternResolutionInfo.IsNullOrEmpty())
                    ExtendedData.Add("patternInfo", PatternResolutionInfo.Select(x => x.Serialize()).ToList());

                //Add the extended data for the map, if any
                int mapID = GetMapID();
                if (mapID > BaseSlotID)
                {
                    StudioResolveInfo extResolve = LoadedStudioResolutionInfo.Where(x => x.LocalSlot == mapID).FirstOrDefault();
                    if (extResolve != null)
                    {
                        ExtendedData.Add("mapInfoGUID", extResolve.GUID);

                        //Set map ID back to default
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Setting Map ID:{mapID}->{extResolve.Slot}");
                        SetMapID(extResolve.Slot);
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
                            Sideloader.Logger.LogDebug($"Setting BGM ID:{bgmID}->{extResolve.Slot}");
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
            private static void SavePostfix()
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