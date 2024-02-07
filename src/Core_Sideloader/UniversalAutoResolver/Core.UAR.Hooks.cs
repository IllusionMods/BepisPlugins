#if !RG
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
#else
using Chara;
using CharaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using Illusion.Unity.Component;
using Manager;
using Sideloader.ListLoader;
using System.Collections.Generic;
using System.Linq;
using BinaryWriter = Il2CppSystem.IO.BinaryWriter;
#endif

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
    //TODO clean up
    #if RG
    internal static class HarmonyExtentions
    {
        private static readonly Type MonoCustomAttrsType;
        private static readonly MethodInfo GetCustomAttributesMethod1;
        private static readonly MethodInfo GetCustomAttributesMethod2;
        private static readonly MethodInfo GetCustomAttributesBaseMethod;
        private static readonly HarmonyMethod GetCustomAttributesHookMethod;

        static HarmonyExtentions()
        {
            MonoCustomAttrsType = Type.GetType($"{nameof(System)}.MonoCustomAttrs");
            GetCustomAttributesMethod1 = AccessTools.Method(MonoCustomAttrsType, "GetCustomAttributes", new[] { typeof(ICustomAttributeProvider), typeof(bool) });
            GetCustomAttributesMethod2 = AccessTools.Method(MonoCustomAttrsType, "GetCustomAttributes", new[] { typeof(ICustomAttributeProvider), typeof(Type), typeof(bool) });
            GetCustomAttributesBaseMethod = AccessTools.Method(MonoCustomAttrsType, "GetCustomAttributesBase", new[] { typeof(ICustomAttributeProvider), typeof(Type), typeof(bool) });
            GetCustomAttributesHookMethod = new HarmonyMethod(typeof(HarmonyExtentions), nameof(GetCustomAttributesHook));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Harmony Create(string id = null) =>
            new Harmony(id ?? $"harmony-auto-{Guid.NewGuid()}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Harmony CreateAndPatchAll(Type type, bool ignoreTypeLoadException) =>
            CreateAndPatchAll(type, null, ignoreTypeLoadException);

        public static Harmony CreateAndPatchAll(Type type, string harmonyInstanceId = null, bool ignoreTypeLoadException = true)
        {
            var harmony = Create(harmonyInstanceId);
            harmony.PatchAll(type, ignoreTypeLoadException);
            return harmony;
        }

        public static void PatchAll(this Harmony harmony, Type type, bool ignoreTypeLoadException = true)
        {
            if (ignoreTypeLoadException)
                harmony.Patch(GetCustomAttributesMethod1, GetCustomAttributesHookMethod);
            harmony.CreateClassProcessor(type, allowUnannotatedType: true).Patch();
            if (ignoreTypeLoadException)
                harmony.Unpatch(GetCustomAttributesMethod1, GetCustomAttributesHookMethod.method);
        }

        private static bool GetCustomAttributesHook(ref object[] __result, ICustomAttributeProvider obj, bool inherit)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            try
            {
                if (!inherit)
                    __result = (GetCustomAttributesBaseMethod.Invoke(null, new object[] { obj, null, inherit }) as object[]).Clone() as object[];
                else
                    __result = GetCustomAttributesMethod2.Invoke(null, new object[] { obj, MonoCustomAttrsType, inherit }) as object[];
            }
            catch (Exception ex)
            {
                while (ex is not TypeLoadException)
                    if ((ex = ex.InnerException) == null)
                        throw;
                __result = new object[0];
            }
            return false;
        }
    }
    internal class PathExtentions
{
    public static string GetRelativePath(string basePath, string absolutePath, string emptyPath = "")
    {
        if (string.IsNullOrEmpty(absolutePath))
            return emptyPath;
        string relativeTo = !string.IsNullOrEmpty(basePath) ?
            Path.GetFullPath(basePath) : Directory.GetCurrentDirectory();
        string path = Path.GetFullPath(absolutePath);
        int relativeToLength = relativeTo.Length, pathLength = path.Length;
        if (relativeTo[relativeToLength - 1] != '\\')
            relativeToLength = (relativeTo += '\\').Length;
        if (path[pathLength - 1] != '\\')
            pathLength = (path += '\\').Length;
        int length = Math.Min(relativeToLength, pathLength), i = 0, pathStart = 0;
        while (i < length && PathCharEquals(relativeTo[i], path[i]))
            if (path[i++] == '\\')
                pathStart = i;
        if ((length = GetRootLength(path)) > pathStart)
            return path.Remove(Math.Max(pathLength - 1, length));
        int count = relativeTo.Skip(i).Where(c => c == '\\').Count();
        if ((length = count * 3 + (pathLength -= pathStart)) == 0)
            return emptyPath;
        StringBuilder sb = new StringBuilder(length);
        sb.Insert(0, "..\\", count).Append(path, pathStart, pathLength).Length--;
        return sb.ToString();
    }

    public static int GetRootLength(string path)
    {
        int length = path.Length, i;
        if (length <= 0)
            return 0;
        char c;
        if ((c = path[0]) != '\\' && c != '/')
            return
                length >= 2 && path[1] == ':' && PathCharIsAlpha(c) ?
                length >= 3 && ((c = path[2]) == '\\' || c == '/') ?
                3 : 2 : 0;
        if (length < 2 || (c = path[1]) != '\\' && c != '/')
            return 1;
        i = length >= 4 && ((c = path[2]) == '.' || c == '?') && ((c = path[3]) == '\\' || c == '/') ?
            length >= 8 && ((c = path[7]) == '\\' || c == '/') && path[4] == 'U' && path[5] == 'N' && path[6] == 'C' ?
            8 : 4 : 2;
        for (int j = i != 4 ? 2 : 1; i < length;)
            if (((c = path[i++]) == '\\' || c == '/') && --j == 0)
                break;
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PathCharEquals(char lhs, char rhs) =>
        lhs == rhs ||
        (lhs |= (char)('a' - 'A')) == (rhs | (char)('a' - 'A')) &&
        (uint)(lhs - 'a') < 'z' - 'a' + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PathCharIsAlpha(char c) =>
        (uint)((c | ('a' - 'A')) - 'a') < 'z' - 'a' + 1;

#if false   // unused
    public static readonly char[] PathSeparators = new char[] { '\\', '/', ':' };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char PathCharToLower(char c) =>
        (uint)(c - 'A') < 'Z' - 'A' + 1 ? (char)(c + ('a' - 'A')) : c;

    public static bool PathEquals(string lhs, string rhs)
    {
        int length = lhs.Length;
        if (length != rhs.Length)
            return false;
        for (int i = 0; i < length; i++)
        {
            char a, b;
            if (PathCharEquals(a = lhs[i], b = rhs[i]))
                continue;
            if (a == '\\' ? b == '/' : a == '/' && b == '\\')
                continue;
            return false;
        }
        return true;
    }

    public class PathComparer : IComparer<string>
    {
        public int Compare(string lhs, string rhs)
        {
            int x = lhs.Length, y = rhs.Length;
            int count = Math.Min(x, y);
            for (int i = 0; i < count; i++)
            {
                char a, b;
                if (PathCharEquals(a = lhs[i], b = rhs[i]))
                    continue;
                if (a == '\\' ? b == '/' : a == '/' && b == '\\')
                    continue;
                if ((x = lhs.IndexOfAny(PathSeparators, i)) != -1)
                    x = (lhs[x] + 1) & 2;
                if ((y = rhs.IndexOfAny(PathSeparators, i)) != -1)
                    y = (rhs[y] + 1) & 2;
                if ((y -= x) != 0)
                    return y;
                return PathCharToLower(a) - PathCharToLower(b);
            }
            if (x > y)
                return lhs.IndexOfAny(PathSeparators, y) ^ -2;
            if (x < y)
                return rhs.IndexOfAny(PathSeparators, x) | 1;
            return 0;
        }
    }

    public static bool IsSubPathOf(this string subPath, string basePath)
    {
        subPath = Path.GetFullPath(Path.Combine(subPath ?? string.Empty, ".\\"));
        basePath = Path.GetFullPath(Path.Combine(basePath ?? string.Empty, ".\\"));
        return subPath.Length >= basePath.Length && PathEquals(subPath.Remove(basePath.Length), basePath);
    }
#endif
}
    #endif
        internal static partial class Hooks
        {
            /// <summary>
            /// A flag for disabling certain events when importing KK cards to EC. Should always be set to false in InstallHooks for KK and always remain false.
            /// </summary>
            private static bool DoingImport;

            internal static void InstallHooks()
            {
#if !RG
                Harmony.CreateAndPatchAll(typeof(Hooks), "Sideloader.UniversalAutoResolver");
#else
                HarmonyExtentions.CreateAndPatchAll(typeof(Hooks), "Sideloader.UniversalAutoResolver");
#endif

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
#if !RG
                string cardName = file.charaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = file.parameter?.fullname?.Trim();
                Sideloader.Logger.LogDebug($"Loading card [{cardName}]");

                var extData = ExtendedSave.GetExtendedDataById(file, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(file, UARExtID);
#else
                string cardName = file.CharaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = file.Parameter?.fullname?.Trim();
                Sideloader.Logger.LogDebug($"Loading card [{cardName}]");

                var extData = ExtendedSave.GetExtendedDataById(file, UARExtID) ?? ExtendedSave.GetExtendedDataById(file, UARExtIDOld);
#endif
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
#elif RG
                // Resolve the bundleID to the same ID as the hair
                if (file.Custom.hair != null)
                    foreach (var hairPart in file.Custom.hair.parts)
                        if (hairPart.id > BaseSlotID)
                            hairPart.bundleId = hairPart.id;
                foreach (var hairPart in file.Coordinate.SelectMany(coordinate => coordinate.hair.parts))
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
#elif AI || HS2 || RG
                            if (Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(slot))
#endif
                                continue;

                        //For accessories, make sure we're checking the appropriate category
                        if (obj is ChaFileAccessory.PartsInfo)
                        {
                            ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)obj;

                            if ((int)kv.Key.Category != AccessoryInfo.type)
                            {
                                //If the current category does not match the accessory's category do not attempt a resolution info lookup
                                continue;
                            }
                        }
                        else if (kv.Key.Prefix == StructReference.AccessoryPropPrefix)
                        {
                            // If we are not an accessory then skip trying to resolve accessory props
                            continue;
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
#elif RG
                // Resolve the bundleID to the same ID as the hair
                if (file.Custom.hair != null)
                    foreach (var hairPart in file.Custom.hair.parts)
                        if (hairPart.id > BaseSlotID)
                            hairPart.bundleId = hairPart.id;
                foreach (var hairPart in file.Coordinate.SelectMany(coordinate => coordinate.hair.parts))
                    if (hairPart.id > BaseSlotID)
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

#if !RG
                string cardName = __instance.charaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = __instance.parameter?.fullname?.Trim();

                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtID);
#else
                string cardName = __instance.CharaFileName;
                if (cardName.IsNullOrEmpty())
                    cardName = __instance.Parameter?.fullname?.Trim();

                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtID) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld);
                if (extData == null)
                    return;
#endif

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
                    Sideloader.Logger.LogError($"Reloading card [{cardName}] failed - Unknown data type:{extData.data["info"]?.GetType()}");
                    return;
                }

                Sideloader.Logger.LogDebug($"Reloading card [{cardName}] - External info count: {extInfo.Count}");

                if (Sideloader.DebugLogging.Value)
                {
                    foreach (ResolveInfo info in extInfo)
                        Sideloader.Logger.LogDebug($"External info: {info.GUID} : {info.Property} : {info.Slot}");
                }

                // Create a property name lookup to speed up saving (on the order of 4 minutes -> 15 seconds when saving fully populated KK game)
                var extInfoLookup = new Dictionary<string, ResolveInfo>();
                // To keep backwards compatibility, only the first entry with a given property in the extInfo list should be used if there are multiple.
                // Achieve this by adding items in reverse so the first instance overwrites the second instance that was already set.
                // There should be no duplicates most of the time so it's faster to do it this way than check if item exists on each iteration.
                for (var index = extInfo.Count - 1; index >= 0; index--)
                {
                    var info = extInfo[index];
                    extInfoLookup[info.Property] = info;
                }

                void ResetStructResolveStructure(Dictionary<CategoryProperty, StructValue<int>> propertyDict, object structure, IEnumerable<ResolveInfo> extInfo2, string propertyPrefix = "")
                {
                    foreach (var kv in propertyDict)
                    {
                        // Old and slow: var extResolve = extInfo.FirstOrDefault(x => x.Property == $"{propertyPrefix}{kv.Key}");
                        extInfoLookup.TryGetValue($"{propertyPrefix}{kv.Key}", out var extResolve);

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
#elif RG
                // Resolve the bundleID to the same ID as the hair
                if (__instance.Custom.hair != null)
                    foreach (var hairPart in __instance.Custom.hair.parts)
                        if (hairPart.id > BaseSlotID)
                            hairPart.bundleId = hairPart.id;
                foreach (var hairPart in __instance.Coordinate.SelectMany(coordinate => coordinate.hair.parts))
                    if (hairPart.id > BaseSlotID)
                            hairPart.bundleId = hairPart.id;
#endif
            }

            #endregion

            #region ChaFileCoordinate

            internal static void ExtendedCoordinateLoad(ChaFileCoordinate file)
            {
                Sideloader.Logger.LogDebug($"Loading coordinate [{file.coordinateName}]");

#if !RG
                var extData = ExtendedSave.GetExtendedDataById(file, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(file, UARExtID);
#else
                var extData = ExtendedSave.GetExtendedDataById(file, UARExtID) ?? ExtendedSave.GetExtendedDataById(file, UARExtIDOld);
#endif
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
#elif AI || HS2 || RG
                            if (Lists.InternalDataList[(int)kv.Key.Category].ContainsKey(slot))
#endif
                                continue;

                        //For accessories, make sure we're checking the appropriate category
                        if (obj is ChaFileAccessory.PartsInfo)
                        {
                            ChaFileAccessory.PartsInfo AccessoryInfo = (ChaFileAccessory.PartsInfo)obj;

                            if ((int)kv.Key.Category != AccessoryInfo.type)
                            {
                                //If the current category does not match the accessory's category do not attempt a resolution info lookup
                                continue;
                            }
                        }
                        else if (kv.Key.Prefix == StructReference.AccessoryPropPrefix)
                        {
                            // If we are not an accessory then skip trying to resolve accessory props
                            continue;
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

#if !RG
                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtID);
#else
                var extData = ExtendedSave.GetExtendedDataById(__instance, UARExtID) ?? ExtendedSave.GetExtendedDataById(__instance, UARExtIDOld);
                if (extData == null)
                    return;
#endif

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
#elif RG
            /// <summary>
            /// Find the head preset data
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            private static void LoadFacePresetPrefix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                __state = null;

                ChaListControl chaListCtrl = Singleton<Character>.Instance._chaListCtrl;
                ListInfoBase listInfo = chaListCtrl.GetListInfo(__instance.Parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.Custom.face.headId);
                if (listInfo == null) return;

                string preset = listInfo.GetInfo(ChaListDefine.KeyType.Preset);
                string guid = null;

                int headID = __instance.Custom.face.headId;
                if (headID >= BaseSlotID)
                {
                    var resolveinfo = TryGetResolutionInfo(__instance.Parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.Custom.face.headId);
                    if (resolveinfo == null) return;

                    headID = resolveinfo.Slot;
                    guid = resolveinfo.GUID;
                }

                var headPresetInfo = TryGetHeadPresetInfo(headID, guid, preset);
                __state = headPresetInfo;
            }

            /// <summary>
            /// Use the head preset data to resolve the IDs
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            internal static void LoadFacePresetPostfix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                if (__state == null) return;

                var faceResolveInfos = new List<ResolveInfo>();
                var makeupResolveInfos = new List<ResolveInfo>();
                var structref = __instance.Parameter.sex == 0 ? StructReference.ChaFileFacePropertiesMale : StructReference.ChaFileFacePropertiesFemale;

                foreach (var property in structref)
                {
                    if (__state.FaceData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.Custom.face), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.FaceData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.FaceData[property.Key.Property], null, null, null);
                    else
                        faceResolveInfos.Add(resolveinfo);
                }

                foreach (var property in StructReference.ChaFileMakeupProperties)
                {
                    if (__state.MakeupData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.Custom.face.makeup), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.MakeupData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.MakeupData[property.Key.Property], null, null, null);
                    else
                        makeupResolveInfos.Add(resolveinfo);
                }

                ResolveStructure(structref, __instance.Custom.face, faceResolveInfos);
                ResolveStructure(StructReference.ChaFileMakeupProperties, __instance.Custom.face.makeup, makeupResolveInfos);
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

#if AI || HS2 || RG

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
#if !RG
            [HarmonyPrefix, HarmonyPatch(typeof(CvsC_CreateCoordinateFile), nameof(CvsC_CreateCoordinateFile.CreateCoordinateFile))]
            internal static void ChaFileConstructor()
            {
                ChaControl chaCtrl = Singleton<CustomBase>.Instance?.chaCtrl;
                if (chaCtrl?.chaFile?.parameter != null && chaCtrl?.chaFile?.coordinate?.clothes != null)
                    StoreSexOnClothes(chaCtrl.chaFile.parameter.sex, chaCtrl.chaFile.coordinate.clothes);
            }
#else
            [HarmonyPrefix, HarmonyPatch(typeof(CvsC_CreateCoordinateFile), nameof(CvsC_CreateCoordinateFile.CreateCoordinateFileBefore))]
            private static void CreateCoordinateFileBeforePreHook()
            {
                var chaFile = Singleton<CustomBase>.Instance?.chaCtrl?.ChaFile;
                if (chaFile is null)
                    return;
                var parameter = chaFile.Parameter;
                if (parameter is null)
                    return;
                var sex = parameter.sex;
                var coordinates = chaFile.Coordinate;
                if (coordinates is null)
                    return;
                for (int i = 0; i < coordinates.Count; i++)
                {
                    var clothes = coordinates[i]?.clothes;
                    if (clothes is not null)
                        StoreSexOnClothes(sex, clothes);
                }
            }
#endif
#endif
        }
    }
}
