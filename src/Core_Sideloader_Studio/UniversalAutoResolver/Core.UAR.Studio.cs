using System;
using HarmonyLib;
using Sideloader.ListLoader;
using Studio;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS0618

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        /// <summary>
        /// Extended save ID for Studio animations saved to characters in scenes
        /// </summary>
        [Obsolete("Unused")]
        public const string UARExtIDStudioAnimation = UARExtID + ".studioanimation";

        /// <summary>
        /// All loaded StudioResolveInfo
        /// </summary>
        [Obsolete("Use GetStudioResolveInfos and AddStudioResolutionInfo instead")]
        public static List<StudioResolveInfo> LoadedStudioResolutionInfo = new List<StudioResolveInfo>();

        static int LastLoadedStudioResolutionInfoCount = 0;

        /// <summary>
        /// LocalSlot -> resolve info lookup.
        /// </summary>
        internal static Dictionary<int, List<StudioResolveInfo>> StudioResolutionInfoLocalSlotLookup = new Dictionary<int, List<StudioResolveInfo>>();
        /// <summary>
        /// GUID + Slot -> resolve info lookup.
        /// </summary>
        internal static Dictionary<string, Dictionary<int, List<StudioResolveInfo>>> StudioResolutionInfoGuidLookup = new Dictionary<string, Dictionary<int, List<StudioResolveInfo>>>(StringComparer.OrdinalIgnoreCase);

        public static void AddStudioResolutionInfo(StudioResolveInfo sri)
        {
            UpdateLookupsIfNeeded();

            LoadedStudioResolutionInfo.Add(sri);
            LastLoadedStudioResolutionInfoCount = LoadedStudioResolutionInfo.Count;

            AddToLookups(sri);
        }

        /// <summary>
        /// Handle other plugins adding items to LoadedStudioResolutionInfo directly. Assumes items are added to the end, not inserted.
        /// Necessary for AnimationLoader to work.
        /// </summary>
        private static void UpdateLookupsIfNeeded()
        {
            while (LastLoadedStudioResolutionInfoCount < LoadedStudioResolutionInfo.Count)
            {
                AddToLookups(LoadedStudioResolutionInfo[LastLoadedStudioResolutionInfoCount]);
                LastLoadedStudioResolutionInfoCount++;
            }
        }

        private static void AddToLookups(StudioResolveInfo resolveInfo)
        {
            if (!StudioResolutionInfoLocalSlotLookup.TryGetValue(resolveInfo.LocalSlot, out var localSlotList))
            {
                localSlotList = new List<StudioResolveInfo>();
                StudioResolutionInfoLocalSlotLookup.Add(resolveInfo.LocalSlot, localSlotList);
            }

            localSlotList.Add(resolveInfo);

            if (!StudioResolutionInfoGuidLookup.TryGetValue(resolveInfo.GUID, out var slotLookup))
            {
                slotLookup = new Dictionary<int, List<StudioResolveInfo>>();
                StudioResolutionInfoGuidLookup.Add(resolveInfo.GUID, slotLookup);
            }

            if (!slotLookup.TryGetValue(resolveInfo.Slot, out var slotList))
            {
                slotList = new List<StudioResolveInfo>();
                slotLookup.Add(resolveInfo.Slot, slotList);
            }

            slotList.Add(resolveInfo);
        }


        private static bool IsResolveItem(StudioResolveInfo sri) => sri.ResolveItem;
        private static readonly ICollection<StudioResolveInfo> _EmptyResolveInfos = new StudioResolveInfo[0];

        /// <summary>
        /// Get all resolve infos with a given Local Slot. Optionally only return ResolveItems.
        /// With <paramref name="onlyResolveItems"/><code>==true</code> in most cases returns a single item so it's fine to use FirstOrDefault.
        /// </summary>
        public static IEnumerable<StudioResolveInfo> GetStudioResolveInfos(int localSlot, bool onlyResolveItems)
        {
            UpdateLookupsIfNeeded();

            if (StudioResolutionInfoLocalSlotLookup.TryGetValue(localSlot, out var result))
                return onlyResolveItems ? result.Where(IsResolveItem) : result;
            return _EmptyResolveInfos;
        }

        /// <summary>
        /// Get all resolve infos with a given GUID and Slot. Optionally only return ResolveItems.
        /// With <paramref name="onlyResolveItems"/><code>==true</code> in most cases returns a single item so it's fine to use FirstOrDefault.
        /// </summary>
        public static IEnumerable<StudioResolveInfo> GetStudioResolveInfos(string guid, int slot, bool onlyResolveItems)
        {
            UpdateLookupsIfNeeded();

            if (StudioResolutionInfoGuidLookup.TryGetValue(guid, out var slotLookup))
            {
                if (slotLookup.TryGetValue(slot, out var result))
                    return onlyResolveItems ? result.Where(IsResolveItem) : result;
            }
            return _EmptyResolveInfos;
        }

        internal static void GenerateStudioResolutionInfo(Manifest manifest, Lists.StudioListData data)
        {
            string StudioListType;
            if (data.FileNameWithoutExtension.Contains('_'))
                StudioListType = data.FileNameWithoutExtension.Split('_')[0].ToLower();
            else
                return; //Not a studio list

            if (StudioListType == "itembonelist")
            {
                foreach (List<string> entry in data.Entries)
                {
                    int slot = int.Parse(entry[0]);
                    int newSlot;

                    //See if the item this bone info cooresponds to has been resolved and set the ID to the same resolved ID
                    var item = GetStudioResolveInfos(manifest.GUID, slot, true).FirstOrDefault();
                    newSlot = item == null ? slot : item.LocalSlot;

                    AddStudioResolutionInfo(new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = slot,
                        LocalSlot = newSlot,
                        ResolveItem = false,
                        Author = manifest.Author,
                        Website = manifest.Website,
                        Name = manifest.Name
                    });

                    entry[0] = newSlot.ToString();
                }
            }
            else if (Sideloader.StudioListResolveBlacklist.Contains(StudioListType))
            {
                foreach (List<string> entry in data.Entries)
                {
                    //Add it to the resolution info as is, studio will automatically merge groups with the same IDs without causing exceptions.
                    //The IDs are expected to stay the same anyway as ItemLists will contain a reference to them.
                    //Because of this, all ID lookups should check if the thing is a ResolveItem.
                    AddStudioResolutionInfo(new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(entry[0]),
                        LocalSlot = int.Parse(entry[0]),
                        ResolveItem = false,
                        Author = manifest.Author,
                        Website = manifest.Website,
                        Name = manifest.Name
                    });
                }
            }
            else
            {
                foreach (List<string> entry in data.Entries)
                {
                    int newSlot = GetUniqueSlotID();

                    StudioResolveInfo studioResolveInfo = new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(entry[0]),
                        LocalSlot = newSlot,
                        ResolveItem = true,
                        Author = manifest.Author,
                        Website = manifest.Website,
                        Name = manifest.Name
                    };

                    //Group and category is important for animations since the same ID can be used in different groups and categories
                    //...probably other item types too, but don't tell anyone or I'll have to add support for it
                    if (StudioListType == "anime" || StudioListType == "hanime")
                    {
#if KK || KKS
                        studioResolveInfo.Group = int.Parse(entry[1]);
                        studioResolveInfo.Category = int.Parse(entry[2]);
#elif AI || HS2
                        studioResolveInfo.Group = int.Parse(entry[2]);
                        studioResolveInfo.Category = int.Parse(entry[3]);
#endif
                    }

                    AddStudioResolutionInfo(studioResolveInfo);

                    if (Sideloader.DebugLoggingResolveInfo.Value)
                    {
                        Sideloader.Logger.LogInfo($"StudioResolveInfo - " +
                                                  $"GUID: {manifest.GUID} " +
                                                  $"Name: {manifest.Name} " +
                                                  $"Author: {manifest.Author} " +
                                                  $"Website: {manifest.Website} " +
                                                  $"Slot: {int.Parse(entry[0])} " +
                                                  $"LocalSlot: {newSlot} " +
                                                  $"Count: {LoadedStudioResolutionInfo.Count}");
                    }

                    entry[0] = newSlot.ToString();
                }
            }
        }

        internal enum ResolveType { Save, Load }
        internal static void ResolveStudioObjects(ExtensibleSaveFormat.PluginData extendedData, ResolveType resolveType)
        {
            Dictionary<int, ObjectInfo> ObjectList = StudioObjectSearch.FindObjectInfo(StudioObjectSearch.SearchType.All);

            //Resolve all patterns for objects
            if (extendedData != null && extendedData.data.ContainsKey("patternInfo"))
            {
                List<StudioPatternResolveInfo> extPatternInfo;

                if (resolveType == ResolveType.Save)
                    extPatternInfo = ((List<byte[]>)extendedData.data["patternInfo"]).Select(x => StudioPatternResolveInfo.Deserialize(x)).ToList();
                else
                    extPatternInfo = ((object[])extendedData.data["patternInfo"]).Select(x => StudioPatternResolveInfo.Deserialize((byte[])x)).ToList();

                foreach (StudioPatternResolveInfo extPatternResolve in extPatternInfo)
                    ResolveStudioObjectPattern(extPatternResolve, ObjectList[extPatternResolve.DicKey], resolveType);
            }

            //Resolve every item with extended data
            if (extendedData != null && extendedData.data.ContainsKey("itemInfo"))
            {
                List<StudioResolveInfo> extInfo;

                if (resolveType == ResolveType.Save)
                    extInfo = ((List<byte[]>)extendedData.data["itemInfo"]).Select(x => StudioResolveInfo.Deserialize(x)).ToList();
                else
                    extInfo = ((object[])extendedData.data["itemInfo"]).Select(x => StudioResolveInfo.Deserialize((byte[])x)).ToList();

                foreach (StudioResolveInfo extResolve in extInfo)
                {
                    ResolveStudioObject(extResolve, ObjectList[extResolve.DicKey], resolveType);
                    ObjectList.Remove(extResolve.DicKey);
                }
            }

            //Resolve every item without extended data in case of hard mods
            if (resolveType == ResolveType.Load)
                foreach (ObjectInfo OI in ObjectList.Where(x => x.Value is OIItemInfo || x.Value is OILightInfo || x.Value is OICharInfo).Select(x => x.Value))
                    ResolveStudioObject(OI);
        }

        internal static void ResolveStudioObject(StudioResolveInfo extResolve, ObjectInfo OI, ResolveType resolveType = ResolveType.Load)
        {
            if (OI is OIItemInfo Item)
            {
                StudioResolveInfo intResolve = GetStudioResolveInfos(extResolve.GUID, Item.no, true).FirstOrDefault();
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Item) [{extResolve.GUID}] {Item.no}->{intResolve.LocalSlot}");
                    Item.no = intResolve.LocalSlot;
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID, extResolve.Author, extResolve.Website, extResolve.Name);
            }
            else if (OI is OILightInfo Light)
            {
                StudioResolveInfo intResolve = GetStudioResolveInfos(extResolve.GUID, Light.no, true).FirstOrDefault();
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Light) [{extResolve.GUID}] {Light.no}->{intResolve.LocalSlot}");
                    Light.no = intResolve.LocalSlot;
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID, extResolve.Author, extResolve.Website, extResolve.Name);
            }
            else if (OI is OICharInfo CharInfo)
            {
                //Resolve the animation ID for the character

                StudioResolveInfo intResolve = GetStudioResolveInfos(extResolve.GUID, CharInfo.animeInfo.no, true).FirstOrDefault(x => x.Group == extResolve.Group && x.Category == extResolve.Category);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Animation) [{extResolve.GUID}] {CharInfo.animeInfo.group}:{CharInfo.animeInfo.category}:{CharInfo.animeInfo.no}->{intResolve.LocalSlot}");
                    CharInfo.animeInfo.no = intResolve.LocalSlot;
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID, extResolve.Author, extResolve.Website, extResolve.Name);
            }
        }

        /// <summary>
        /// Compatibility resolving for objects with no extended save data
        /// </summary>
        internal static void ResolveStudioObject(ObjectInfo OI)
        {
            if (OI is OIItemInfo Item)
            {
                if (!Lists.InternalStudioItemList.Contains(Item.no))
                {
                    //Item does not exist in the item list, probably a missing hard mod. See if we have a sideloader mod with the same ID
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == Item.no);
                    if (intResolve != null)
                    {
                        //Found a match
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Item) {Item.no}->{intResolve.LocalSlot}");
                        Item.no = intResolve.LocalSlot;
                    }
                    else
                    {
                        //No match was found
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Item) failed, no match found for ID {Item.no}");
                    }
                }
            }
            else if (OI is OILightInfo Light)
            {
                if (!Singleton<Info>.Instance.dicLightLoadInfo.TryGetValue(Light.no, out Info.LightLoadInfo lightLoadInfo))
                {
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == Light.no);
                    if (intResolve != null)
                    {
                        //Found a match
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Light) {Light.no}->{intResolve.LocalSlot}");
                        Light.no = intResolve.LocalSlot;
                    }
                    else
                    {
                        //No match was found
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Light) failed, no match found for ID {Light.no}");
                    }
                }
            }
            else if (OI is OICharInfo CharInfo)
            {
                bool animationFound = false;
                if (Singleton<Info>.Instance.dicAnimeLoadInfo.TryGetValue(CharInfo.animeInfo.group, out var animeLoadInfo1))
                    if (animeLoadInfo1.TryGetValue(CharInfo.animeInfo.category, out var animeLoadInfo2))
                        if (animeLoadInfo2.TryGetValue(CharInfo.animeInfo.no, out var animeLoadInfo3))
                            animationFound = true;

                //Animation does not exist in the animation list, probably a missing hard mod. See if we have a sideloader mod with the same ID, Group, and Category
                if (!animationFound)
                {
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == CharInfo.animeInfo.no && x.Group == CharInfo.animeInfo.group && x.Category == CharInfo.animeInfo.category);
                    if (intResolve != null)
                    {
                        //Found a match
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Animation) {CharInfo.animeInfo.no}->{intResolve.LocalSlot} Group {CharInfo.animeInfo.group} Category {CharInfo.animeInfo.category}");
                        CharInfo.animeInfo.no = intResolve.LocalSlot;
                    }
                    else
                    {
                        //No match was found
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Animation) failed, no match found for ID {CharInfo.animeInfo} Group {CharInfo.animeInfo.group} Category {CharInfo.animeInfo.category}");
                    }
                }
            }
        }

        internal static void ResolveStudioObjectPattern(StudioPatternResolveInfo extResolve, ObjectInfo OI, ResolveType resolveType = ResolveType.Load)
        {
            if (OI is OIItemInfo Item)
            {
#if KK || KKS
                for (int i = 0; i < Item.pattern.Length; i++)
                {
                    if (!extResolve.ObjectPatternInfo.TryGetValue(i, out var patternInfo)) continue;

                    var intResolve = TryGetResolutionInfo(Item.pattern[i].key, ChaListDefine.CategoryNo.mt_pattern, patternInfo.GUID);
                    if (intResolve != null)
                    {
                        if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Resolving (Studio Item Pattern) [{patternInfo.GUID}] {Item.pattern[i].key}->{intResolve.LocalSlot}");
                        Item.pattern[i].key = intResolve.LocalSlot;
                    }
                    else if (resolveType == ResolveType.Load)
                    {
                        ShowGUIDError(patternInfo.GUID, patternInfo.Author, patternInfo.Website, patternInfo.Name);
                        Item.pattern[i].key = BaseSlotID - 1;
                    }
                }
#elif AI || HS2
                for (int i = 0; i < Item.colors.Length; i++)
                {
                    if (!extResolve.ObjectPatternInfo.TryGetValue(i, out var patternInfo)) continue;

                    var intResolve = TryGetResolutionInfo(Item.colors[i].pattern.key, AIChara.ChaListDefine.CategoryNo.st_pattern, patternInfo.GUID);
                    if (intResolve != null)
                    {
                        if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Resolving (Studio Item Pattern) [{patternInfo.GUID}] {Item.colors[i].pattern.key}->{intResolve.LocalSlot}");
                        Item.colors[i].pattern.key = intResolve.LocalSlot;
                    }
                    else if (resolveType == ResolveType.Load)
                    {
                        ShowGUIDError(patternInfo.GUID, patternInfo.Author, patternInfo.Website, patternInfo.Name);
                        Item.colors[i].pattern.key = BaseSlotID - 1;
                    }
                }
#endif
            }
        }

        internal static void ResolveStudioMap(ExtensibleSaveFormat.PluginData extData, ResolveType resolveType)
        {
            //Set map ID to the resolved ID
            int MapID = GetMapID();

            if (MapID == -1) //Loaded scene has no map
                return;

            if (extData != null && extData.data.ContainsKey("mapInfoGUID"))
            {
                string MapGUID = (string)extData.data["mapInfoGUID"];

                string MapAuthor = null;
                if (extData.data.TryGetValue("mapInfoAuthor", out object MapAuthorData))
                    MapAuthor = (string)MapAuthorData;
                string MapWebsite = null;
                if (extData.data.TryGetValue("mapInfoWebsite", out object MapWebsiteData))
                    MapWebsite = (string)MapWebsiteData;
                string MapName = null;
                if (extData.data.TryGetValue("mapInfoName", out object MapNameData))
                    MapName = (string)MapNameData;

                StudioResolveInfo intResolve = GetStudioResolveInfos(MapGUID, MapID, true).FirstOrDefault();
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Map) [{MapGUID}] {MapID}->{intResolve.LocalSlot}");
                    SetMapID(intResolve.LocalSlot);
                }
                else
                    ShowGUIDError(MapGUID, MapAuthor, MapWebsite, MapName);
            }
            else if (resolveType == ResolveType.Load)
            {
                if (!Singleton<Info>.Instance.dicMapLoadInfo.TryGetValue(MapID, out Info.MapLoadInfo mapInfo))
                {
                    //Map ID saved to the scene doesn't exist in the map list, try compatibility resolving
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == MapID);
                    if (intResolve != null)
                    {
                        //Found a matching sideloader mod
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Map) {MapID}->{intResolve.LocalSlot}");
                        SetMapID(intResolve.LocalSlot);
                    }
                    else
                    {
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Map) failed, no match found for ID {MapID}");
                    }
                }
            }
        }

        internal static void ResolveStudioFilter(ExtensibleSaveFormat.PluginData extData, ResolveType resolveType)
        {
#if KK || KKS
            //Set filter ID to the resolved ID
            int filterID = Studio.Studio.Instance.sceneInfo.aceNo;

            if (extData != null && extData.data.ContainsKey("filterInfoGUID"))
            {
                string filterGUID = (string)extData.data["filterInfoGUID"];

                string filterAuthor = null;
                if (extData.data.TryGetValue("filterInfoAuthor", out object filterAuthorData))
                    filterAuthor = (string)filterAuthorData;
                string filterWebsite = null;
                if (extData.data.TryGetValue("filterInfoWebsite", out object filterWebsiteData))
                    filterWebsite = (string)filterWebsiteData;
                string filterName = null;
                if (extData.data.TryGetValue("filterInfoName", out object filterNameData))
                    filterName = (string)filterNameData;


                StudioResolveInfo intResolve = GetStudioResolveInfos(filterGUID, filterID, true).FirstOrDefault();
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Filter) [{filterGUID}] {filterID}->{intResolve.LocalSlot}");
                    Studio.Studio.Instance.sceneInfo.aceNo = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(filterGUID, filterAuthor, filterWebsite, filterName);
            }
            else if (resolveType == ResolveType.Load)
            {
                if (!Info.Instance.dicFilterLoadInfo.TryGetValue(filterID, out Info.LoadCommonInfo filterInfo))
                {
                    //Filter ID saved to the scene doesn't exist in the filter list, try compatibility resolving
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == filterID);
                    if (intResolve != null)
                    {
                        //Found a matching sideloader mod
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Filter) {filterID}->{intResolve.LocalSlot}");
                        Studio.Studio.Instance.sceneInfo.aceNo = intResolve.LocalSlot;
                    }
                    else
                    {
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Filter) failed, no match found for ID {filterID}");
                    }
                }
            }
