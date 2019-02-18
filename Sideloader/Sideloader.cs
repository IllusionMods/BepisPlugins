using BepInEx;
using BepInEx.Logging;
using ICSharpCode.SharpZipLib.Zip;
using ResourceRedirector;
using Shared;
using Sideloader.AutoResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace Sideloader
{
    [BepInDependency(ResourceRedirector.ResourceRedirector.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID: GUID, Name: "Mod Sideloader", Version: Version)]
    public class Sideloader : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.sideloader";
        public const string Version = BepisPlugins.Metadata.PluginsVersion;

        protected List<ZipFile> Archives = new List<ZipFile>();

        protected List<Manifest> LoadedManifests = new List<Manifest>();

        protected static Dictionary<string, ZipFile> PngList = new Dictionary<string, ZipFile>();
        protected static HashSet<string> PngFolderList = new HashSet<string>();
        protected static HashSet<string> PngFolderOnlyList = new HashSet<string>();

        /// <summary>
        /// Check if a mod with specified GUID has been loaded
        /// </summary>
        public bool IsModLoaded(string guid)
        {
            if (guid == null) return false;
            return LoadedManifests.Any(x => x.GUID == guid);
        }

        public Sideloader()
        {
            //ilmerge
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name == "I18N, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756"
                 || args.Name == "I18N.West, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
                    return Assembly.GetExecutingAssembly();

                return null;
            };

            //install hooks
            Hooks.InstallHooks();
            AutoResolver.Hooks.InstallHooks();
            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);
            ResourceRedirector.ResourceRedirector.AssetBundleResolvers.Add(AssetBundleRedirectHook);

            //check mods directory
            var modDirectory = Path.Combine(Paths.GameRootPath, "mods");

            if (!Directory.Exists(modDirectory))
            {
                Logger.Log(LogLevel.Warning, "[SIDELOADER] Could not find the \"mods\" directory");
                return;
            }

            LoadModsFromDirectory(modDirectory);
        }

        private void LoadModsFromDirectory(string modDirectory)
        {
            string GetRelativeArchiveDir(string archiveDir)
            {
                return archiveDir.Length < modDirectory.Length ? archiveDir : archiveDir.Substring(modDirectory.Length).Trim(' ', '/', '\\');
            }

            Logger.Log(LogLevel.Info, "[SIDELOADER] Scanning the \"mods\" directory...");

            // Look for mods, load their manifests
            var allMods = Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories)
                .Where(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                            x.EndsWith(".zipmod", StringComparison.OrdinalIgnoreCase));

            var archives = new Dictionary<ZipFile, Manifest>();

            foreach (var archivePath in allMods)
            {
                ZipFile archive = null;
                try
                {
                    archive = new ZipFile(archivePath);

                    if (Manifest.TryLoadFromZip(archive, out Manifest manifest))
                        archives.Add(archive, manifest);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load archive \"{GetRelativeArchiveDir(archivePath)}\" with error: {ex.Message}");
                    Logger.Log(LogLevel.Debug, $"[SIDELOADER] Error details: {ex}");
                    archive?.Close();
                }
            }

            // Handlie duplicate GUIDs and load unique mods
            foreach (var modGroup in archives.GroupBy(x => x.Value.GUID))
            {
                // Order by version if available, else use modified dates (less reliable)
                // If versions match, prefer mods inside folders or with more descriptive names so modpacks are preferred
                var orderedModsQuery = modGroup.All(x => !string.IsNullOrEmpty(x.Value.Version))
                    ? modGroup.OrderByDescending(x => x.Value.Version, new ManifestVersionComparer()).ThenByDescending(x => x.Key.Name.Length)
                    : modGroup.OrderByDescending(x => File.GetLastWriteTime(x.Key.Name));

                var orderedMods = orderedModsQuery.ToList();

                if (orderedMods.Count > 1)
                {
                    var modList = string.Join(", ", orderedMods.Select(x => '"' + GetRelativeArchiveDir(x.Key.Name) + '"').ToArray());
                    Logger.Log(LogLevel.Warning, $"[SIDELOADER] Archives with identical GUIDs detected! Archives: {modList}");
                    Logger.Log(LogLevel.Warning, $"[SIDELOADER] Only \"{GetRelativeArchiveDir(orderedMods[0].Key.Name)}\" will be loaded because it's the newest");

                    // Don't keep the duplicate archives in memory
                    foreach (var dupeMod in orderedMods.Skip(1))
                        dupeMod.Key.Close();
                }

                // Actually load the mods (only one per GUID, the newest one)
                var archive = orderedMods[0].Key;
                var manifest = orderedMods[0].Value;
                try
                {
                    Archives.Add(archive);
                    LoadedManifests.Add(manifest);

                    LoadAllUnityArchives(archive, archive.Name);
                    LoadAllLists(archive, manifest);
                    BuildPngFolderList(archive);

                    var trimmedName = manifest.Name?.Trim();
                    var displayName = !string.IsNullOrEmpty(trimmedName) ? trimmedName : Path.GetFileName(archive.Name);

                    Logger.Log(LogLevel.Info, $"[SIDELOADER] Loaded {displayName} {manifest.Version ?? ""}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load archive \"{GetRelativeArchiveDir(archive.Name)}\" with error: {ex.Message}");
                    Logger.Log(LogLevel.Debug, $"[SIDELOADER] Error details: {ex}");
                }
            }
            BuildPngOnlyFolderList();
        }

        protected void SetPossessNew(ChaListData data)
        {
            for (int i = 0; i < data.lstKey.Count; i++)
            {
                if (data.lstKey[i] == "Possess")
                {
                    foreach (var kv in data.dictList)
                    {
                        kv.Value[i] = "1";
                    }
                    break;
                }
            }
        }

        protected void LoadAllLists(ZipFile arc, Manifest manifest)
        {
            List<ZipEntry> BoneList = new List<ZipEntry>();

            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.StartsWith("abdata/list/characustom", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var stream = arc.GetInputStream(entry);
                        var chaListData = ListLoader.LoadCSV(stream);

                        SetPossessNew(chaListData);
                        UniversalAutoResolver.GenerateResolutionInfo(manifest, chaListData);
                        ListLoader.ExternalDataList.Add(chaListData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load list file \"{entry.Name}\" from archive \"{arc.Name}\" with error: {ex.Message}");
                        Logger.Log(LogLevel.Error, $"[SIDELOADER] Error details: {ex}");
                    }
                }
                if (entry.Name.StartsWith("abdata/studio/info", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    if (Path.GetFileNameWithoutExtension(entry.Name).ToLower().StartsWith("itembonelist_"))
                        BoneList.Add(entry);
                    else
                    {
                        try
                        {
                            var stream = arc.GetInputStream(entry);
                            var studioListData = ListLoader.LoadStudioCSV(stream, entry.Name);

                            UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                            ListLoader.ExternalStudioDataList.Add(studioListData);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load list file \"{entry.Name}\" from archive \"{arc.Name}\" with error: {ex.Message}");
                            Logger.Log(LogLevel.Error, $"[SIDELOADER] Error details: {ex}");
                        }
                    }
                }
            }

            //ItemBoneList data must be resolved after the corresponding item so they can be resolved to the same ID
            foreach (ZipEntry entry in BoneList)
            {
                try
                {
                    var stream = arc.GetInputStream(entry);
                    var studioListData = ListLoader.LoadStudioCSV(stream, entry.Name);

                    UniversalAutoResolver.GenerateStudioResolutionInfo(manifest, studioListData);
                    ListLoader.ExternalStudioDataList.Add(studioListData);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load list file \"{entry.Name}\" from archive \"{arc.Name}\" with error: {ex.Message}");
                    Logger.Log(LogLevel.Error, $"[SIDELOADER] Error details: {ex}");
                }
            }
        }
        /// <summary>
        /// Construct a list of all folders that contain a .png
        /// </summary>
        protected void BuildPngFolderList(ZipFile arc)
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
                        Logger.Log(LogLevel.Warning, $"[SIDELOADER] Duplicate asset detected! {assetBundlePath}");
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
        protected void BuildPngOnlyFolderList()
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
        /// Check whether the .png file comes from a sideloader mod
        /// </summary>
        public static bool IsPng(string pngFile)
        {
            return PngList.ContainsKey(pngFile);
        }

        private static MethodInfo locateZipEntryMethodInfo = typeof(ZipFile).GetMethod("LocateEntry", BindingFlags.NonPublic | BindingFlags.Instance);

        protected void LoadAllUnityArchives(ZipFile arc, string archiveFilename)
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

                            Logger.Log(LogLevel.Debug, $"[SIDELOADER] Streaming {entry.Name} ({archiveFilename}) unity3d file from disk, offset {index}");
                            bundle = AssetBundle.LoadFromFile(archiveFilename, 0, (ulong)index);
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, $"[SIDELOADER] Cannot stream {entry.Name} ({archiveFilename}) unity3d file from disk, loading to RAM instead");
                            var stream = arc.GetInputStream(entry);

                            byte[] buffer = new byte[entry.Size];

                            stream.Read(buffer, 0, (int)entry.Size);

                            BundleManager.RandomizeCAB(buffer);

                            bundle = AssetBundle.LoadFromMemory(buffer);
                        }

                        if (bundle == null)
                        {
                            Logger.Log(LogLevel.Error, $"[SIDELOADER] Asset bundle \"{entry.Name}\" ({Path.GetFileName(archiveFilename)}) failed to load. It might have a conflicting CAB string.");
                        }

                        return bundle;
                    }

                    BundleManager.AddBundleLoader(getBundleFunc, assetBundlePath, out string warning);

                    if (!string.IsNullOrEmpty(warning))
                        Logger.Log(LogLevel.Warning, $"[SIDELOADER] {warning}");
                }
            }
        }

        protected bool RedirectHook(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            string zipPath = $"{manifestAssetBundleName ?? "abdata"}/{assetBundleName.Replace(".unity3d", "", StringComparison.OrdinalIgnoreCase)}/{assetName}";

            if (type == typeof(Texture2D))
            {
                zipPath = $"{zipPath}.png";

                //Only search the archives for a .png that can actually be found
                if (PngList.TryGetValue(zipPath, out ZipFile archive))
                {
                    var entry = archive.GetEntry(zipPath);

                    if (entry != null)
                    {
                        var stream = archive.GetInputStream(entry);

                        var tex = ResourceRedirector.AssetLoader.LoadTexture(stream, (int)entry.Size);

                        if (zipPath.Contains("clamp"))
                            tex.wrapMode = TextureWrapMode.Clamp;
                        else if (zipPath.Contains("repeat"))
                            tex.wrapMode = TextureWrapMode.Repeat;

                        result = new AssetBundleLoadAssetOperationSimulation(tex);
                        return true;
                    }
                }
            }

            if (BundleManager.TryGetObjectFromName(assetName, assetBundleName, type, out UnityEngine.Object obj))
            {
                result = new AssetBundleLoadAssetOperationSimulation(obj);
                return true;
            }

            result = null;
            return false;
        }

        protected bool AssetBundleRedirectHook(string assetBundleName, out AssetBundle result)
        {
            //The only asset bundles that need to be loaded are studio maps
            //Loading asset bundles unnecessarily can interfere with normal sideloader asset handling so avoid it whenever possible
            if (Hooks.MapLoading)
            {
                string bundle = assetBundleName.Remove(0, assetBundleName.IndexOf("/abdata/")).Replace("/abdata/", "");
                if (Hooks.MapABName == bundle)
                {
                    //Only load asset bundles that do not exist on disk to avoid loading partial files
                    if (!File.Exists(assetBundleName))
                    {
                        if (BundleManager.Bundles.TryGetValue(bundle, out List<Lazy<AssetBundle>> lazyList))
                        {
                            Hooks.MapLoading = false;
                            Hooks.MapABName = "";

                            //If more than one exist, only the first will be loaded.
                            result = lazyList[0].Instance;
                            return true;
                        }
                    }
                }
            }
            Hooks.MapLoading = false;
            Hooks.MapABName = "";

            result = null;
            return false;
        }
    }
}
