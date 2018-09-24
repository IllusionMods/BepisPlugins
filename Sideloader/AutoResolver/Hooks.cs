using System;
using ExtensibleSaveFormat;
using Harmony;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Illusion.Extensions;

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

            var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader.universalautoresolver");
            harmony.PatchAll(typeof(Hooks));
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

        #region CustomScnene Load Hooks

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

        #endregion
    }
}