#endif
        }

        internal static void ResolveStudioRamp(ExtensibleSaveFormat.PluginData extData, ResolveType resolveType)
        {
#if KK || KKS
            //Set ramp ID to the resolved ID
            int rampID = Studio.Studio.Instance.sceneInfo.rampG;

            if (extData != null && extData.data.ContainsKey("rampInfoGUID"))
            {
                string rampGUID = (string)extData.data["rampInfoGUID"];

                string rampAuthor = null;
                if (extData.data.TryGetValue("rampInfoAuthor", out object rampAuthorData))
                    rampAuthor = (string)rampAuthorData;
                string rampWebsite = null;
                if (extData.data.TryGetValue("rampInfoWebsite", out object rampWebsiteData))
                    rampWebsite = (string)rampWebsiteData;
                string rampName = null;
                if (extData.data.TryGetValue("rampInfoName", out object rampNameData))
                    rampName = (string)rampNameData;

                ResolveInfo intResolve = TryGetResolutionInfo(rampID, "Ramp", rampGUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Ramp) [{rampID}] {rampID}->{intResolve.LocalSlot}");

                    Studio.Studio.Instance.sceneInfo.rampG = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(rampGUID, rampAuthor, rampWebsite, rampName);
            }
            else if (resolveType == ResolveType.Load)
            {
                if (!Lists.InternalDataList[ChaListDefine.CategoryNo.mt_ramp].ContainsKey(rampID))
                {
                    //Ramp ID saved to the scene doesn't exist in the items list, try compatibility resolving
                    ResolveInfo intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.Slot == rampID);
                    if (intResolve != null)
                    {
                        //Found a matching sideloader mod
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio Ramp) {rampID}->{intResolve.LocalSlot}");
                        Studio.Studio.Instance.sceneInfo.rampG = intResolve.LocalSlot;
                    }
                    else
                    {
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio Ramp) failed, no match found for ID {rampID}");
                    }
                }
            }
