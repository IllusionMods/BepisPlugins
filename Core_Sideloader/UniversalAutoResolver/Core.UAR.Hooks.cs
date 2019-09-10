using BepInEx.Harmony;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using Sideloader.ListLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if AI
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// A flag for disabling certain events when importing KK cards to EC. Should always be set to false in InstallHooks for KK and always remain false.
            /// </summary>
            private static bool DoingImport = true;

            internal static void InstallHooks()
            {
                var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));

                ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
                ExtendedSave.CardBeingSaved += ExtendedCardSave;

                ExtendedSave.CoordinateBeingLoaded += ExtendedCoordinateLoad;
                ExtendedSave.CoordinateBeingSaved += ExtendedCoordinateSave;

#if EC
                ExtendedSave.CardBeingImported += ExtendedCardImport;
                ExtendedSave.CoordinateBeingImported += ExtendedCoordinateImport;
#elif KK
                ExtendedSave.SceneBeingLoaded += ExtendedSceneLoad;
                ExtendedSave.SceneBeingImported += ExtendedSceneImport;

                harmony.Patch(typeof(Studio.SystemButtonCtrl).GetNestedType("AmplifyColorEffectInfo", AccessTools.all).GetMethod("OnValueChangedLut", AccessTools.all),
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(OnValueChangedLutPrefix), AccessTools.all)), null);
                harmony.Patch(typeof(Studio.SystemButtonCtrl).GetNestedType("AmplifyColorEffectInfo", AccessTools.all).GetMethod("UpdateInfo", AccessTools.all), null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(ACEUpdateInfoPostfix), AccessTools.all)));
                harmony.Patch(typeof(Studio.SystemButtonCtrl).GetNestedType("EtcInfo", AccessTools.all).GetMethod("UpdateInfo", AccessTools.all), null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(ETCUpdateInfoPostfix), AccessTools.all)));
#endif

#if !EC
                DoingImport = false;
#endif
            }

            #region ChaFile

            internal static void ExtendedCardLoad(ChaFile file)
            {
                Sideloader.Logger.LogDebug($"Loading card [{file.charaFileName}]");

                var extData = ExtendedSave.GetExtendedDataById(file, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(file, UARExtID);
                List<ResolveInfo> extInfo;

                if (extData == null || !extData.data.ContainsKey("info"))
                {
                    Sideloader.Logger.LogDebug("No sideloader marker found");
                    extInfo = null;
                }
                else
                {
                    var tmpExtInfo = (object[])extData.data["info"];
                    extInfo = tmpExtInfo.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();

                    Sideloader.Logger.LogDebug($"Sideloader marker found, external info count: {extInfo.Count}");

                    if (Sideloader.DebugLogging.Value)
                    {
                        foreach (ResolveInfo info in extInfo)
                            Sideloader.Logger.LogDebug($"External info: {info.GUID} : {info.Property} : {info.Slot}");
                    }
                }

                IterateCardPrefixes(ResolveStructure, file, extInfo);
            }

            internal static void ExtendedCardSave(ChaFile file)
            {
                if (DoingImport) return;

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
                        if (slot < BaseSlotID)
#if KK || EC
                            if (Lists.InternalDataList[kv.Key.Category].ContainsKey(slot))
#elif AI
                            if (Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(slot))
#endif
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

                        var info = TryGetResolutionInfo(kv.Key.ToString(), slot);

                        if (info == null)
                            continue;

                        var newInfo = info.DeepCopy();
                        newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

                        kv.Value.SetMethod(obj, newInfo.Slot);

                        resolutionInfo.Add(newInfo);
                    }
                }

                IterateCardPrefixes(IterateStruct, file, null);

                ExtendedSave.SetExtendedDataById(file, UARExtID, new PluginData
                {
                    data = new Dictionary<string, object>
                    {
                        ["info"] = resolutionInfo.Select(x => x.Serialize()).ToList()
                    }
                });
            }

