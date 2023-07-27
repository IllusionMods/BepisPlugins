using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Shared;
using Sideloader.AutoResolver;
using Sideloader.ListLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using XUnity.ResourceRedirector;
using MessagePack;
using System.Threading;
using MessagePack.Resolvers;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader
{
    /// <summary>
    /// Allows for loading mods in .zip format from the mods folder and automatically resolves ID conflicts.
    /// </summary>
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID, ExtensibleSaveFormat.ExtendedSave.Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
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
        /// <summary> Dictionary of loaded zip file name and its zipmod metadata </summary>
        internal static readonly Dictionary<string, ZipmodInfo> Zipmods = new Dictionary<string, ZipmodInfo>();

        /// <summary> Dictionary of GUID and loaded manifest files </summary>
        public static readonly Dictionary<string, Manifest> Manifests = new Dictionary<string, Manifest>();
        /// <summary> Dictionary of GUID and loaded zip file name </summary>
        public static readonly Dictionary<string, string> ZipArchives = new Dictionary<string, string>();
        /// <summary> List of all loaded manifest files </summary>
        [Obsolete("Use Manifests or GetManifest")]
        public static List<Manifest> LoadedManifests;

        private static readonly Dictionary<string, ZipmodInfo> PngList = new Dictionary<string, ZipmodInfo>();
        private static readonly HashSet<string> PngFolderList = new HashSet<string>();
        private static readonly HashSet<string> PngFolderOnlyList = new HashSet<string>();

        internal static ConfigEntry<bool> MissingModWarning { get; private set; }
        internal static ConfigEntry<bool> DebugLogging { get; private set; }
        internal static ConfigEntry<bool> DebugLoggingResolveInfo { get; private set; }
        internal static ConfigEntry<bool> DebugLoggingModLoading { get; private set; }
        internal static ConfigEntry<bool> RandomizeSlotIds { get; private set; }
        internal static ConfigEntry<bool> KeepMissingAccessories { get; private set; }
        internal static ConfigEntry<bool> MigrationEnabled { get; private set; }
        internal static ConfigEntry<string> AdditionalModsDirectory { get; private set; }
        internal static ConfigEntry<bool> CachingEnabled { get; private set; }

        [UsedImplicitly]
        private void Awake()
        {
#if KK      // Fixes an issue with reading some zips made on Japanese systems. Only needed on .Net 3.5, it doesn't affect newer Unity versions.
            ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = 0;
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

            RandomizeSlotIds = Config.Bind("General", "Randomize Slot IDs", true,
                new ConfigDescription("Helps detect bugs in Sideloader and other plugins by making them more obvious. If false, the bugs might not affect current game instance but will suddenly pop up after updating the game or sending scenes/cards to another users.", null, "Advanced"));
            KeepMissingAccessories = Config.Bind("General", "Keep missing accessories", false,
                "Missing accessories will be replaced by a default item with color and position information intact when loaded in the character maker.");
            MigrationEnabled = Config.Bind("General", "Migration enabled", true,
                "Attempt to change the GUID and/or ID of mods based on the data configured in the manifest.xml. Helps keep backwards compatibility when updating mods.");
            AdditionalModsDirectory = Config.Bind("General", "Additional mods directory", FindKoiZipmodDir(),
                "Additional directory to load zipmods from.");
            CachingEnabled = Config.Bind("General", "Cache zipmod metadata", true,
                "Drastically speeds up game startup speed, especially on slow HDDs, by not parsing zipmods unless they get changed. Disable to force sideloader to always read and parse all zipmods.");

            if (!Directory.Exists(ModsDirectory))
                Logger.LogWarning("Could not find the mods directory: " + ModsDirectory);

            if (!AdditionalModsDirectory.Value.IsNullOrWhiteSpace() && !Directory.Exists(AdditionalModsDirectory.Value))
                Logger.LogWarning("Could not find the additional mods directory specified in config: " + AdditionalModsDirectory.Value);

            LoadModsFromDirectories(ModsDirectory, AdditionalModsDirectory.Value);
        }

        #region Data loading

        private void LoadModsFromDirectories(params string[] modDirectories)
        {
            var swTotal = Stopwatch.StartNew();

            // ------------------------------------------------------
            // Find zipmod files on drive
            // ------------------------------------------------------

            var foundZipfiles = new List<ZipmodInfo>();
            var waitHandle = new ManualResetEvent(false);
            // Do as much as possible in background while the cache is being loaded (makes this essentially free)
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    foreach (var modDirectory in modDirectories.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
                    {
                        var swFiles = Stopwatch.StartNew();
                        var prevCount = foundZipfiles.Count;
                        foreach (var zipFile in Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories))
                        {
                            if (zipFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                zipFile.EndsWith(".zipmod", StringComparison.OrdinalIgnoreCase))
                                foundZipfiles.Add(new ZipmodInfo(zipFile));
                        }

                        Logger.LogInfo($"Found {(foundZipfiles.Count - prevCount)} zip/zipmod files in directory [{modDirectory}] in {swFiles.ElapsedMilliseconds}ms");
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Crash while searching for zip/zipmod files! " + e);
                }
                finally
                {
                    waitHandle.Set();
                }
            });

            // ------------------------------------------------------

            var cache = LoadCache();

            waitHandle.WaitOne();

            // ------------------------------------------------------
            // Load the zipmod metadata either from drive or cache
            // ------------------------------------------------------

            var groupedZipmodsToLoad = new Dictionary<string, List<ZipmodInfo>>();
            void AddZipmodToLoad(ZipmodInfo zipmodInfo)
            {
                if (!zipmodInfo.Valid) return;

                if (!groupedZipmodsToLoad.TryGetValue(zipmodInfo.Manifest.GUID, out var ziplist))
                {
                    ziplist = new List<ZipmodInfo>(1);
                    groupedZipmodsToLoad[zipmodInfo.Manifest.GUID] = ziplist;
                }

                ziplist.Add(zipmodInfo);
            }

            // Very fast
            foreach (var newInfo in foundZipfiles)
            {
                cache.TryGetValue(newInfo.FileName, out var cachedInfo);
                if (cachedInfo != null && newInfo.FileSize == cachedInfo.FileSize && newInfo.LastWriteTime == cachedInfo.LastWriteTime)
                {
                    Zipmods.Add(cachedInfo.FileName, cachedInfo);

                    // Replay error/info messages
                    if (cachedInfo.Error != null)
                    {
                        if (cachedInfo.Error.StartsWith("Skipping"))
                            Logger.LogInfo(cachedInfo.Error);
                        else
                            Logger.LogError(cachedInfo.Error);
                    }
                    else
                    {
                        AddZipmodToLoad(cachedInfo);
                    }
                }
                else
                {
                    try
                    {
                        if (cache.Count > 0 && DebugLogging.Value)
                            Logger.LogDebug($"Cache MISS for {newInfo.FileName}  -  {(cachedInfo != null ? "size/date changed" : "entry missing")}");

                        // Add to the mod list first so that it gets saved to the cache even if it throws an exception
                        Zipmods.Add(newInfo.FileName, newInfo);

                        var archive = newInfo.GetZipFile();

                        newInfo.Manifest = Manifest.LoadFromZip(archive);
                        //Skip the mod if it is not for this game
                        if (newInfo.Manifest.Games.Count != 0 && !newInfo.Manifest.Games.Select(x => x.ToLower()).Any(GameNameList.Contains))
                            throw new PlatformNotSupportedException();

                        newInfo.LoadAllLists();

                        AddZipmodToLoad(newInfo);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        var msg = $"Skipping archive \"{newInfo.RelativeFileName}\" because it's meant for {string.Join(", ", newInfo.Manifest.Games.ToArray())}";
                        Logger.LogInfo(msg);
                        newInfo.Error = msg;
                        newInfo.Dispose();
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Failed to load archive \"{newInfo.RelativeFileName}\" with error: {ex}";
                        Logger.LogError(msg);
                        newInfo.Error = msg;
                        newInfo.Dispose();
                    }
                }
            }

            // ------------------------------------------------------

            WriteCache();

            // ------------------------------------------------------
            // Actually load the zipmods to use in the game from the metadata
            // ------------------------------------------------------

            var enableModLoadingLogging = DebugLoggingModLoading.Value;
            var modLoadInfoSb = enableModLoadingLogging ? new StringBuilder(1000) : null;

            var gatheredResolutionInfos = new List<ResolveInfo>();
            var gatheredMigrationInfos = new List<MigrationInfo>();
