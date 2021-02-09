using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using Sideloader.ListLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if AI || HS2
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
                var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

                ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
                ExtendedSave.CardBeingSaved += ExtendedCardSave;

                ExtendedSave.CoordinateBeingLoaded += ExtendedCoordinateLoad;
                ExtendedSave.CoordinateBeingSaved += ExtendedCoordinateSave;

#if EC
                ExtendedSave.CardBeingImported += ExtendedCardImport;
                ExtendedSave.CoordinateBeingImported += ExtendedCoordinateImport;
#else
                ExtendedSave.SceneBeingLoaded += ExtendedSceneLoad;
                ExtendedSave.SceneBeingImported += ExtendedSceneImport;
#endif

#if KK
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
                string cardName = file.charaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = file.parameter?.fullname?.Trim();
                Sideloader.Logger.LogDebug($"Loading card [{cardName}]");

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

#if AI || HS2
                //Resolve the bundleID to the same ID as the hair
                foreach (var hairPart in file.custom.hair.parts)
                    if (hairPart.id > BaseSlotID)
                        hairPart.bundleId = hairPart.id;
#endif
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
#elif AI || HS2
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

#if AI || HS2
                //Resolve the bundleID to the same ID as the hair
                foreach (var hairPart in file.custom.hair.parts)
                    hairPart.bundleId = hairPart.id;
#endif
            }

#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            private static void ChaFileSaveFilePostHook(ChaFile __instance)
            {
                if (DoingImport) return;

                string cardName = __instance.charaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = __instance.parameter?.fullname?.Trim();
                Sideloader.Logger.LogDebug($"Reloading card [{cardName}]");

                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtID);

                // Some old ChaFile cards has object[] for "info"

                List<ResolveInfo> extInfo;

                if (extData.data["info"] is List<byte[]> lstByte)
                {
                    extInfo = lstByte.Select(ResolveInfo.Deserialize).ToList();
                }
                else if (extData.data["info"] is object[] objArray)
                {
                    extInfo = objArray.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();
                }
                else
                {
                    Sideloader.Logger.LogError("Unknown data type:" + (extData.data["info"]).GetType());
                    return;
                }

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
                        var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key}");

                        if (extResolve != null)
                            kv.Value.SetMethod(structure, extResolve.LocalSlot);
                    }
                }

                IterateCardPrefixes(ResetStructResolveStructure, __instance, extInfo);

#if AI || HS2
                //Resolve the bundleID to the same ID as the hair
                foreach (var hairPart in __instance.custom.hair.parts)
                    if (hairPart.id > BaseSlotID)
                        hairPart.bundleId = hairPart.id;
#endif
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
#elif AI || HS2
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
            private static void ChaFileCoordinateSaveFilePostHook(ChaFileCoordinate __instance, string path)
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
                        var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key}");

                        if (extResolve != null)
                            kv.Value.SetMethod(structure, extResolve.LocalSlot);
                    }
                }

                IterateCoordinatePrefixes(ResetStructResolveStructure, __instance, extInfo);
            }

            #endregion

#if AI || HS2
            /// <summary>
            /// Find the head preset data
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            private static void LoadFacePresetPrefix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                __state = null;
                int headID = __instance.custom.face.headId;
                if (headID >= BaseSlotID)
                {
                    ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                    ListInfoBase listInfo = chaListCtrl.GetListInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    string preset = listInfo.GetInfo(ChaListDefine.KeyType.Preset);

                    var resolveinfo = TryGetResolutionInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    if (resolveinfo == null) return;

                    var headPresetInfo = TryGetHeadPresetInfo(resolveinfo.Slot, resolveinfo.GUID, preset);
                    __state = headPresetInfo;
                }
                else
                {
                    ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                    ListInfoBase listInfo = chaListCtrl.GetListInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    string preset = listInfo.GetInfo(ChaListDefine.KeyType.Preset);

                    var headPresetInfo = TryGetHeadPresetInfo(headID, null, preset);
                    __state = headPresetInfo;
                }
            }
            /// <summary>
            /// Use the head preset data to resolve the IDs
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            internal static void LoadFacePresetPostfix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                if (__state == null) return;

                List<ResolveInfo> faceResolveInfos = new List<ResolveInfo>();
                List<ResolveInfo> makeupResolveInfos = new List<ResolveInfo>();
                Dictionary<CategoryProperty, StructValue<int>> structref = __instance.parameter.sex == 0 ? StructReference.ChaFileFacePropertiesMale : StructReference.ChaFileFacePropertiesFemale;

                foreach (var property in structref)
                {
                    if (__state.FaceData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.custom.face), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.FaceData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.FaceData[property.Key.Property]);
                    else
                        faceResolveInfos.Add(resolveinfo);
                }

                foreach (var property in StructReference.ChaFileMakeupProperties)
                {
                    if (__state.MakeupData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.custom.face.makeup), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.MakeupData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.MakeupData[property.Key.Property]);
                    else
                        makeupResolveInfos.Add(resolveinfo);
                }

                ResolveStructure(structref, __instance.custom.face, faceResolveInfos);
                ResolveStructure(StructReference.ChaFileMakeupProperties, __instance.custom.face.makeup, makeupResolveInfos);
            }
#endif
        }
    }
}
