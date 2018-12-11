using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using ExtensibleSaveFormat;
using Harmony;
using Illusion.Extensions;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            harmony.Patch(typeof(Studio.MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("InitFileList", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPreHook), BindingFlags.Static | BindingFlags.Public)),
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPostHook), BindingFlags.Static | BindingFlags.Public)));
        }

        public static bool IsResolving { get; set; } = true;

        #region ChaFile

        private static void IterateCardPrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, IEnumerable<ResolveInfo>, string> action, ChaFile file, IEnumerable<ResolveInfo> extInfo)
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
            if (!IsResolving)
                return;

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

                Logger.Log(LogLevel.Debug, "Sideloader marker found");
                Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count}");
                foreach (ResolveInfo info in extInfo)
                    Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
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
            foreach (ResolveInfo info in extInfo)
                Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");

            void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
            {
                foreach (var kv in propertyDict)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                    if (extResolve != null)
                    {
                        kv.Value.SetMethod(structure, extResolve.LocalSlot);

                        Logger.Log(LogLevel.Debug, $"[UAR] Resetting {extResolve.GUID}:{extResolve.Property} to internal slot {extResolve.LocalSlot}");
                    }
                }
            }

            IterateCardPrefixes(ResetStructResolveStructure, __instance, extInfo);
        }

        #endregion

        #region ChaFileCoordinate

        private static void IterateCoordinatePrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, IEnumerable<ResolveInfo>, string> action, ChaFileCoordinate coordinate, IEnumerable<ResolveInfo> extInfo)
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
            if (!IsResolving)
                return;

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

                Logger.Log(LogLevel.Debug, "Sideloader marker found");
                Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count}");
                foreach (ResolveInfo info in extInfo)
                    Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
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
            var extInfo = tmpExtInfo.Select(ResolveInfo.Unserialize);

            Logger.Log(LogLevel.Debug, $"External info count: {extInfo.Count()}");
            foreach (ResolveInfo info in extInfo)
                Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");

            void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
            {
                foreach (var kv in propertyDict)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                    if (extResolve != null)
                    {
                        kv.Value.SetMethod(structure, extResolve.LocalSlot);

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

            //Maps are not imported
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
                        //Logger.Log(LogLevel.Info, $"Setting [{item.dicKey}] ID:{item.no}->{extResolve.Slot}");
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
                            ObjectOrder = ItemOrder[Light.dicKey]
                        };
                        ObjectResolutionInfo.Add(intResolve);

                        //Set item ID back to default
                        //Logger.Log(LogLevel.Info, $"Setting [{item.dicKey}] ID:{item.no}->{extResolve.Slot}");
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
                    //Logger.Log(LogLevel.Info, $"Setting Map ID:{mapID}->{extResolve.Slot}");
                    Studio.Studio.Instance.sceneInfo.map = extResolve.Slot;
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
        }
        #endregion

        #region Resolving Override Hooks
        //Prevent resolving when loading the list of characters in Chara Maker since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePreHook()
        {
            IsResolving = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePostHook()
        {
            IsResolving = true;
        }
        //Prevent resolving when loading the list of coordinates in Chara Maker since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
        public static void CustomCoordinatePreHook()
        {
            IsResolving = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
        public static void CustomCoordinatePostHook()
        {
            IsResolving = true;
        }
        //Prevent resolving when loading the list of characters in Studio since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitFemaleList")]
        public static void StudioFemaleListPreHook()
        {
            IsResolving = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CharaList), "InitFemaleList")]
        public static void StudioFemaleListPostHook()
        {
            IsResolving = true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitMaleList")]
        public static void StudioMaleListPreHook()
        {
            IsResolving = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CharaList), "InitMaleList")]
        public static void StudioMaleListPostHook()
        {
            IsResolving = true;
        }
        //Prevent resolving when loading the list of coordinates in Studio since it is irrelevant here
        public static void StudioCoordinateListPreHook()
        {
            IsResolving = false;
        }
        public static void StudioCoordinateListPostHook()
        {
            IsResolving = true;
        }
        #endregion
    }
}