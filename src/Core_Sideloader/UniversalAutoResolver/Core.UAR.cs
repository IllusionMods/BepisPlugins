﻿using Illusion.Extensions;
using Sideloader.ListLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Logging = BepInEx.Logging;
#if AI
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Automatically resolves ID conflicts by saving GUID to the card and changing item IDs at runtime
    /// </summary>
    public static partial class UniversalAutoResolver
    {
        /// <summary>
        /// Extended save ID
        /// </summary>
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";
        /// <summary>
        /// Extended save ID used in EmotionCreators once upon a time, no longer used but must still be checked for cards that still use it
        /// </summary>
        public const string UARExtIDOld = "EC.Core.Sideloader.UniversalAutoResolver";

        private static ILookup<int, ResolveInfo> _resolveInfoLookupSlot;
        private static ILookup<int, ResolveInfo> _resolveInfoLookupLocalSlot;
        private static ILookup<string, MigrationInfo> _migrationInfoLookupGUID;
        private static ILookup<int, MigrationInfo> _migrationInfoLookupSlot;

        /// <summary>
        /// The starting point for UAR IDs
        /// </summary>
        public const int BaseSlotID = 100000000;
        private static int CurrentSlotID = BaseSlotID;

        /// <summary>
        /// All loaded ResolveInfo
        /// </summary>
        public static IEnumerable<ResolveInfo> LoadedResolutionInfo => _resolveInfoLookupSlot?.SelectMany(x => x) ?? Enumerable.Empty<ResolveInfo>();
        /// <summary>
        /// Get the ResolveInfo for an item
        /// </summary>
        /// <param name="property">Property as defined in StructReference</param>
        /// <param name="localSlot">Current (resolved) ID of the item</param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(string property, int localSlot) =>
            _resolveInfoLookupLocalSlot?[localSlot].FirstOrDefault(x => x.Property == property);
        /// <summary>
        /// Get the ResolveInfo for an item
        /// </summary>
        /// <param name="categoryNo">Category number of the item</param>
        /// <param name="localSlot">Current (resolved) ID of the item</param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(ChaListDefine.CategoryNo categoryNo, int localSlot) =>
            _resolveInfoLookupLocalSlot?[localSlot].FirstOrDefault(x => x.CategoryNo == categoryNo);
        /// <summary>
        /// Get the ResolveInfo for an item. Used for compatibility resolving in cases where GUID is not known (hard mods).
        /// </summary>
        /// <param name="slot">Original ID as defined in the list file</param>
        /// <param name="property">Property as defined in StructReference</param>
        /// <param name="categoryNo">Category number of the item</param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, ChaListDefine.CategoryNo categoryNo) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.CategoryNo == categoryNo);
        /// <summary>
        /// Get the ResolveInfo for an item
        /// </summary>
        /// <param name="slot">Original ID as defined in the list file</param>
        /// <param name="categoryNo">Category number of the item</param>
        /// <param name="guid"></param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(int slot, ChaListDefine.CategoryNo categoryNo, string guid) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.CategoryNo == categoryNo && x.GUID == guid);
        /// <summary>
        /// Get the ResolveInfo for an item
        /// </summary>
        /// <param name="slot">Original ID as defined in the list file</param>
        /// <param name="property"></param>
        /// <param name="guid"></param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, string guid) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.GUID == guid);
        /// <summary>
        /// Get the ResolveInfo for an item
        /// </summary>
        /// <param name="slot">Original ID as defined in the list file</param>
        /// <param name="property"></param>
        /// <param name="categoryNo"></param>
        /// <param name="guid"></param>
        /// <returns>ResolveInfo</returns>
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, ChaListDefine.CategoryNo categoryNo, string guid) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.CategoryNo == categoryNo && x.GUID == guid);
        /// <summary>
        /// Get all MigrationInfo for the GUID
        /// </summary>
        /// <param name="guidOld">GUID that will be migrated</param>
        /// <returns>A list of MigrationInfo</returns>
        public static List<MigrationInfo> GetMigrationInfo(string guidOld) => _migrationInfoLookupGUID?[guidOld].ToList();
        /// <summary>
        /// Get all MigrationInfo for the ID
        /// </summary>
        /// <param name="idOld">ID that will be migrated</param>
        /// <returns>A list of MigrationInfo</returns>
        public static List<MigrationInfo> GetMigrationInfo(int idOld) => _migrationInfoLookupSlot?[idOld].ToList();

        internal static void SetResolveInfos(ICollection<ResolveInfo> results)
        {
            _resolveInfoLookupSlot = results.ToLookup(info => info.Slot);
            _resolveInfoLookupLocalSlot = results.ToLookup(info => info.LocalSlot);
        }
        internal static void SetMigrationInfos(ICollection<MigrationInfo> results)
        {
            _migrationInfoLookupGUID = results.ToLookup(info => info.GUIDOld);
            _migrationInfoLookupSlot = results.ToLookup(info => info.IDOld);
        }

        /// <summary>
        /// Change the ID of items saved to a card to their resolved IDs
        /// </summary>
        internal static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ICollection<ResolveInfo> extInfo, string propertyPrefix = "")
        {
            foreach (var kv in propertyDict)
            {
                string property = $"{propertyPrefix}{kv.Key}";

                //For accessories, make sure we're checking the appropriate category
                if (kv.Key.Category.ToString().Contains("ao_"))
                {
                    ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)structure;

                    if ((int)kv.Key.Category != AccessoryInfo.type)
                    {
                        //If the current category does not match the category saved to the card do not attempt resolving
                        continue;
                    }
                }

                ResolveInfo extResolve = extInfo?.FirstOrDefault(x => x.Property == property);
                if (extResolve == null)
                {
                    CompatibilityResolve(kv, structure);
                    continue;
                }

                if (Sideloader.MigrationEnabled.Value)
                    MigrateData(extResolve, kv.Key.Category);

                //If the GUID is blank or has been made blank by migration do compatibility resolve
                if (extResolve.GUID.IsNullOrWhiteSpace())
                {
                    CompatibilityResolve(kv, structure);
                    continue;
                }

                //the property has external slot information 
                var intResolve = TryGetResolutionInfo(extResolve.Slot, kv.Key.ToString(), kv.Key.Category, extResolve.GUID);

                if (intResolve != null)
                {
                    //found a match to a corrosponding internal mod
                    if (Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving {extResolve.GUID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.LocalSlot}");
                    kv.Value.SetMethod(structure, intResolve.LocalSlot);
                }
                else
                {
#if KK || EC
                    if (Lists.InternalDataList[kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#elif AI
                    if (Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#endif
                    {
#if KK || EC
                        string mainAB = Lists.InternalDataList[kv.Key.Category][kv.Value.GetMethod(structure)].dictInfo[(int)ChaListDefine.KeyType.MainAB];
#elif AI
                        string mainAB = Lists.InternalDataList[(int)kv.Key.Category][kv.Value.GetMethod(structure)].dictInfo[(int)ChaListDefine.KeyType.MainAB];
#endif
                        mainAB = mainAB.Replace("chara/", "").Replace(".unity3d", "").Replace(kv.Key.Category.ToString() + "_", "").Replace("/", "");

                        if (int.TryParse(mainAB, out int x))
                        {
                            //ID found but it conflicts with a vanilla item. Change the ID to avoid conflicts.
                            ShowGUIDError(extResolve.GUID);
                            if (kv.Key.Category.ToString().Contains("ao_") && Sideloader.KeepMissingAccessories.Value && Manager.Scene.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                                kv.Value.SetMethod(structure, 1);
                            else
                                kv.Value.SetMethod(structure, BaseSlotID - 1);
                        }
                        else
                        {
                            //ID found and it does not conflict with a vanilla item, likely the user has a hard mod version of the mod installed
                            Sideloader.Logger.LogDebug($"Missing mod detected [{extResolve.GUID}] but matching ID found");
                        }
                    }
                    else
                    {
                        //ID not found. Change the ID to avoid potential future conflicts.
                        ShowGUIDError(extResolve.GUID);
                        if (kv.Key.Category.ToString().Contains("ao_") && Sideloader.KeepMissingAccessories.Value && Manager.Scene.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                            kv.Value.SetMethod(structure, 1);
                        else
                            kv.Value.SetMethod(structure, BaseSlotID - 1);
                    }
                }
            }
        }

        private static void CompatibilityResolve(KeyValuePair<CategoryProperty, StructValue<int>> kv, object structure)
        {
            //Only attempt compatibility resolve if the ID does not belong to a vanilla item or hard mod
#if KK || EC
            if (!Lists.InternalDataList[kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#elif AI
            if (!Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#endif
            {
                int slot = kv.Value.GetMethod(structure);
                //The property does not have external slot information
                //Check if we have a corrosponding item for backwards compatbility
                var intResolve = TryGetResolutionInfo(slot, kv.Key.ToString(), kv.Key.Category);
                if (intResolve != null)
                {
                    //found a match
                    if (Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Compatibility resolving {intResolve.Property} from slot {slot} to slot {intResolve.LocalSlot}");

                    kv.Value.SetMethod(structure, intResolve.LocalSlot);
                    return;
                }

                //Check for migration info for this ID
                var migrationInfo = GetMigrationInfo(slot).FirstOrDefault(x => x.Category == kv.Key.Category);
                if (migrationInfo != null)
                {
                    var intResolve2 = TryGetResolutionInfo(migrationInfo.IDNew, kv.Key.ToString(), kv.Key.Category, migrationInfo.GUIDNew);
                    if (intResolve2 != null)
                    {
                        Sideloader.Logger.LogInfo($"Migrating {migrationInfo.IDOld} -> {migrationInfo.GUIDNew}:{migrationInfo.IDNew}");

                        kv.Value.SetMethod(structure, intResolve2.LocalSlot);
                        return;
                    }
                }

                //No match was found
                if (Sideloader.DebugLogging.Value)
                    Sideloader.Logger.LogDebug($"Compatibility resolving failed, no match found for ID {kv.Value.GetMethod(structure)} Category {kv.Key.Category}");
                if (kv.Key.Category.ToString().Contains("ao_") && Sideloader.KeepMissingAccessories.Value && Manager.Scene.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                    kv.Value.SetMethod(structure, 1);
            }
        }

        internal static void IterateCardPrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action, ChaFile file, ICollection<ResolveInfo> extInfo)
        {
            action(StructReference.ChaFileFaceProperties, file.custom.face, extInfo, "");
            action(StructReference.ChaFileBodyProperties, file.custom.body, extInfo, "");
            action(StructReference.ChaFileHairProperties, file.custom.hair, extInfo, "");
#if AI
            action(StructReference.ChaFileMakeupProperties, file.custom.face.makeup, extInfo, "");
#else
            action(StructReference.ChaFileMakeupProperties, file.custom.face.baseMakeup, extInfo, "");
#endif

#if KK
            for (int i = 0; i < file.coordinate.Length; i++)
            {
                var coordinate = file.coordinate[i];
                string prefix = $"outfit{i}.";
                IterateCoordinatePrefixes(action, coordinate, extInfo, prefix);
            }
#else
            IterateCoordinatePrefixes(action, file.coordinate, extInfo, "outfit.");
#endif
        }

        internal static void IterateCoordinatePrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action, ChaFileCoordinate coordinate, ICollection<ResolveInfo> extInfo, string prefix = "")
        {
            prefix = prefix.IsNullOrWhiteSpace() ? string.Empty : prefix;
            action(StructReference.ChaFileClothesProperties, coordinate.clothes, extInfo, prefix);

            for (int acc = 0; acc < coordinate.accessory.parts.Length; acc++)
            {
                string accPrefix = $"{prefix}accessory{acc}.";

                action(StructReference.ChaFileAccessoryPartsInfoProperties, coordinate.accessory.parts[acc], extInfo, accPrefix);
            }
        }

        internal static void MigrateData(ResolveInfo extResolve, ChaListDefine.CategoryNo categoryNo)
        {
            if (extResolve.GUID.IsNullOrWhiteSpace()) return;

            var migrationInfoList = GetMigrationInfo(extResolve.GUID);

            if (migrationInfoList.Any(x => x.MigrationType == MigrationType.StripAll))
            {
                extResolve.GUID = "";
                return;
            }

            foreach (var migrationInfo in migrationInfoList.Where(x => x.MigrationType == MigrationType.MigrateAll))
            {
                if (Sideloader.GetManifest(migrationInfo.GUIDNew) != null)
                {
                    Sideloader.Logger.LogInfo($"Migrating GUID {migrationInfo.GUIDOld} -> {migrationInfo.GUIDNew}");
                    extResolve.GUID = migrationInfo.GUIDNew;
                    return;
                }
            }

            foreach (var migrationInfo in migrationInfoList.Where(x => x.IDOld == extResolve.Slot && categoryNo == extResolve.CategoryNo))
            {
                if (Sideloader.GetManifest(migrationInfo.GUIDNew) != null)
                {
                    Sideloader.Logger.LogInfo($"Migrating {migrationInfo.GUIDOld}:{migrationInfo.IDOld} -> {migrationInfo.GUIDNew}:{migrationInfo.IDNew}");
                    extResolve.GUID = migrationInfo.GUIDNew;
                    extResolve.Slot = migrationInfo.IDNew;
                    return;
                }
            }
        }

        internal static void GenerateResolutionInfo(Manifest manifest, ChaListData data, List<ResolveInfo> results)
        {
            var category = (ChaListDefine.CategoryNo)data.categoryNo;

            var propertyKeys = StructReference.CollatedStructValues.Keys.Where(x => x.Category == category).ToList();

            foreach (var kv in data.dictList)
            {
                int newSlot = Interlocked.Increment(ref CurrentSlotID);

#if KK || EC
                if (data.categoryNo == (int)ChaListDefine.CategoryNo.mt_ramp)
                {
                    //Special handling for ramp stuff since it's the only thing that isn't saved to the character
                    if (Sideloader.DebugLoggingResolveInfo.Value)
                    {
                        Sideloader.Logger.LogInfo($"ResolveInfo - " +
                                                  $"GUID: {manifest.GUID} " +
                                                  $"Slot: {int.Parse(kv.Value[0])} " +
                                                  $"LocalSlot: {newSlot} " +
                                                  $"Property: Ramp " +
                                                  $"CategoryNo: {category} " +
                                                  $"Count: {LoadedResolutionInfo.Count()}");
                    }

                    results.Add(new ResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(kv.Value[0]),
                        LocalSlot = newSlot,
                        Property = "Ramp",
                        CategoryNo = category
                    });
                }
                else
#endif
                {
                    results.AddRange(propertyKeys.Select(propertyKey =>
                    {
                        if (Sideloader.DebugLoggingResolveInfo.Value)
                        {
                            Sideloader.Logger.LogInfo($"ResolveInfo - " +
                                                      $"GUID: {manifest.GUID} " +
                                                      $"Slot: {int.Parse(kv.Value[0])} " +
                                                      $"LocalSlot: {newSlot} " +
                                                      $"Property: {propertyKey.ToString()} " +
                                                      $"CategoryNo: {category} " +
                                                      $"Count: {LoadedResolutionInfo.Count()}");
                        }

                        return new ResolveInfo
                        {
                            GUID = manifest.GUID,
                            Slot = int.Parse(kv.Value[0]),
                            LocalSlot = newSlot,
                            Property = propertyKey.ToString(),
                            CategoryNo = category
                        };
                    }));
                }

                kv.Value[0] = newSlot.ToString();
            }
        }

        internal static void GenerateMigrationInfo(Manifest manifest, List<MigrationInfo> results)
        {
            manifest.LoadMigrationInfo();
            results.AddRange(manifest.MigrationList);
        }

        internal static void ShowGUIDError(string guid)
        {
            Logging.LogLevel loglevel = Sideloader.MissingModWarning.Value ? Logging.LogLevel.Warning | Logging.LogLevel.Message : Logging.LogLevel.Warning;

            if (LoadedResolutionInfo.Any(x => x.GUID == guid))
                //we have the GUID loaded, so the user has an outdated mod
                Sideloader.Logger.Log(loglevel, $"[UAR] WARNING! Outdated mod detected! [{guid}]");
#if KK || AI
            else if (LoadedStudioResolutionInfo.Any(x => x.GUID == guid))
                //we have the GUID loaded, so the user has an outdated mod
                Sideloader.Logger.Log(loglevel, $"[UAR] WARNING! Outdated mod detected! [{guid}]");
#endif
            else
                //did not find a match, we don't have the mod
                Sideloader.Logger.Log(loglevel, $"[UAR] WARNING! Missing mod detected! [{guid}]");
        }
    }
}