#endif
        }

        internal static void ResolveStudioBGM(ExtensibleSaveFormat.PluginData extData, ResolveType resolveType)
        {
            //Set bgm ID to the resolved ID
            int bgmID = Singleton<Studio.Studio>.Instance.sceneInfo.bgmCtrl.no;

            if (bgmID == -1) //Loaded scene has no bgm
                return;

            if (extData != null && extData.data.ContainsKey("bgmInfoGUID"))
            {
                string bgmGUID = (string)extData.data["bgmInfoGUID"];

                string bgmAuthor = null;
                if (extData.data.TryGetValue("bgmInfoAuthor", out object bgmAuthorData))
                    bgmAuthor = (string)bgmAuthorData;
                string bgmWebsite = null;
                if (extData.data.TryGetValue("bgmInfoWebsite", out object bgmWebsiteData))
                    bgmWebsite = (string)bgmWebsiteData;
                string bgmName = null;
                if (extData.data.TryGetValue("bgmInfoName", out object bgmNameData))
                    bgmName = (string)bgmNameData;

                StudioResolveInfo intResolve = GetStudioResolveInfos(bgmGUID, bgmID, true).FirstOrDefault();
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio BGM) [{bgmGUID}] {bgmID}->{intResolve.LocalSlot}");
                    Singleton<Studio.Studio>.Instance.sceneInfo.bgmCtrl.no = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(bgmGUID, bgmAuthor, bgmWebsite, bgmName);
            }
            else if (resolveType == ResolveType.Load)
            {
                if (!Singleton<Info>.Instance.dicBGMLoadInfo.TryGetValue(bgmID, out Info.LoadCommonInfo bgmInfo))
                {
                    //BGM ID saved to the scene doesn't exist in the bgm list, try compatibility resolving
                    StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == bgmID);
                    if (intResolve != null)
                    {
                        //Found a matching sideloader mod
                        if (Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Compatibility resolving (Studio BGM) {bgmID}->{intResolve.LocalSlot}");
                        Singleton<Studio.Studio>.Instance.sceneInfo.bgmCtrl.no = intResolve.LocalSlot;
                    }
                    else
                    {
                        Sideloader.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[UAR] Compatibility resolving (Studio BGM) failed, no match found for ID {bgmID}");
                    }
                }
            }
        }

        /// <summary>
        /// Get the current map's ID
        /// </summary>
        /// <returns></returns>
        internal static int GetMapID() => (int)MapIDField.GetValue();
        /// <summary>
        /// Set the current map's ID
        /// </summary>
        /// <param name="id"></param>
        internal static void SetMapID(int id) => MapIDField.SetValue(id);

        /// <summary>
        /// Find the field containing the map ID for cross version compatibility since this was changed in an update to AI Girl and is different between KK and AI/HS2
        /// </summary>
        private static Traverse MapIDField
        {
            get
            {
                if (Traverse.Create(Studio.Studio.Instance.sceneInfo).Field("map").FieldExists())
                    return Traverse.Create(Studio.Studio.Instance.sceneInfo).Field("map");
                else
                    return Traverse.Create(Studio.Studio.Instance.sceneInfo).Field("mapInfo").Field("no");
            }
        }
    }
}