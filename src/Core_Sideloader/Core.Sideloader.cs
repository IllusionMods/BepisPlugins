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
using JetBrains.Annotations;
using UnityEngine;
using XUnity.ResourceRedirector;
using ADV.Commands.Base;
using MessagePack;
using UnityEngine.Assertions;
using System.ComponentModel;
using System.Threading;
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
        /// <summary> Dictionary of loaded zip file name and its zipmod metadata </summary>
        private static Dictionary<string, ZipmodInfo> Zipmods = new Dictionary<string, ZipmodInfo>();

        /// <summary> List of all loaded manifest files </summary>
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

        class TestLogListener : ILogListener
        {
            public void Dispose()
            {

            }

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                if (eventArgs.Source.SourceName == "Sideloader" && eventArgs.Level == LogLevel.Error)
                    Console.WriteLine(new StackTrace());
            }
        }

        internal void Awake()
        {
            BepInEx.Logging.Logger.Listeners.Add(new TestLogListener());

#if KK // Fixes an issue with reading some zips made on Japanese systems. Only needed on .Net 3.5, it doesn't affect newer Unity versions.
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

            RandomizeSlotIds = Config.Bind("General", "Randomize Slot IDs", true,
                new ConfigDescription("Helps detect bugs in Sideloader and other plugins by making them more obvious. If false, the bugs might not affect current game instance but will suddenly pop up after updating the game or sending scenes/cards to another users.", null, "Advanced"));
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

        private static string GetRelativeArchiveDir(string archiveDir) => !archiveDir.StartsWith(ModsDirectory, StringComparison.OrdinalIgnoreCase) ? archiveDir : archiveDir.Substring(ModsDirectory.Length).Trim(' ', '/', '\\');

        Stopwatch sw1 = new Stopwatch();
        Stopwatch sw2 = new Stopwatch();
        Stopwatch sw3 = new Stopwatch();
        Stopwatch sw4 = new Stopwatch();
        private void LoadModsFromDirectories(params string[] modDirectories)
        {
            var stopWatch = Stopwatch.StartNew();

            sw1.Start();
            //1038

            var foundZipfiles = new List<ZipmodInfo>();
            var waitHandle = new ManualResetEvent(false);
            // Do as much as possible in background while the cache is being loaded (makes this essentially free)
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    foreach (var modDirectory in modDirectories.Where(Directory.Exists))
                    {
                        var prevCount = foundZipfiles.Count;
                        foreach (var zipFile in Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories))
                        {
                            if (!zipFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && !zipFile.EndsWith(".zipmod", StringComparison.OrdinalIgnoreCase))
                                continue;

                            foundZipfiles.Add(new ZipmodInfo(zipFile));
                        }

                        Logger.LogInfo("Found " + (foundZipfiles.Count - prevCount) + " zip/zipmod files in directory: " + modDirectory);
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

            var cachePath = @"e:\test.txt"; //Path.Combine(BepInEx.Paths.CachePath, "sideloader_zipmodcache.bin");
            var cache = new Dictionary<string, ZipmodInfo>();
            try
            {
                if (File.Exists(cachePath))
                {
                    // Takes around 1s
                    using (var fileStream = File.OpenRead(cachePath))
                    {
                        // todo check sideloader version and invalidate
                        cache = LZ4MessagePackSerializer.Deserialize<Dictionary<string, ZipmodInfo>>(fileStream);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to load cache: " + e);
            }

            waitHandle.WaitOne();


            //var groupedZipmods = new Dictionary<string, List<ZipmodInfo>>();

            // Very fast
            foreach (var info in foundZipfiles)
            {
                var zipFileName = info.FileName;
                cache.TryGetValue(zipFileName, out var cachedInfo);
                if (cachedInfo != null && info.FileSize == cachedInfo.FileSize && info.LastWriteTime == cachedInfo.LastWriteTime)
                {
                    if (zipFileName != cachedInfo.FileName) Console.WriteLine($"WTF {zipFileName}   /   {cachedInfo.FileName}");
                    Zipmods.Add(zipFileName, cachedInfo);

                    if (cachedInfo.Error != null)
                    {
                        if (cachedInfo.Error.StartsWith("Skipping"))
                            Logger.LogWarning(cachedInfo.Error);
                        else
                            Logger.LogError(cachedInfo.Error);
                    }

                    continue;
                }

                if (cache.Count > 0)
                    Console.WriteLine("Cache MISS for " + zipFileName);

                Zipmods.Add(info.FileName, info);

                try
                {
                    var archive = info.GetZipFile();

                    info.Manifest = Manifest.LoadFromZip(archive);
                    //Skip the mod if it is not for this game
                    bool allowed = info.Manifest.Games.Count == 0 || info.Manifest.Games.Select(x => x.ToLower()).Any(GameNameList.Contains);
                    if (!allowed)
                    {
                        var msg = $"Skipping archive \"{info.RelativeFileName}\" because it's meant for {string.Join(", ", info.Manifest.Games.ToArray())}";
                        Logger.LogInfo(msg);
                        info.Error = msg;
                        continue;
                    }

                    info.BundleInfos.AddRange(FindAllBundlesInArchive(archive));

                    LoadAllLists(info);

                    info.pngNames.AddRange(GetPngAssetFilenames(archive));
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to load archive \"{info.RelativeFileName}\" with error: {ex}";
                    Logger.LogError(msg);
                    info.Error = msg;
                    info.Dispose();
                }
            }

            waitHandle.Reset();
            // Can serialize in background since Zipmods should not get modified
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    File.WriteAllBytes(cachePath, LZ4MessagePackSerializer.Serialize(Zipmods));
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Failed to save cache: " + e);
                }
                finally
                {
                    waitHandle.Set();
                }
            });

            var enableModLoadingLogging = DebugLoggingModLoading.Value;
            var modLoadInfoSb = enableModLoadingLogging ? new StringBuilder(1000) : null;

            var gatheredResolutionInfos = new List<ResolveInfo>();
            var gatheredMigrationInfos = new List<MigrationInfo>();
#if AI || HS2
            var gatheredHeadPresetInfos = new List<HeadPresetInfo>();
            var gatheredFaceSkinInfos = new List<FaceSkinInfo>();
#endif


            //586

            // Handle duplicate GUIDs and load unique mods
            //0ms
            var valids = Zipmods.Values.Where(x => x.Valid).ToList();
            sw1.Stop();
            sw2.Start();
            //520ms
            var groupBy = valids.GroupBy(x => x.Manifest.GUID).ToList();
            sw2.Stop();
            sw3.Start();
            //63ms
            var modGroups = groupBy.OrderBy(x => x.Key).ToList();

            sw3.Stop();
            sw4.Start();
            //224

            foreach (var modGroup in modGroups)
            {
                // Order by version if available, else use modified dates (less reliable)
                // If versions match, prefer mods inside folders or with more descriptive names so modpacks are preferred
                var orderedModsQuery = modGroup.All(x => !string.IsNullOrEmpty(x.Manifest.Version))
                    ? modGroup.OrderByDescending(x => x.Manifest.Version, new ManifestVersionComparer()).ThenByDescending(x => x.FileName.Length)
                    : modGroup.OrderByDescending(x => x.LastWriteTime);

                var orderedMods = orderedModsQuery.ToList();
                var zipmod = orderedMods[0];

                if (orderedMods.Count > 1)
                {
                    var modList = string.Join(", ", orderedMods.Skip(1).Select(x => '"' + zipmod.RelativeFileName + '"').ToArray());
                    Logger.LogWarning($"Multiple versions detected, only \"{zipmod.RelativeFileName}\" will be loaded. Skipped versions: {modList}");

                    // Don't keep the duplicate archives in memory
                    foreach (var dupeMod in orderedMods.Skip(1))
                        dupeMod.Dispose();
                }
                // Actually load the mods (only one per GUID, the newest one)
                try
                {
                    var manifest = zipmod.Manifest;
                    ZipArchives[manifest.GUID] = zipmod.FileName;
                    Manifests[manifest.GUID] = manifest;

                    AddBundles(zipmod.BundleInfos);
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

            sw4.Stop();
            //31

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

            // Should finish by now, but wait just to be safe
            waitHandle.WaitOne();

            stopWatch.Stop();

            if (enableModLoadingLogging)
                Logger.LogInfo($"List of loaded mods:\n{modLoadInfoSb}");
            Logger.LogInfo($"Successfully loaded {Zipmods.Count(x => x.Value.Loaded)} mods out of {Zipmods.Count} archives in {stopWatch.ElapsedMilliseconds}ms");

            var failedPaths = Zipmods.Values.Where(x => !x.Loaded).Select(x => x.RelativeFileName).ToArray();
            if (failedPaths.Length > 0)
                Logger.LogWarning("Could not load " + failedPaths.Length + " mods, see previous warnings for more information. File names of skipped archives:\n" + string.Join(" | ", failedPaths));




            Console.WriteLine($"sw1 {sw1.ElapsedMilliseconds}  sw2 {sw2.ElapsedMilliseconds}  sw3 {sw3.ElapsedMilliseconds}  sw4 {sw4.ElapsedMilliseconds}");

        }

        [MessagePackObject(true)]
        internal class ZipmodInfo : IDisposable
        {
            public Manifest Manifest;
            public string FileName;
            public string RelativeFileName;
            public DateTime LastWriteTime;
            public long FileSize;
            public string Error;

            //todo generate cache id from file size and last change date

            [IgnoreMember]
            private ZipFile _zipFile;

            public List<ChaListData> charaLists = new List<ChaListData>();
            public List<BundleLoadInfo> BundleInfos = new List<BundleLoadInfo>();
            public List<Lists.StudioListData> boneLists = new List<Lists.StudioListData>();
            public List<Lists.StudioListData> studioLists = new List<Lists.StudioListData>();
            public List<Lists.StudioListData> mapLists = new List<Lists.StudioListData>();
            public List<string> pngNames = new List<string>();

            [IgnoreMember]
            public bool Loaded;

            public ZipmodInfo() { }
            public ZipmodInfo(string fileName)
            {
                FileName = fileName;
                RelativeFileName = GetRelativeArchiveDir(fileName);

                var fi = new FileInfo(fileName);
                LastWriteTime = fi.LastWriteTimeUtc;
                FileSize = fi.Length;
            }

            public ZipFile GetZipFile()
            {
                if (_zipFile == null)
                {
                    _zipFile = new ZipFile(FileName);
                }
                else if (_zipFile.Count == 0)
                {
                    // Disposing makes entry count = 0, but it should be at least 1 for the manifest.
                    // Still try to dispose the old zipfile just to be safe.
                    _zipFile.Close();
                    _zipFile = new ZipFile(FileName);
                }

                return _zipFile;
            }

            // todo use to be able to cache non-zipmod files?
            [IgnoreMember]
            public bool Valid => Manifest != null && Error == null;

            //public long GetCacheHash()
            //{
            //    return LastWriteTime.Ticks ^ ~FileSize;
            //}
            //static long Reverse(long x)
            //{
            //    long y = 0;
            //    for (int i = 0; i < 32; ++i)
            //    {
            //        y <<= 1;
            //        y |= (x & 1);
            //        x >>= 1;
            //    }
            //    return y;
            //}

            public void Dispose()
            {
                if (_zipFile != null)
                {
                    _zipFile.Close();
                    _zipFile = null;
                }
            }
        }

        private static void LoadAllLists(ZipmodInfo zipmod)
        {
            var arc = zipmod.GetZipFile();
            var manifest = zipmod.Manifest;
            foreach (ZipEntry entry in arc)
            {
                try
                {
                    if (entry.Name.StartsWith("abdata/list/characustom", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var stream = arc.GetInputStream(entry);
                        var chaListData = Lists.LoadCSV(stream);

                        SetPossessNew(chaListData);

                        zipmod.charaLists.Add(chaListData);
                    }
#if KK || AI || HS2 || KKS
                    else if (entry.Name.StartsWith("abdata/studio/info", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.GetFileNameWithoutExtension(entry.Name).ToLower().StartsWith("itembonelist_"))
                        {
                            var stream = arc.GetInputStream(entry);
                            var studioListData = Lists.LoadStudioCSV(stream, entry.Name, manifest.GUID);

                            zipmod.boneLists.Add(studioListData);
                        }
                        else
                        {
                            var stream = arc.GetInputStream(entry);
                            var studioListData = Lists.LoadStudioCSV(stream, entry.Name, manifest.GUID);

                            zipmod.studioLists.Add(studioListData);
                        }
                    }
#endif
#if AI || HS2
                    else if (entry.Name.StartsWith("abdata/list/map/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var stream = arc.GetInputStream(entry);
                        var data = Lists.LoadExcelDataCSV(stream, entry.Name);
                        zipmod.mapLists.Add(data);
                    }
#endif
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to load list file \"{entry.Name}\" from archive \"{GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                }
            }
        }

        private static void AddAllLists(ZipmodInfo zipmod, List<ResolveInfo> _gatheredResolutionInfos)
        {
            var manifest = zipmod.Manifest;
            foreach (var chaListData in zipmod.charaLists)
            {
                UniversalAutoResolver.GenerateResolutionInfo(manifest, chaListData, _gatheredResolutionInfos);
                Lists.ExternalDataList.Add(chaListData);
            }
            foreach (var studioListData in zipmod.studioLists)
            {
                UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                Lists.ExternalStudioDataList.Add(studioListData);
            }
            foreach (var mapListData in zipmod.mapLists)
            {
                Lists.AddExcelDataCSV(mapListData);
            }
            foreach (var boneListData in zipmod.boneLists)
            {
                UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, boneListData);
                Lists.ExternalStudioDataList.Add(boneListData);
            }
        }

        private static void SetPossessNew(ChaListData data)
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
        /// Get filenames of all .png files in this archive that are inside an abdata folder.
        /// </summary>
        [Pure]
        private static IEnumerable<string> GetPngAssetFilenames(ZipFile arc)
        {
            foreach (ZipEntry entry in arc)
            {
                //Only list folders for .pngs in abdata folder
                //i.e. skip preview pics or character cards that might be included with the mod
                if (entry.Name.StartsWith("abdata/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    yield return entry.Name;
                }
            }
        }
        /// <summary>
        /// Construct a list of all folders that contain a .png
        /// </summary>
        private static void BuildPngFolderList(ZipmodInfo zipmod)
        {
            foreach (var pngAssetFilename in zipmod.pngNames)
            {
                //Make a list of all the .png files and archive they come from
                if (PngList.ContainsKey(pngAssetFilename))
                {
                    if (DebugLoggingModLoading.Value)
                        Logger.LogWarning($"Duplicate .png asset detected! {pngAssetFilename} in \"{zipmod.RelativeFileName}\"");
                }
                else
                    PngList.Add(pngAssetFilename, zipmod);

                // todo generate this at the very end after all archive png files are added so theres no duplicates
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

        [MessagePackObject(true)]
        public class BundleLoadInfo
        {
            public BundleLoadInfo(string archiveFilename, long streamOffset, string bundleFullPath, string bundleTrimmedPath)
            {
                ArchiveFilename = archiveFilename;
                StreamOffset = streamOffset;
                BundleFullPath = bundleFullPath;
                BundleTrimmedPath = bundleTrimmedPath;
            }
            public string ArchiveFilename;
            [IgnoreMember]
            public bool CanBeStreamed => StreamOffset >= 0;

            public long StreamOffset = -1;

            public string BundleFullPath;
            public string BundleTrimmedPath;

            public AssetBundle LoadBundle()
            {
                AssetBundle bundle;

                if (CanBeStreamed)
                {
                    if (DebugLogging.Value)
                        Logger.LogDebug($"Streaming \"{BundleFullPath}\" ({GetRelativeArchiveDir(ArchiveFilename)}) unity3d file from disk, offset {StreamOffset}");

                    bundle = AssetBundle.LoadFromFile(ArchiveFilename, 0, (ulong)StreamOffset);
                }
                else
                {
                    Logger.LogDebug($"Cannot stream \"{BundleFullPath}\" ({GetRelativeArchiveDir(ArchiveFilename)}) unity3d file from disk, loading to RAM instead");

                    var arc = Zipmods[ArchiveFilename].GetZipFile();
                    var entry = arc.GetEntry(BundleFullPath);
                    var stream = arc.GetInputStream(entry);

                    byte[] buffer = new byte[entry.Size];

                    stream.Read(buffer, 0, (int)entry.Size);

                    // The line below can either be commented in or out - it doesn't really matter. 
                    //  - If in: It will generate successive unique CAB-strings for these asset bundles
                    //  - If out: The CAB of the actual asset bundle will be used if possible, otherwise a random CAB is generated by the Resource Redirector due to the call to 'ResourceRedirection.EnableRandomizeCabIfConflict(-2000, false)'
                    //BundleManager.RandomizeCAB(buffer);

                    bundle = AssetBundleHelper.LoadFromMemory($"\"{BundleFullPath}\" ({GetRelativeArchiveDir(ArchiveFilename)})", buffer, 0);
                }

                if (bundle == null)
                {
                    Logger.LogError($"Asset bundle \"{BundleFullPath}\" ({GetRelativeArchiveDir(ArchiveFilename)}) failed to load. It might have a conflicting CAB string.");
                }

                return bundle;
            }
        }

        [Pure]
        private static IEnumerable<BundleLoadInfo> FindAllBundlesInArchive(ZipFile arc)
        {
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
                {
                    string assetBundlePath = entry.Name;

                    if (assetBundlePath.Contains('/'))
                        assetBundlePath = assetBundlePath.Remove(0, assetBundlePath.IndexOf('/') + 1);

                    if (entry.CompressionMethod == CompressionMethod.Stored)
                    {
                        long index = (long)locateZipEntryMethodInfo.Invoke(arc, new object[] { entry });
                        yield return new BundleLoadInfo(arc.Name, index, entry.Name, assetBundlePath);
                    }
                    else
                    {
                        yield return new BundleLoadInfo(arc.Name, -1, entry.Name, assetBundlePath);
                    }
                }
            }
        }
        private static void AddBundles(IEnumerable<BundleLoadInfo> bundleInfos)
        {
            foreach (var bundleLoadInfo in bundleInfos)
            {
                BundleManager.AddBundleLoader(bundleLoadInfo.LoadBundle, bundleLoadInfo.BundleTrimmedPath, out string warning);

                if (!string.IsNullOrEmpty(warning) && DebugLoggingModLoading.Value)
                    Logger.LogWarning($"{warning} in \"{GetRelativeArchiveDir(bundleLoadInfo.ArchiveFilename)}\"");
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
    }
}
