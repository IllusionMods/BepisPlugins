using HarmonyLib;
using Sideloader.ListLoader;
using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        /// <summary>
        /// Extended save ID for Studio animations saved to characters in scenes
        /// </summary>
        public const string UARExtIDStudioAnimation = UARExtID + ".studioanimation";

        /// <summary>
        /// All loaded StudioResolveInfo
        /// </summary>
        public static List<StudioResolveInfo> LoadedStudioResolutionInfo = new List<StudioResolveInfo>();

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
                    var item = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.GUID == manifest.GUID && x.Slot == slot);
                    newSlot = item == null ? slot : item.LocalSlot;

                    LoadedStudioResolutionInfo.Add(new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = slot,
                        LocalSlot = newSlot,
                        ResolveItem = false
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
                    LoadedStudioResolutionInfo.Add(new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(entry[0]),
                        LocalSlot = int.Parse(entry[0]),
                        ResolveItem = false
                    });
                }
            }
            else
            {
                foreach (List<string> entry in data.Entries)
                {
                    int newSlot = Interlocked.Increment(ref CurrentSlotID);

                    StudioResolveInfo studioResolveInfo = new StudioResolveInfo
                    {
                        GUID = manifest.GUID,
                        Slot = int.Parse(entry[0]),
                        LocalSlot = newSlot,
                        ResolveItem = true
                    };

                    //Group and category is important for animations since the same ID can be used in different groups and categories
                    //...probably other item types too, but don't tell anyone or I'll have to add support for it
                    if (StudioListType == "anime" || StudioListType == "hanime")
                    {
#if KK
                        studioResolveInfo.Group = int.Parse(entry[1]);
                        studioResolveInfo.Category = int.Parse(entry[2]);
#elif AI || HS2
                        studioResolveInfo.Group = int.Parse(entry[2]);
                        studioResolveInfo.Category = int.Parse(entry[3]);
#endif
                    }

                    LoadedStudioResolutionInfo.Add(studioResolveInfo);

                    if (Sideloader.DebugLoggingResolveInfo.Value)
                    {
                        Sideloader.Logger.LogInfo($"StudioResolveInfo - " +
                                                  $"GUID: {manifest.GUID} " +
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
                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == Item.no && x.GUID == extResolve.GUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Item) [{extResolve.GUID}] {Item.no}->{intResolve.LocalSlot}");
                    Traverse.Create(Item).Property("no").SetValue(intResolve.LocalSlot);
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID);
            }
            else if (OI is OILightInfo Light)
            {
                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == Light.no && x.GUID == extResolve.GUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Light) [{extResolve.GUID}] {Light.no}->{intResolve.LocalSlot}");
                    Traverse.Create(Light).Property("no").SetValue(intResolve.LocalSlot);
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID);
            }
            else if (OI is OICharInfo CharInfo)
            {
                //Resolve the animation ID for the character
                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == CharInfo.animeInfo.no && x.GUID == extResolve.GUID && x.Group == extResolve.Group && x.Category == extResolve.Category);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Animation) [{extResolve.GUID}] {CharInfo.animeInfo.group}:{CharInfo.animeInfo.category}:{CharInfo.animeInfo.no}->{intResolve.LocalSlot}");
                    CharInfo.animeInfo.no = intResolve.LocalSlot;
                }
                else if (resolveType == ResolveType.Load)
                    ShowGUIDError(extResolve.GUID);
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
                        Traverse.Create(Item).Property("no").SetValue(intResolve.LocalSlot);
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
                        Traverse.Create(Light).Property("no").SetValue(intResolve.LocalSlot);
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
#if KK
                for (int i = 0; i < Item.pattern.Length; i++)
                {
                    if (!extResolve.ObjectPatternInfo.TryGetValue(i, out var patternInfo)) continue;

                    var intResolve = TryGetResolutionInfo(Item.pattern[i].key, ChaListDefine.CategoryNo.mt_pattern, patternInfo.GUID);
                    if (intResolve != null)
                    {
                        if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                            Sideloader.Logger.LogDebug($"Resolving (Studio Item Pattern) [{ patternInfo.GUID}] {Item.pattern[i].key}->{intResolve.LocalSlot}");
                        Item.pattern[i].key = intResolve.LocalSlot;
                    }
                    else if (resolveType == ResolveType.Load)
                    {
                        ShowGUIDError(patternInfo.GUID);
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
                            Sideloader.Logger.LogDebug($"Resolving (Studio Item Pattern) [{ patternInfo.GUID}] {Item.colors[i].pattern.key}->{intResolve.LocalSlot}");
                        Item.colors[i].pattern.key = intResolve.LocalSlot;
                    }
                    else if (resolveType == ResolveType.Load)
                    {
                        ShowGUIDError(patternInfo.GUID);
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

                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == MapID && x.GUID == MapGUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Map) [{MapGUID}] {MapID}->{intResolve.LocalSlot}");
                    SetMapID(intResolve.LocalSlot);
                }
                else
                    ShowGUIDError(MapGUID);
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
#if KK
            //Set filter ID to the resolved ID
            int filterID = Studio.Studio.Instance.sceneInfo.aceNo;

            if (extData != null && extData.data.ContainsKey("filterInfoGUID"))
            {
                string filterGUID = (string)extData.data["filterInfoGUID"];

                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == filterID && x.GUID == filterGUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Filter) [{filterGUID}] {filterID}->{intResolve.LocalSlot}");
                    Studio.Studio.Instance.sceneInfo.aceNo = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(filterGUID);
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
#if KK
            //Set ramp ID to the resolved ID
            int rampID = Studio.Studio.Instance.sceneInfo.rampG;

            if (extData != null && extData.data.ContainsKey("rampInfoGUID"))
            {
                string rampGUID = (string)extData.data["rampInfoGUID"];

                ResolveInfo intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == rampID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio Ramp) [{rampID}] {rampID}->{intResolve.LocalSlot}");

                    Studio.Studio.Instance.sceneInfo.rampG = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(rampGUID);
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

                StudioResolveInfo intResolve = LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == bgmID && x.GUID == bgmGUID);
                if (intResolve != null)
                {
                    if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                        Sideloader.Logger.LogDebug($"Resolving (Studio BGM) [{bgmGUID}] {bgmID}->{intResolve.LocalSlot}");
                    Singleton<Studio.Studio>.Instance.sceneInfo.bgmCtrl.no = intResolve.LocalSlot;
                }
                else
                    ShowGUIDError(bgmGUID);
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