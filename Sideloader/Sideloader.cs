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
    [BepInPlugin(GUID: "com.bepis.bepinex.sideloader", Name: "Mod Sideloader", Version: "1.1")]
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
            string modDirectory = Path.Combine(Paths.GameRootPath, "mods");

            if (!Directory.Exists(modDirectory))
                return;

            
            //load zips
            foreach (string archivePath in Directory.GetFiles(modDirectory, "*.zip", SearchOption.AllDirectories))
            {
                var archive = new ZipFile(archivePath);

                if (!Manifest.TryLoadFromZip(archive, out Manifest manifest))
                {
                    Logger.Log(LogLevel.Warning, $"[SIDELOADER] Cannot load {Path.GetFileName(archivePath)} due to missing/invalid manifest.");
                    continue;
                }

                if (LoadedManifests.Any(x => x.GUID == manifest.GUID))
                {
                    Logger.Log(LogLevel.Warning, $"[SIDELOADER] Skipping {Path.GetFileName(archivePath)} due to duplicate GUID \"{manifest.GUID}\".");
                    continue;
                }

                string name = !string.IsNullOrEmpty(manifest.Name?.Trim())
                    ? manifest.Name
                    : Path.GetFileName(archivePath);

                Logger.Log(LogLevel.Info, $"[SIDELOADER] Loaded {name} {manifest.Version ?? ""}");

                Archives.Add(archive);
                LoadedManifests.Add(manifest);

                LoadAllUnityArchives(archive);

                LoadAllLists(archive, manifest);
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
                LoadedData.Add(manifest, new List<ChaListData>(new [] { data } ));
            }
        }

        protected void LoadAllLists(ZipFile arc, Manifest manifest)
        {
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.StartsWith("abdata/list/characustom", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
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
            }
        }

        protected void LoadAllUnityArchives(ZipFile arc)
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
                        var stream = arc.GetInputStream(entry);

                        byte[] buffer = new byte[entry.Size];

                        stream.Read(buffer, 0, (int)entry.Size);

                        BundleManager.RandomizeCAB(buffer);

                        return AssetBundle.LoadFromMemory(buffer);
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

                        var tex = ResourceRedirector.AssetLoader.LoadTexture(stream, (int) entry.Size);
                        
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
