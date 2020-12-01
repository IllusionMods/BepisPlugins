using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ICSharpCode.SharpZipLib.Zip;
using Shared;
using Sideloader.AutoResolver;
using Sideloader.ListLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using XUnity.ResourceRedirector;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader
{
    /// <summary>
    /// Allows for loading mods in .zip format from the mods folder and automatically resolves ID conflicts.
    /// </summary>
    public partial class Sideloader
    {
        /// <summary> Plugin GUID </summary>
        public const string GUID = "com.bepis.bepinex.sideloader";
        /// <summary> Plugin name </summary>
        public const string PluginName = "Sideloader";
        /// <summary> Plugin version </summary>
        public const string Version = BepisPlugins.Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;

        /// <summary> Directory from which to load mods </summary>
        public static string ModsDirectory { get; } = Path.Combine(Paths.GameRootPath, "mods");
        private readonly List<ZipFile> Archives = new List<ZipFile>();

        /// <summary> List of all loaded manifest files </summary>
        public static readonly Dictionary<string, Manifest> Manifests = new Dictionary<string, Manifest>();
        /// <summary> Dictionary of GUID and loaded zip file name </summary>
        public static readonly Dictionary<string, string> ZipArchives = new Dictionary<string, string>();
        /// <summary> List of all loaded manifest files </summary>
        [Obsolete("Use Manifests or GetManifest")]
        public static List<Manifest> LoadedManifests;

        private static readonly Dictionary<string, ZipFile> PngList = new Dictionary<string, ZipFile>();
        private static readonly HashSet<string> PngFolderList = new HashSet<string>();
        private static readonly HashSet<string> PngFolderOnlyList = new HashSet<string>();
        private readonly List<ResolveInfo> _gatheredResolutionInfos = new List<ResolveInfo>();
        private readonly List<MigrationInfo> _gatheredMigrationInfos = new List<MigrationInfo>();

        internal static ConfigEntry<bool> MissingModWarning { get; private set; }
        internal static ConfigEntry<bool> DebugLogging { get; private set; }
        internal static ConfigEntry<bool> DebugLoggingResolveInfo { get; private set; }
        internal static ConfigEntry<bool> DebugLoggingModLoading { get; private set; }
        internal static ConfigEntry<bool> KeepMissingAccessories { get; private set; }
        internal static ConfigEntry<bool> MigrationEnabled { get; private set; }
        internal static ConfigEntry<string> AdditionalModsDirectory { get; private set; }

        internal void Awake()
        {
#if KK
            ZipConstants.DefaultCodePage = 0;
#endif
            Logger = base.Logger;

            Hooks.InstallHooks();
            UniversalAutoResolver.Hooks.InstallHooks();
            Lists.Hooks.InstallHooks();

            ResourceRedirection.EnableSyncOverAsyncAssetLoads();
            ResourceRedirection.EnableRedirectMissingAssetBundlesToEmptyAssetBundle(-1000);
            ResourceRedirection.EnableRandomizeCabIfConflict(-2000, false);
            ResourceRedirection.RegisterAsyncAndSyncAssetLoadingHook(RedirectHook);
            ResourceRedirection.RegisterAsyncAndSyncAssetBundleLoadingHook(AssetBundleLoadingHook);

            MissingModWarning = Config.Bind("Logging", "Show missing mod warnings", true,
                "Whether missing mod warnings will be displayed on screen. Messages will still be written to the log.");
            DebugLogging = Config.Bind("Logging", "Debug logging", false,
                "Enable additional logging useful for debugging issues with Sideloader and sideloader mods.\nWarning: Will increase load and save times noticeably and will result in very large log sizes.");
            DebugLoggingResolveInfo = Config.Bind("Logging", "Debug resolve info logging", false,
                "Enable verbose logging for debugging issues with Sideloader and sideloader mods.\nWarning: Will increase game start up time and will result in very large log sizes.");
            DebugLoggingModLoading = Config.Bind("Logging", "Debug mod loading logging", false,
                "Enable verbose logging when loading mods.");

            KeepMissingAccessories = Config.Bind("General", "Keep missing accessories", false,
                "Missing accessories will be replaced by a default item with color and position information intact when loaded in the character maker.");
            MigrationEnabled = Config.Bind("General", "Migration enabled", true,
                "Attempt to change the GUID and/or ID of mods based on the data configured in the manifest.xml. Helps keep backwards compatibility when updating mods.");
            AdditionalModsDirectory = Config.Bind("General", "Additional mods directory", FindKoiZipmodDir(),
                "Additional directory to load zipmods from.");

            if (!Directory.Exists(ModsDirectory))
                Logger.LogWarning("Could not find the mods directory: " + ModsDirectory);

            if (!AdditionalModsDirectory.Value.IsNullOrWhiteSpace() && !Directory.Exists(AdditionalModsDirectory.Value))
                Logger.LogWarning("Could not find the additional mods directory specified in config: " + AdditionalModsDirectory.Value);

            LoadModsFromDirectories(ModsDirectory, AdditionalModsDirectory.Value);
        }

        private static string GetRelativeArchiveDir(string archiveDir) => archiveDir.Length < ModsDirectory.Length ? archiveDir : archiveDir.Substring(ModsDirectory.Length).Trim(' ', '/', '\\');

        private void LoadModsFromDirectories(params string[] modDirectories)
        {
            var stopWatch = Stopwatch.StartNew();

            // Look for mods, load their manifests
            var allMods = new List<string>();
            foreach (var modDirectory in modDirectories)
            {
                if (!modDirectory.IsNullOrWhiteSpace() && Directory.Exists(modDirectory))
                {
                    var prevCount = allMods.Count;

                    allMods.AddRange(Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories)
                        .Where(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                    x.EndsWith(".zipmod", StringComparison.OrdinalIgnoreCase)));

                    Logger.LogInfo("Found " + (allMods.Count - prevCount) + " zipmods in directory: " + modDirectory);
                }
            }

            var archives = allMods.RunParallel(archivePath =>
            {
                ZipFile archive = null;
                try
                {
                    archive = new ZipFile(archivePath);

                    if (Manifest.TryLoadFromZip(archive, out Manifest manifest))
                    {
                        //Skip the mod if it is not for this game
                        if (!manifest.Game.IsNullOrWhiteSpace() && !GameNameList.Contains(manifest.Game.ToLower().Replace("!", "")))
                        {
                            Logger.LogInfo($"Skipping archive \"{GetRelativeArchiveDir(archivePath)}\" because it's meant for {manifest.Game}");
                            return null;
                        }

                        return new { archive, manifest };
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to load archive \"{GetRelativeArchiveDir(archivePath)}\" with error: {ex}");
                    archive?.Close();
                }
                return null;
            }, 3).Where(x => x != null).ToList();

            var enableModLoadingLogging = DebugLoggingModLoading.Value;
            var modLoadInfoSb = enableModLoadingLogging ? new StringBuilder(1000) : null;

            // Handle duplicate GUIDs and load unique mods
            foreach (var modGroup in archives.GroupBy(x => x.manifest.GUID).OrderBy(x => x.Key))
            {
                // Order by version if available, else use modified dates (less reliable)
                // If versions match, prefer mods inside folders or with more descriptive names so modpacks are preferred
                var orderedModsQuery = modGroup.All(x => !string.IsNullOrEmpty(x.manifest.Version))
                    ? modGroup.OrderByDescending(x => x.manifest.Version, new ManifestVersionComparer()).ThenByDescending(x => x.archive.Name.Length)
                    : modGroup.OrderByDescending(x => File.GetLastWriteTime(x.archive.Name));

                var orderedMods = orderedModsQuery.ToList();

                if (orderedMods.Count > 1)
                {
                    var modList = string.Join(", ", orderedMods.Skip(1).Select(x => '"' + GetRelativeArchiveDir(x.archive.Name) + '"').ToArray());
                    Logger.LogWarning($"Multiple versions detected, only \"{GetRelativeArchiveDir(orderedMods[0].archive.Name)}\" will be loaded. Skipped versions: {modList}");

                    // Don't keep the duplicate archives in memory
                    foreach (var dupeMod in orderedMods.Skip(1))
                        dupeMod.archive.Close();
                }

                // Actually load the mods (only one per GUID, the newest one)
                var archive = orderedMods[0].archive;
                var manifest = orderedMods[0].manifest;
                try
                {
                    Archives.Add(archive);
                    ZipArchives[manifest.GUID] = archive.Name;
                    Manifests[manifest.GUID] = manifest;

                    LoadAllUnityArchives(archive, archive.Name);
                    LoadAllLists(archive, manifest);
                    BuildPngFolderList(archive);

                    UniversalAutoResolver.GenerateMigrationInfo(manifest, _gatheredMigrationInfos);
#if AI || HS2
                    UniversalAutoResolver.GenerateHeadPresetInfo(manifest, _gatheredHeadPresetInfos);
                    UniversalAutoResolver.GenerateFaceSkinInfo(manifest, _gatheredFaceSkinInfos);
#endif

                    var trimmedName = manifest.Name?.Trim();
                    var displayName = !string.IsNullOrEmpty(trimmedName) ? trimmedName : Path.GetFileName(archive.Name);

                    if (enableModLoadingLogging)
                        modLoadInfoSb.AppendLine($"Loaded {displayName} {manifest.Version}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to load archive \"{GetRelativeArchiveDir(archive.Name)}\" with error: {ex}");
                }
            }

            UniversalAutoResolver.SetResolveInfos(_gatheredResolutionInfos);
            UniversalAutoResolver.SetMigrationInfos(_gatheredMigrationInfos);
#if AI || HS2
            UniversalAutoResolver.SetHeadPresetInfos(_gatheredHeadPresetInfos);
            UniversalAutoResolver.SetFaceSkinInfos(_gatheredFaceSkinInfos);
            UniversalAutoResolver.ResolveFaceSkins();
#endif

            BuildPngOnlyFolderList();

#pragma warning disable CS0618 // Type or member is obsolete
            LoadedManifests = Manifests.Values.AsEnumerable().ToList();
#pragma warning restore CS0618 // Type or member is obsolete

            stopWatch.Stop();

            if (enableModLoadingLogging)
                Logger.LogInfo($"List of loaded mods:\n{modLoadInfoSb}");
            Logger.LogInfo($"Successfully loaded {Archives.Count} mods out of {allMods.Count} archives in {stopWatch.ElapsedMilliseconds}ms");

            var failedPaths = allMods.Except(Archives.Select(x => x.Name));
            var failedStrings = failedPaths.Select(GetRelativeArchiveDir).ToArray();
            if (failedStrings.Length > 0)
                Logger.LogWarning("Could not load " + failedStrings.Length + " mods, see previous warnings for more information. File names of skipped archives:\n" + string.Join(" | ", failedStrings));
        }

        private void LoadAllLists(ZipFile arc, Manifest manifest)
        {
            List<ZipEntry> BoneList = new List<ZipEntry>();
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.StartsWith("abdata/list/characustom", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var stream = arc.GetInputStream(entry);
                        var chaListData = Lists.LoadCSV(stream);

                        SetPossessNew(chaListData);
                        UniversalAutoResolver.GenerateResolutionInfo(manifest, chaListData, _gatheredResolutionInfos);
                        Lists.ExternalDataList.Add(chaListData);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to load list file \"{entry.Name}\" from archive \"{GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                    }
                }
#if KK || AI || HS2
                else if (entry.Name.StartsWith("abdata/studio/info", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    if (Path.GetFileNameWithoutExtension(entry.Name).ToLower().StartsWith("itembonelist_"))
                        BoneList.Add(entry);
                    else
                    {
                        try
                        {
                            var stream = arc.GetInputStream(entry);
                            var studioListData = Lists.LoadStudioCSV(stream, entry.Name, manifest.GUID);

                            UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                            Lists.ExternalStudioDataList.Add(studioListData);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to load list file \"{entry.Name}\" from archive \"{GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                        }
                    }
                }
#if AI || HS2
                else if (entry.Name.StartsWith("abdata/list/map/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string assetBundleName = entry.Name;
                        assetBundleName = assetBundleName.Remove(0, assetBundleName.IndexOf('/') + 1); //Remove "abdata/"
                        assetBundleName = assetBundleName.Remove(assetBundleName.LastIndexOf('/')); //Remove the .csv filename
                        assetBundleName += ".unity3d";

                        string assetName = entry.Name;
                        assetName = assetName.Remove(0, assetName.LastIndexOf('/') + 1); //Remove all but the filename
                        assetName = assetName.Remove(assetName.LastIndexOf('.')); //Remove the .csv

                        var stream = arc.GetInputStream(entry);
                        Lists.LoadExcelDataCSV(assetBundleName, assetName, stream);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to load list file \"{entry.Name}\" from archive \"{GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                    }
                }
#endif
#endif
            }

#if KK || AI || HS2
            //ItemBoneList data must be resolved after the corresponding item so they can be resolved to the same ID
            foreach (ZipEntry entry in BoneList)
            {
                try
                {
                    var stream = arc.GetInputStream(entry);
                    var studioListData = Lists.LoadStudioCSV(stream, entry.Name, manifest.GUID);

                    UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                    Lists.ExternalStudioDataList.Add(studioListData);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to load list file \"{entry.Name}\" from archive \"{GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                }
            }
#endif
        }

        private void SetPossessNew(ChaListData data)
        {
            for (int i = 0; i < data.lstKey.Count; i++)
            {
                if (data.lstKey[i] == "Possess")
                {
                    foreach (var kv in data.dictList)
                        kv.Value[i] = "1";
                    break;
                }
            }
        }
        /// <summary>
        /// Construct a list of all folders that contain a .png
        /// </summary>
        private void BuildPngFolderList(ZipFile arc)
        {
            foreach (ZipEntry entry in arc)
            {
                //Only list folders for .pngs in abdata folder
                //i.e. skip preview pics or character cards that might be included with the mod
                if (entry.Name.StartsWith("abdata/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    string assetBundlePath = entry.Name;
                    assetBundlePath = assetBundlePath.Remove(0, assetBundlePath.IndexOf('/') + 1); //Remove "abdata/"

                    //Make a list of all the .png files and archive they come from
                    if (PngList.ContainsKey(entry.Name))
                    {
                        if (DebugLoggingModLoading.Value)
                            Logger.LogWarning($"Duplicate .png asset detected! {assetBundlePath} in \"{GetRelativeArchiveDir(arc.Name)}\"");
                    }
                    else
                        PngList.Add(entry.Name, arc);

                    assetBundlePath = assetBundlePath.Remove(assetBundlePath.LastIndexOf('/')); //Remove the .png filename
                    if (!PngFolderList.Contains(assetBundlePath))
                    {
                        //Make a unique list of all folders that contain a .png
                        PngFolderList.Add(assetBundlePath);
                    }
                }
            }
        }
        /// <summary>
        /// Build a list of folders that contain .pngs but do not match an existing asset bundle
        /// </summary>
        private void BuildPngOnlyFolderList()
        {
            foreach (string folder in PngFolderList) //assetBundlePath
            {
                string assetBundlePath = folder + ".unity3d";

                //The file exists at this location, no need to add a bundle
                if (File.Exists(Application.dataPath + "/../abdata/" + assetBundlePath))
                    continue;

                //Bundle has already been added by LoadAllUnityArchives
                if (BundleManager.Bundles.ContainsKey(assetBundlePath))
                    continue;

                PngFolderOnlyList.Add(folder);
            }
        }

        /// <summary>
        /// Check whether the asset bundle matches a folder that contains .png files and does not match an existing asset bundle
        /// </summary>
        public static bool IsPngFolderOnly(string assetBundleName)
        {
            var extStart = assetBundleName.LastIndexOf('.');
            var trimmedName = extStart >= 0 ? assetBundleName.Remove(extStart) : assetBundleName;
            return PngFolderOnlyList.Contains(trimmedName);
        }

        /// <summary>
        /// Check if a mod with specified GUID has been loaded.
        /// </summary>
        [Obsolete("Use GetManifest and check null instead")]
        public bool IsModLoaded(string guid) => GetManifest(guid) != null;

        /// <summary>
        /// Check if a mod with specified GUID has been loaded and fetch its manifest.
        /// Returns null if there was no mod with this guid loaded.
        /// </summary>
        /// <param name="guid">GUID of the mod.</param>
        /// <returns>Manifest of the loaded mod or null if mod is not loaded.</returns>
        public static Manifest GetManifest(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            Manifests.TryGetValue(guid, out Manifest manifest);
            return manifest;
        }

        /// <summary>
        /// Get a list of file paths to all png files inside the loaded mods
        /// </summary>
        public static IEnumerable<string> GetPngNames() => PngList.Keys;

        /// <summary>
        /// Get a new copy of the png file if it exists in any of the loaded zipmods
        /// </summary>
        public static Texture2D GetPng(string pngPath, TextureFormat format = TextureFormat.RGBA32, bool mipmap = true)
        {
            if (string.IsNullOrEmpty(pngPath))
                return null;

            //Only search the archives for a .png that can actually be found
            if (PngList.TryGetValue(pngPath, out ZipFile archive))
            {
                var entry = archive.GetEntry(pngPath);

                if (entry != null)
                {
                    // Load png byte data from the archive and load it into a new texture
                    var stream = archive.GetInputStream(entry);
                    var fileLength = (int)entry.Size;
                    var buffer = new byte[fileLength];
                    stream.Read(buffer, 0, fileLength);
                    var tex = new Texture2D(2, 2, format, mipmap);
                    tex.LoadImage(buffer);

                    if (pngPath.Contains("clamp"))
                        tex.wrapMode = TextureWrapMode.Clamp;
                    else if (pngPath.Contains("repeat"))
                        tex.wrapMode = TextureWrapMode.Repeat;

                    return tex;
                }
            }

            return null;
        }
        /// <summary>
        /// Check whether the .png file comes from a sideloader mod
        /// </summary>
        public static bool IsPng(string pngFile) => PngList.ContainsKey(pngFile);

        private static readonly MethodInfo locateZipEntryMethodInfo = typeof(ZipFile).GetMethod("LocateEntry", AccessTools.all);

        private void LoadAllUnityArchives(ZipFile arc, string archiveFilename)
        {
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
                {
                    string assetBundlePath = entry.Name;

                    if (assetBundlePath.Contains('/'))
                        assetBundlePath = assetBundlePath.Remove(0, assetBundlePath.IndexOf('/') + 1);

                    AssetBundle getBundleFunc()
                    {
                        AssetBundle bundle;

                        if (entry.CompressionMethod == CompressionMethod.Stored)
                        {
                            long index = (long)locateZipEntryMethodInfo.Invoke(arc, new object[] { entry });

                            if (DebugLogging.Value)
                                Logger.LogDebug($"Streaming \"{entry.Name}\" ({GetRelativeArchiveDir(archiveFilename)}) unity3d file from disk, offset {index}");

                            bundle = AssetBundle.LoadFromFile(archiveFilename, 0, (ulong)index);
                        }
                        else
                        {
                            Logger.LogDebug($"Cannot stream \"{entry.Name}\" ({GetRelativeArchiveDir(archiveFilename)}) unity3d file from disk, loading to RAM instead");
                            var stream = arc.GetInputStream(entry);

                            byte[] buffer = new byte[entry.Size];

                            stream.Read(buffer, 0, (int)entry.Size);

                            // The line below can either be commented in or out - it doesn't really matter. 
                            //  - If in: It will generate successive unique CAB-strings for these asset bundles
                            //  - If out: The CAB of the actual asset bundle will be used if possible, otherwise a random CAB is generated by the Resource Redirector due to the call to 'ResourceRedirection.EnableRandomizeCabIfConflict(-2000, false)'
                            //BundleManager.RandomizeCAB(buffer);

                            bundle = AssetBundleHelper.LoadFromMemory($"\"{entry.Name}\" ({GetRelativeArchiveDir(archiveFilename)})", buffer, 0);
                        }

                        if (bundle == null)
                        {
                            Logger.LogError($"Asset bundle \"{entry.Name}\" ({GetRelativeArchiveDir(archiveFilename)}) failed to load. It might have a conflicting CAB string.");
                        }

                        return bundle;
                    }

                    BundleManager.AddBundleLoader(getBundleFunc, assetBundlePath, out string warning);

                    if (!string.IsNullOrEmpty(warning) && DebugLoggingModLoading.Value)
                        Logger.LogWarning($"{warning} in \"{GetRelativeArchiveDir(archiveFilename)}\"");
                }
            }
        }

        /// <summary>
        /// Try to get ExcelData that was originally in .csv form in the mod
        /// </summary>
        /// <param name="assetBundleName">Name of the folder containing the .csv file</param>
        /// <param name="assetName">Name of the .csv file without the file extension</param>
        /// <param name="excelData">ExcelData or null if none exists</param>
        /// <returns>True if ExcelData was returned</returns>
        public static bool TryGetExcelData(string assetBundleName, string assetName, out ExcelData excelData)
        {
            excelData = null;
            if (Lists.ExternalExcelData.TryGetValue(assetBundleName, out var assets))
                if (assets.TryGetValue(assetName, out excelData))
                    return true;
            return false;
        }

        /// <summary>
        /// Check whether the asset bundle at the specified path is one managed by Sideloader
        /// </summary>
        /// <param name="assetBundlePath">Path to the asset bundle without the leading abdata, i.e. map/list/mapinfo/mymap.unity3d</param>
        /// <returns>True if the asset bundle is managed by Sideloader, false if not (doesn't exist, vanilla asset bundle, etc)</returns>
        public static bool IsSideloaderAB(string assetBundlePath)
        {
            if (BundleManager.Bundles.ContainsKey(assetBundlePath))
                return true;
            if (Lists.ExternalExcelData.ContainsKey(assetBundlePath))
                return true;
            if (IsPngFolderOnly(assetBundlePath))
                return true;
            return false;
        }

        private void RedirectHook(IAssetLoadingContext context)
        {
            if (context.Parameters.Name == null || context.Bundle.name == null) return;

            if (typeof(Texture).IsAssignableFrom(context.Parameters.Type))
            {
                string zipPath = $"abdata/{context.Bundle.name.Replace(".unity3d", "", StringComparison.OrdinalIgnoreCase)}/{context.Parameters.Name}.png";

                var tex = GetPng(zipPath);
                if (tex != null)
                {
                    context.Asset = tex;
                    context.Complete();
                    return;
                }
            }
            if (context.Parameters.Type == typeof(ExcelData))
            {
                if (TryGetExcelData(context.Bundle.name, context.Parameters.Name, out var excelData))
                {
                    context.Asset = excelData;
                    context.Complete();
                    return;
                }
            }

            if (BundleManager.TryGetObjectFromName(context.Parameters.Name, context.Bundle.name, context.Parameters.Type, out UnityEngine.Object obj))
            {
                context.Asset = obj;
                context.Complete();
            }
        }

        private void AssetBundleLoadingHook(IAssetBundleLoadingContext context)
        {
            if (context.Parameters.LoadType != AssetBundleLoadType.LoadFromFile) return;

            var path = context.Parameters.Path;
            if (path == null) return;

            var abdataIndex = path.IndexOf("/abdata/");
            if (abdataIndex == -1) return;

            string bundle = path.Substring(abdataIndex).Replace("/abdata/", "");
            if (!File.Exists(path))
            {
                if (BundleManager.Bundles.TryGetValue(bundle, out List<LazyCustom<AssetBundle>> lazyList))
                {
                    context.Bundle = lazyList[0].Instance;
                    context.Bundle.name = bundle;
                    context.Complete();
                }
                else
                {
                    //Create a placeholder asset bundle for png files without a matching asset bundle
                    if (IsPngFolderOnly(bundle))
                    {
                        context.Bundle = AssetBundleHelper.CreateEmptyAssetBundle();
                        context.Bundle.name = bundle;
                        context.Complete();
                    }
                    //Placeholder for .csv excel data
                    else if (Lists.ExternalExcelData.ContainsKey(bundle))
                    {
                        context.Bundle = AssetBundleHelper.CreateEmptyAssetBundle();
                        context.Bundle.name = bundle;
                        context.Complete();
                    }
                }
            }
            else
            {
                var ab = AssetBundle.LoadFromFile(context.Parameters.Path, context.Parameters.Crc, context.Parameters.Offset);
                if (ab != null)
                {
                    context.Bundle = ab;
                    context.Bundle.name = bundle;
                    context.Complete();
                }
            }
        }
    }
}