#if AI || HS2
            var gatheredHeadPresetInfos = new List<HeadPresetInfo>();
            var gatheredFaceSkinInfos = new List<FaceSkinInfo>();
#endif
            var bundlesToLoad = new List<BundleLoadInfo>();
            // Load the mods (only one per GUID, the newest one). Whole loop takes around 280ms for 3800 items.
            foreach (var modGroup in groupedZipmodsToLoad.OrderBy(x => x.Key).Select(x => x.Value))
            {
                ZipmodInfo zipmod;
                // Handle multiple versions/copies of a single zipmod
                if (modGroup.Count > 1)
                {
                    List<ZipmodInfo> orderedMods;
                    try
                    {
                        // Order by version if available, else use modified dates (less reliable)
                        // If versions match, prefer mods inside folders or with more descriptive names so modpacks are preferred
                        var orderedModsQuery = modGroup.All(x => !string.IsNullOrEmpty(x.Manifest.Version))
                            ? modGroup.OrderByDescending(x => x.Manifest.Version, new ManifestVersionComparer()).ThenByDescending(x => x.FileName.Length)
                            : modGroup.OrderByDescending(x => x.LastWriteTime);

                        orderedMods = orderedModsQuery.ToList();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to sort versions of [{modGroup[0].Manifest.GUID}]: [{string.Join("] , [", modGroup.Select(x => x.Manifest.Version).ToArray())}] with error: {e}");
                        orderedMods = modGroup;
                    }

                    zipmod = orderedMods[0];

                    var modList = string.Join(", ", orderedMods.Skip(1).Select(x => '"' + x.RelativeFileName + '"').ToArray());
                    Logger.LogWarning($"Multiple versions detected, only \"{zipmod.RelativeFileName}\" will be loaded. Skipped versions: {modList}");

                    // Don't keep the duplicate archives in memory
                    foreach (var dupeMod in orderedMods.Skip(1))
                        dupeMod.Dispose();
                }
                else
                {
                    zipmod = modGroup[0];
                }

                try
                {
                    var manifest = zipmod.Manifest;
                    ZipArchives[manifest.GUID] = zipmod.FileName;
                    Manifests[manifest.GUID] = manifest;

                    bundlesToLoad.AddRange(zipmod.BundleInfos);
                    AddAllLists(zipmod, gatheredResolutionInfos);
                    BuildPngFolderList(zipmod);

                    gatheredMigrationInfos.AddRange(manifest.MigrationList);
#if AI || HS2
                    gatheredHeadPresetInfos.AddRange(manifest.HeadPresetList);
                    gatheredFaceSkinInfos.AddRange(manifest.FaceSkinList);
#endif

                    if (enableModLoadingLogging)
                    {
                        var trimmedName = manifest.Name?.Trim();
                        var displayName = !string.IsNullOrEmpty(trimmedName) ? trimmedName : Path.GetFileName(zipmod.FileName);
                        modLoadInfoSb.AppendLine($"Loaded {displayName} {manifest.Version}");
                    }

                    zipmod.Loaded = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to load archive \"{zipmod.RelativeFileName}\", state might be corrupted! Error: {ex}");
                }
            }

            // Past this point everything is very fast

            AddBundles(bundlesToLoad);

            UniversalAutoResolver.SetResolveInfos(gatheredResolutionInfos);
            UniversalAutoResolver.SetMigrationInfos(gatheredMigrationInfos);