#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            internal static void ChaFileSaveFilePostHook(ChaFile __instance)
            {
                if (DoingImport) return;

                Sideloader.Logger.LogDebug($"Reloading card [{__instance.charaFileName}]");

                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtID);

                var tmpExtInfo = (List<byte[]>)extData.data["info"];
                var extInfo = tmpExtInfo.Select(ResolveInfo.Deserialize).ToList();

                Sideloader.Logger.LogDebug($"External info count: {extInfo.Count}");

                if (Sideloader.DebugLogging.Value)
                {
                    foreach (ResolveInfo info in extInfo)
                        Sideloader.Logger.LogDebug($"External info: {info.GUID} : {info.Property} : {info.Slot}");
                }

                void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
                {
                    foreach (var kv in propertyDict)
                    {
                        var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                        if (extResolve != null)
                            kv.Value.SetMethod(structure, extResolve.LocalSlot);
                    }
                }

                IterateCardPrefixes(ResetStructResolveStructure, __instance, extInfo);
            }

            #endregion

            #region ChaFileCoordinate

            internal static void ExtendedCoordinateLoad(ChaFileCoordinate file)
            {
                Sideloader.Logger.LogDebug($"Loading coordinate [{file.coordinateName}]");

                var extData = ExtendedSave.GetExtendedDataById(file, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(file, UARExtID);
                List<ResolveInfo> extInfo;

                if (extData == null || !extData.data.ContainsKey("info"))
                {
                    Sideloader.Logger.LogDebug("No sideloader marker found");
                    extInfo = null;
                }
                else
                {
                    var tmpExtInfo = (object[])extData.data["info"];
                    extInfo = tmpExtInfo.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();

                    Sideloader.Logger.LogDebug($"Sideloader marker found, external info count: {extInfo.Count}");

                    if (Sideloader.DebugLogging.Value)
                    {
                        foreach (ResolveInfo info in extInfo)
                            Sideloader.Logger.LogDebug($"External info: {info.GUID} : {info.Property} : {info.Slot}");
                    }
                }

                IterateCoordinatePrefixes(ResolveStructure, file, extInfo);
            }

            internal static void ExtendedCoordinateSave(ChaFileCoordinate file)
            {
                if (DoingImport) return;

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
                        if (slot < BaseSlotID)
#if KK || EC
                            if (Lists.InternalDataList[kv.Key.Category].ContainsKey(slot))
#elif AI
                            if (Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(slot))
#endif
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

                        var info = TryGetResolutionInfo(kv.Key.ToString(), slot);

                        if (info == null)
                            continue;

                        var newInfo = info.DeepCopy();
                        newInfo.Property = $"{propertyPrefix}{newInfo.Property}";

                        kv.Value.SetMethod(obj, newInfo.Slot);

                        resolutionInfo.Add(newInfo);
                    }
                }

                IterateCoordinatePrefixes(IterateStruct, file, null);

                ExtendedSave.SetExtendedDataById(file, UARExtID, new PluginData
                {
                    data = new Dictionary<string, object>
                    {
                        ["info"] = resolutionInfo.Select(x => x.Serialize()).ToList()
                    }
                });
            }

#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string), typeof(int))]
#endif
            internal static void ChaFileCoordinateSaveFilePostHook(ChaFileCoordinate __instance, string path)
            {
                if (DoingImport) return;

                Sideloader.Logger.LogDebug($"Reloading coordinate [{path}]");

                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtID);

                var tmpExtInfo = (List<byte[]>)extData.data["info"];
                var extInfo = tmpExtInfo.Select(ResolveInfo.Deserialize).ToList();

                Sideloader.Logger.LogDebug($"External info count: {extInfo.Count}");

                if (Sideloader.DebugLogging.Value)
                {
                    foreach (ResolveInfo info in extInfo)
                        Sideloader.Logger.LogDebug($"External info: {info.GUID} : {info.Property} : {info.Slot}");
                }

                void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
                {
                    foreach (var kv in propertyDict)
                    {
                        var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key.ToString()}");

                        if (extResolve != null)
                            kv.Value.SetMethod(structure, extResolve.LocalSlot);
                    }
                }

                IterateCoordinatePrefixes(ResetStructResolveStructure, __instance, extInfo);
            }

            #endregion

        }
    }
}