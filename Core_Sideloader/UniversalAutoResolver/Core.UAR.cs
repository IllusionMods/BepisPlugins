using Sideloader.ListLoader;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Logging = BepInEx.Logging;
#if AI
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";
        public const string UARExtIDOld = "EC.Core.Sideloader.UniversalAutoResolver";

        private static ILookup<int, ResolveInfo> _resolveInfoLookupSlot;
        private static ILookup<int, ResolveInfo> _resolveInfoLookupLocalSlot;
        /// <summary>
        /// The starting point for UAR IDs
        /// </summary>
        public const int BaseSlotID = 100000000;
        private static int CurrentSlotID = BaseSlotID;

        public static IEnumerable<ResolveInfo> LoadedResolutionInfo =>
            _resolveInfoLookupSlot?.SelectMany(x => x) ?? Enumerable.Empty<ResolveInfo>();
        public static ResolveInfo TryGetResolutionInfo(string property, int localSlot) =>
            _resolveInfoLookupLocalSlot?[localSlot].FirstOrDefault(x => x.Property == property);
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, ChaListDefine.CategoryNo categoryNo) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.CategoryNo == categoryNo);
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, string guid) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.GUID == guid);
        public static ResolveInfo TryGetResolutionInfo(int slot, string property, ChaListDefine.CategoryNo categoryNo, string guid) =>
            _resolveInfoLookupSlot?[slot].FirstOrDefault(x => x.Property == property && x.CategoryNo == categoryNo && x.GUID == guid);

        public static void SetResolveInfos(ICollection<ResolveInfo> results)
        {
            _resolveInfoLookupSlot = results.ToLookup(info => info.Slot);
            _resolveInfoLookupLocalSlot = results.ToLookup(info => info.LocalSlot);
        }

        public static void ResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, ICollection<ResolveInfo> extInfo, string propertyPrefix = "")
        {
            void CompatibilityResolve(KeyValuePair<CategoryProperty, StructValue<int>> kv)
            {
                //Only attempt compatibility resolve if the ID does not belong to a vanilla item or hard mod
#if KK || EC
                if (!Lists.InternalDataList[kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#elif AI
                if (!Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(kv.Value.GetMethod(structure)))
#endif
                {
                    //the property does not have external slot information
                    //check if we have a corrosponding item for backwards compatbility
                    var intResolve = TryGetResolutionInfo(kv.Value.GetMethod(structure), kv.Key.ToString(), kv.Key.Category);

                    if (intResolve != null)
                    {
                        //found a match
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving {intResolve.Property} from slot {kv.Value.GetMethod(structure)} to slot {intResolve.LocalSlot}");

                        kv.Value.SetMethod(structure, intResolve.LocalSlot);
                    }
                    else
                    {
                        //No match was found
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving failed, no match found for ID {kv.Value.GetMethod(structure)} Category {kv.Key.Category}");
                        if (kv.Key.Category.ToString().Contains("ao_") && Sideloader.KeepMissingAccessories.Value && Manager.Scene.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                            kv.Value.SetMethod(structure, 1);
                    }
                }
            }

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

                if (extInfo != null)
                {
                    var extResolve = extInfo.FirstOrDefault(x => x.Property == property);

                    if (extResolve != null)
                    {
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

                                Sideloader.Logger.LogInfo(mainAB);

                                if (int.TryParse(mainAB, out int x))
                                {
                                    //ID found but it conflicts with a vanilla item. Change the ID to avoid conflicts.
                                    ShowGUIDError(extResolve.GUID);
                                    if (kv.Key.Category.ToString().Contains("ao_") && Sideloader.KeepMissingAccessories.Value && Manager.Scene.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                                        kv.Value.SetMethod(structure, 1);
                                    else
                                        kv.Value.SetMethod(structure, 999999);
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
                                    kv.Value.SetMethod(structure, 999999);
                            }
                        }
                    }
                    else
                    {
                        CompatibilityResolve(kv);
                    }
                }
                else
                {
                    CompatibilityResolve(kv);
                }
            }
        }

        public static void GenerateResolutionInfo(Manifest manifest, ChaListData data, List<ResolveInfo> results)
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
                    if (Sideloader.DebugResolveInfoLogging.Value)
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
                        if (Sideloader.DebugResolveInfoLogging.Value)
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

        public static void ShowGUIDError(string guid)
        {
            Logging.LogLevel loglevel = Sideloader.MissingModWarning.Value ? Logging.LogLevel.Warning | Logging.LogLevel.Message : Logging.LogLevel.Warning;

            if (LoadedResolutionInfo.Any(x => x.GUID == guid))
                //we have the GUID loaded, so the user has an outdated mod
                Sideloader.Logger.Log(loglevel, $"[UAR] WARNING! Outdated mod detected! [{guid}]");
#if KK
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
