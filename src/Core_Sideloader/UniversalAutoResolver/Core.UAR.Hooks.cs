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
using CharaCustom;
#endif
#if !EC
using Studio;
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
            private static bool DoingImport;

            internal static void InstallHooks()
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), "Sideloader.UniversalAutoResolver");

                ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
                ExtendedSave.CardBeingSaved += ExtendedCardSave;

                ExtendedSave.CoordinateBeingLoaded += ExtendedCoordinateLoad;
                ExtendedSave.CoordinateBeingSaved += ExtendedCoordinateSave;

#if EC
                ExtendedSave.CardBeingImported += ExtendedCardImport;
                ExtendedSave.CoordinateBeingImported += ExtendedCoordinateImport;
#elif KKS
                ExtendedSave.CardBeingImported += ExtendedCardImport;
#endif

#if !EC
                ExtendedSave.SceneBeingLoaded += ExtendedSceneLoad;
                ExtendedSave.SceneBeingImported += ExtendedSceneImport;
#endif

#if EC
                DoingImport = true;
#elif KKS
                DoingImport = !BepisPlugins.Constants.InsideStudio;
#else
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
#if KK || EC || KKS
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

#if KK || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool), typeof(int))]
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
#if KK || EC || KKS
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

#if KK || KKS
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
                        ShowGUIDError(__state.FaceData[property.Key.Property], null, null, null);
                    else
                        faceResolveInfos.Add(resolveinfo);
                }

                foreach (var property in StructReference.ChaFileMakeupProperties)
                {
                    if (__state.MakeupData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.custom.face.makeup), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.MakeupData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.MakeupData[property.Key.Property], null, null, null);
                    else
                        makeupResolveInfos.Add(resolveinfo);
                }

                ResolveStructure(structref, __instance.custom.face, faceResolveInfos);
                ResolveStructure(StructReference.ChaFileMakeupProperties, __instance.custom.face.makeup, makeupResolveInfos);
            }
#endif

#if KK || KKS
            /// <summary>
            /// Translate the value (selected index) to the actual ID of the filter. This allows us to save the ID to the scene.
            /// Without this, the index is saved which will be different depending on installed mods and make it impossible to save and load correctly.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(SystemButtonCtrl.AmplifyColorEffectInfo), nameof(SystemButtonCtrl.AmplifyColorEffectInfo.OnValueChangedLut))]
            internal static void OnValueChangedLutPrefix(ref int _value)
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
            [HarmonyPostfix, HarmonyPatch(typeof(SystemButtonCtrl.AmplifyColorEffectInfo), nameof(SystemButtonCtrl.AmplifyColorEffectInfo.UpdateInfo))]
            internal static void ACEUpdateInfoPostfix(SystemButtonCtrl.AmplifyColorEffectInfo __instance)
            {
                int counter = 0;
                foreach (var x in Info.Instance.dicFilterLoadInfo)
                {
                    if (x.Key == Studio.Studio.Instance.sceneInfo.aceNo)
                    {
                        __instance.dropdownLut.value = counter;
                        break;
                    }
                    counter++;
                }
            }

            /// <summary>
            /// Called after a scene load. Find the index of the currrent ramp ID and set the dropdown.
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(SystemButtonCtrl.EtcInfo), nameof(SystemButtonCtrl.EtcInfo.UpdateInfo))]
            internal static void ETCUpdateInfoPostfix(SystemButtonCtrl.EtcInfo __instance)
            {
                int counter = 0;
                foreach (var x in Lists.InternalDataList[ChaListDefine.CategoryNo.mt_ramp])
                {
                    if (x.Key == Studio.Studio.Instance.sceneInfo.rampG)
                    {
                        __instance.dropdownRamp.value = counter;
                        break;
                    }
                    counter++;
                }
            }
#endif

#if AI || HS2

            // Need to differentiate between Male/Female Categories in clothing items.
            // With Character cards, easy, with coordinates...no way to tell.
            // So we'll start storing the related sex of the coordinate card with the clothes data for later use.
            // This won't fix old coordinate cards but will at least fix new ones.

            internal static int RetrieveSexOnClothes(ChaFileClothes clothes)
            {
                if (clothes != null && clothes.TryGetExtendedDataById(Sideloader.GUID, out PluginData data))
                {
                    if (data.data != null && data.data.TryGetValue("CoordinateSex", out object sexData))
                    {
                        return (byte)sexData;
                    }
                }
                return -1;
            }

            private static void StoreSexOnClothes(byte sex, ChaFileClothes clothes)
            {
                PluginData data;
                if (!clothes.TryGetExtendedDataById(Sideloader.GUID, out data))
                {
                    data = new PluginData();                    
                }
                data.data["CoordinateSex"] = sex;
                clothes.SetExtendedDataById(Sideloader.GUID, data);
            }

            // Store Sex on Coordinate for Later Use
            [HarmonyPrefix, HarmonyPatch(typeof(CvsC_CreateCoordinateFile), nameof(CvsC_CreateCoordinateFile.CreateCoordinateFile))]
            internal static void ChaFileConstructor()
            {
                ChaControl chaCtrl = Singleton<CustomBase>.Instance?.chaCtrl;
                if (chaCtrl?.chaFile?.parameter != null && chaCtrl?.chaFile?.coordinate?.clothes != null)
                    StoreSexOnClothes(chaCtrl.chaFile.parameter.sex, chaCtrl.chaFile.coordinate.clothes);
            }

#endif

        }
    }
}
