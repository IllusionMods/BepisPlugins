using BepInEx;
using ICSharpCode.SharpZipLib.Zip;
using ResourceRedirector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Shared;
using Sideloader.AutoResolver;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace Sideloader
{
    [BepInDependency("com.bepis.bepinex.resourceredirector")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin(GUID: "com.bepis.bepinex.sideloader", Name: "Mod Sideloader", Version: "1.3")]
    public class Sideloader : BaseUnityPlugin
    {
        protected List<ZipFile> Archives = new List<ZipFile>();

        protected List<ChaListData> lists = new List<ChaListData>();

        protected List<Manifest> LoadedManifests = new List<Manifest>();

        protected static Dictionary<string, ZipFile> PngList = new Dictionary<string, ZipFile>();
        protected static List<string> PngFolderList = new List<string>();
        protected static List<string> PngFolderOnlyList = new List<string>();


        public static Dictionary<Manifest, List<ChaListData>> LoadedData { get; } = new Dictionary<Manifest, List<ChaListData>>();

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
                if (archiveDir.Length < modDirectory.Length)
                    return archiveDir;
                return archiveDir.Substring(modDirectory.Length).Trim(' ', '/', '\\');
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
                var orderedModsQuery = modGroup.All(x => !string.IsNullOrEmpty(x.Value.Version))
                    ? modGroup.OrderByDescending(x => x.Value.Version, new ManifestVersionComparer())
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

        protected void IndexList(Manifest manifest, ChaListData data)
        {
            if (LoadedData.TryGetValue(manifest, out lists))
            {
                lists.Add(data);
            }
            else
            {
                LoadedData.Add(manifest, new List<ChaListData>(new[] { data }));
            }
        }

        protected void LoadAllLists(ZipFile arc, Manifest manifest)
        {
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
                        IndexList(manifest, chaListData);

                        ListLoader.ExternalDataList.Add(chaListData);

                        if (LoadedData.TryGetValue(manifest, out lists))
                        {
                            lists.Add(chaListData);
                        }
                        else
                        {
                            LoadedData[manifest] = new List<ChaListData> { chaListData };
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"[SIDELOADER] Failed to load list file \"{entry.Name}\" from archive \"{arc.Name}\" with error: {ex.Message}");
                        Logger.Log(LogLevel.Error, $"[SIDELOADER] Error details: {ex}");
                    }
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
                    //Make a list of all the .png files and archive they come from
                    PngList.Add(entry.Name, arc);

                    string assetBundlePath = entry.Name;

                    //Remove the .png filename and "abdata/"
                    assetBundlePath = assetBundlePath.Remove(assetBundlePath.LastIndexOf('/')).Remove(0, assetBundlePath.IndexOf('/') + 1);
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
            if (PngFolderOnlyList.Contains(assetBundleName.Remove(assetBundleName.LastIndexOf('.'))))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Check whether the .png file comes from a sideloader mod
        /// </summary>
        public static bool IsPng(string pngFile)
        {
            if (PngList.ContainsKey(pngFile))
                return true;
            else
                return false;
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

                    Func<AssetBundle> getBundleFunc = () =>
                    {
                        AssetBundle bundle;

                        if (entry.CompressionMethod == CompressionMethod.Stored)
                        {
                            long index = (long)locateZipEntryMethodInfo.Invoke(arc, new object[] { entry });

                            Logger.Log(LogLevel.Info, $"Streaming {entry.Name} ({archiveFilename}) unity3d file from disk, offset {index}");
                            bundle = AssetBundle.LoadFromFile(archiveFilename, 0, (ulong)index);
                        }
                        else
                        {
                            var stream = arc.GetInputStream(entry);

                            byte[] buffer = new byte[entry.Size];

                            stream.Read(buffer, 0, (int)entry.Size);

                            BundleManager.RandomizeCAB(buffer);

                            bundle = AssetBundle.LoadFromMemory(buffer);
                        }

                        if (bundle == null)
                        {
                            Logger.Log(LogLevel.Error, $"Asset bundle \"{entry.Name}\" ({Path.GetFileName(archiveFilename)}) failed to load! Does it have a conflicting CAB string?");
                        }

                        return bundle;
                    };

                    BundleManager.AddBundleLoader(getBundleFunc, assetBundlePath, out string warning);

                    if (!string.IsNullOrEmpty(warning))
                        Logger.Log(LogLevel.Warning, $"[SIDELOADER] WARNING! {warning}");
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
                else
                {
                    if (IsPngFolderOnly(assetBundleName))
                    {
                        //A .png that does not exist is being requested from from an asset bundle that does not exist
                        //Return an empty image to prevent crashing
                        result = new AssetBundleLoadAssetOperationSimulation(new Texture2D(0, 0));
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
    }
}