#if AI || HS2
            UniversalAutoResolver.SetHeadPresetInfos(gatheredHeadPresetInfos);
            UniversalAutoResolver.SetFaceSkinInfos(gatheredFaceSkinInfos);
            UniversalAutoResolver.ResolveFaceSkins();
#endif

            BuildPngOnlyFolderList();

#pragma warning disable CS0618 // Type or member is obsolete
            LoadedManifests = Manifests.Values.AsEnumerable().ToList();
#pragma warning restore CS0618 // Type or member is obsolete

            // ------------------------------------------------------

            if (enableModLoadingLogging)
                Logger.LogInfo($"List of loaded mods:\n{modLoadInfoSb}");
            Logger.LogInfo($"Successfully loaded {Zipmods.Count(x => x.Value.Loaded)} mods out of {Zipmods.Count} archives in {swTotal.ElapsedMilliseconds}ms");

            var failedPaths = Zipmods.Values.Where(x => !x.Loaded).Select(x => x.RelativeFileName).ToArray();
            if (failedPaths.Length > 0)
                Logger.LogWarning("Could not load " + failedPaths.Length + " mods, see previous warnings for more information. File names of skipped archives:\n" + string.Join(" | ", failedPaths));
        }

        private static void AddAllLists(ZipmodInfo zipmod, List<ResolveInfo> gatheredResolutionInfos)
        {
            var manifest = zipmod.Manifest;
            foreach (var chaListData in zipmod.CharaLists)
            {
                UniversalAutoResolver.GenerateResolutionInfo(manifest, chaListData, gatheredResolutionInfos);
                Lists.ExternalDataList.Add(chaListData);
            }
#if !EC
            foreach (var studioListData in zipmod.StudioLists)
            {
                UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                if (!Lists.ExternalStudioDataList.TryGetValue(studioListData.AssetBundleName, out var listOfLists))
                {
                    listOfLists = new List<Lists.StudioListData>();
                    Lists.ExternalStudioDataList.Add(studioListData.AssetBundleName, listOfLists);
                }
                listOfLists.Add(studioListData);
            }
            foreach (var mapListData in zipmod.MapLists)
            {
                Lists.AddExcelDataCSV(mapListData);
            }
            foreach (var boneListData in zipmod.BoneLists)
            {
                UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, boneListData);
                if (!Lists.ExternalStudioDataList.TryGetValue(boneListData.AssetBundleName, out var listOfLists))
                {
                    listOfLists = new List<Lists.StudioListData>();
                    Lists.ExternalStudioDataList.Add(boneListData.AssetBundleName, listOfLists);
                }
                listOfLists.Add(boneListData);
            }
