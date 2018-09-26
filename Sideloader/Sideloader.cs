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

                foreach (var archive in Archives)
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
    }
}