#endif
        }

        /// <summary>
        /// Construct a list of all folders that contain a .png
        /// </summary>
        private static void BuildPngFolderList(ZipmodInfo zipmod)
        {
            foreach (var pngAssetFilename in zipmod.PngNames)
            {
                //Make a list of all the .png files and archive they come from
                if (PngList.ContainsKey(pngAssetFilename))
                {
                    if (DebugLoggingModLoading.Value)
                        Logger.LogWarning($"Duplicate .png asset detected! {pngAssetFilename} in \"{zipmod.RelativeFileName}\"");
                }
                else
                    PngList.Add(pngAssetFilename, zipmod);

                string assetBundlePath = pngAssetFilename;
                assetBundlePath = assetBundlePath.Remove(0, assetBundlePath.IndexOf('/') + 1); //Remove "abdata/"
                assetBundlePath = assetBundlePath.Remove(assetBundlePath.LastIndexOf('/')); //Remove the .png filename
                if (!PngFolderList.Contains(assetBundlePath))
                {
                    //Make a unique list of all folders that contain a .png
                    PngFolderList.Add(assetBundlePath);
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

        private static void AddBundles(ICollection<BundleLoadInfo> bundleInfos)
        {
            // If using debug logging, look for duplicate override bundles
            if (DebugLoggingModLoading.Value)
            {
                // ToLookup keeps the order of items
                foreach (var bundleInfoGroup in bundleInfos.ToLookup(x => x.BundleTrimmedPath))
                {
                    if (bundleInfoGroup.Count() > 1)
                    {
                        var overrideList = bundleInfoGroup.Select((info, i) => $"{i + 1}: {GetRelativeArchiveDir(info.ArchiveFilename)}");
                        Logger.LogWarning($"AssetBundle at [{bundleInfoGroup.First().BundleFullPath}] has multiple overrides! " +
                                          $"Order in which zipmods are searched for assets:\n{string.Join("\n", overrideList.ToArray())}");
                    }
                }
            }

            foreach (var bundleLoadInfo in bundleInfos)
            {
                BundleManager.AddBundleLoader(bundleLoadInfo.LoadBundle, bundleLoadInfo.BundleTrimmedPath);
            }
        }

        #endregion

        #region Public API

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
            if (PngList.TryGetValue(pngPath, out ZipmodInfo zipmod))
            {
                var archive = zipmod.GetZipFile();
                var entry = archive.GetEntry(pngPath);

                if (entry != null)
                {
                    // Load png byte data from the archive and load it into a new texture
                    var stream = archive.GetInputStream(entry);
                    var fileLength = (int)entry.Size;
                    var buffer = new byte[fileLength];
                    _ = stream.Read(buffer, 0, fileLength);
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

        internal static string GetRelativeArchiveDir(string archiveDir)
        {
            if (archiveDir.StartsWith(ModsDirectory, StringComparison.OrdinalIgnoreCase))
                return archiveDir.Substring(ModsDirectory.Length).Trim(' ', '/', '\\');
            else
                return archiveDir;
        }

        #endregion

        #region Asset hooks

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

            if (BundleManager.TryGetObjectFromName(context.Parameters.Name, context.Bundle.name, context.Parameters.Type, out var obj))
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

            var abdataIndex = path.IndexOf("/abdata/", StringComparison.OrdinalIgnoreCase);
            if (abdataIndex == -1 || abdataIndex + "/abdata/".Length >= path.Length) return;

            string bundle = path.Substring(abdataIndex + "/abdata/".Length);
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

        #endregion

        #region Caching

        private static readonly string _CacheName = "sideloader_zipmod_cache.bin";
        private static readonly string _CacheDirectory = Paths.CachePath;
        private static readonly string _CachePath = Path.Combine(_CacheDirectory, _CacheName);
        private void WriteCache()
        {
            // Write all found zipmod metadata to the cache
            // Can NOT serialize in background while AddAllLists->GenerateStudioResolutionInfo is running since it modifies StudioResolveInfo.Entries (and possibly others)
            try
            {
                var swCacheWrite = Stopwatch.StartNew();

                // Clean up old cache files
                foreach (var file in Directory.GetFiles(_CacheDirectory, _CacheName + ".*"))
                    File.Delete(file);

                if (!CachingEnabled.Value) return;

                // Serialize in multiple threads to speed things up a little.
                // Scaling kind of sucks above 2 threads. Cache read: 938ms using 1 thread, 691ms using 2 threads, 665ms using 3 threads
                // Some items take much longer to serialize/deserialize making threads finish very unevenly, probably manifest size?
                var threadCount = Mathf.Clamp(Zipmods.Count / 1500, 1, SystemInfo.processorCount);
                var modsPerThread = (Zipmods.Count / threadCount) + 1;

                var wait = new ManualResetEvent(false);
                var finished = 0;
                for (int i = 0; i < threadCount; i++)
                {
                    var threadIndex = i;
                    var modsToSkip = threadIndex * modsPerThread;
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var filename = _CachePath + "." + threadIndex;
                        try
                        {
                            using (var fileStream = File.OpenWrite(filename))
                            {
                                var toSerialize = Zipmods.Values.Skip(modsToSkip).Take(modsPerThread).ToList();
#if KK
                                LZ4MessagePackSerializer.Serialize(fileStream, toSerialize);
#else
                                LZ4MessagePackSerializer.Serialize(fileStream, toSerialize, ContractlessStandardResolverAllowPrivate.Instance);
#endif
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.LogWarning($"Failed to save cache part [{filename}] with error: {e}");
                        }
                        finally
                        {
                            if (Interlocked.Add(ref finished, 1) >= threadCount)
                                wait.Set();
                        }
                    });
                }

                File.WriteAllText(_CachePath + ".ver", Info.Metadata.Version.ToString());
                wait.WaitOne();
                Logger.LogDebug($"Saved zipmod cache to \"{_CachePath}\" in {swCacheWrite.ElapsedMilliseconds}ms using {threadCount} threads");
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to save cache: " + e);
            }
        }

        private Dictionary<string, ZipmodInfo> LoadCache()
        {
            var cache = new Dictionary<string, ZipmodInfo>();
            if (!CachingEnabled.Value) return cache;
            try
            {
                var swCacheRead = Stopwatch.StartNew();
                if (File.Exists(_CachePath + ".ver"))
                {
                    var cacheVer = new Version(File.ReadAllText(_CachePath + ".ver"));
                    if (cacheVer != Info.Metadata.Version)
                        throw new Exception($"Cache version ({cacheVer}) doesn't match Sideloader version ({Info.Metadata.Version}), it has to be regenerated.");

                    var cachePartFiles = Directory.GetFiles(_CacheDirectory, _CacheName + ".*")
                                                  .Where(x => !x.EndsWith(".ver", StringComparison.OrdinalIgnoreCase)).ToList();
                    var wait = new ManualResetEvent(false);
                    var finished = 0;
                    for (var i = 0; i < cachePartFiles.Count; i++)
                    {
                        var cachePartFile = cachePartFiles[i];
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                using (var fileStream = File.OpenRead(cachePartFile))
                                {
                                    // LZ4 ends up about 80ms slower deserializing and 100ms slower serializing, but the file size is reduced from 16MB to 3.5MB.
                                    // Not sure if it's worth it in that case, depends on drive speed
#if KK
                                    var list = LZ4MessagePackSerializer.Deserialize<List<ZipmodInfo>>(fileStream);
#else
                                    var list = LZ4MessagePackSerializer.Deserialize<List<ZipmodInfo>>(fileStream, ContractlessStandardResolverAllowPrivate.Instance);
#endif
                                    lock (cache)
                                    {
                                        foreach (var zipmodInfo in list)
                                            cache.Add(zipmodInfo.FileName, zipmodInfo);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogWarning($"Failed to load cache part [{cachePartFile}] with error: {e}");
                            }
                            finally
                            {
                                if (Interlocked.Add(ref finished, 1) >= cachePartFiles.Count)
                                    wait.Set();
                            }
                        });
                    }

                    wait.WaitOne();
                    Logger.LogDebug($"Loaded zipmod cache from \"{_CachePath}\" in {swCacheRead.ElapsedMilliseconds}ms using {cachePartFiles.Count} threads");
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to load cache: " + e);
            }

            return cache;
        }

        #endregion
    }
}
