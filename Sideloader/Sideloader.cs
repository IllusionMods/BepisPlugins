using BepInEx;
using BepInEx.Common;
using ICSharpCode.SharpZipLib.Zip;
using ResourceRedirector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Shared;
using Sideloader.AutoResolver;
using UnityEngine;

namespace Sideloader
{
    [BepInDependency("com.bepis.bepinex.resourceredirector")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin(GUID: "com.bepis.bepinex.sideloader", Name: "Mod Sideloader", Version: "1.0")]
    public class Sideloader : BaseUnityPlugin
    {
        protected List<ZipFile> Archives = new List<ZipFile>();

        protected List<ChaListData> lists = new List<ChaListData>();

        public static Dictionary<Manifest, List<ChaListData>> LoadedData { get; } = new Dictionary<Manifest, List<ChaListData>>();



        public Sideloader()
        {
            //install hooks
            Hooks.InstallHooks();
            AutoResolver.Hooks.InstallHooks();
            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);

            //check mods directory
            string modDirectory = Path.Combine(Utility.ExecutingDirectory, "mods");

            if (!Directory.Exists(modDirectory))
                return;

            
            //load zips
            foreach (string archivePath in Directory.GetFiles(modDirectory, "*.zip"))
            {
                var archive = new ZipFile(archivePath);

                if (!Manifest.TryLoadFromZip(archive, out Manifest manifest))
                {
                    BepInLogger.Log($"[SIDELOADER] Cannot load {Path.GetFileName(archivePath)} due to missing/invalid manifest.");
                    continue;
                }
                
                string name = manifest.Name ?? Path.GetFileName(archivePath);
                BepInLogger.Log($"[SIDELOADER] Loaded {name} {manifest.Version ?? ""}");

                Archives.Add(archive);

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
                if (entry.Name.StartsWith("abdata/list/characustom") && entry.Name.EndsWith(".csv"))
                {
                    var stream = arc.GetInputStream(entry);
                    
                    var chaListData = ListLoader.LoadCSV(stream);

                    SetPossessNew(chaListData);
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
                if (entry.Name.EndsWith(".unity3d"))
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

                    BundleManager.AddBundleLoader(getBundleFunc, assetBundlePath);
                }
            }
        }

        protected bool RedirectHook(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            string zipPath = $"{manifestAssetBundleName ?? "abdata"}/{assetBundleName.Replace(".unity3d", "")}/{assetName}";

            if (type == typeof(Texture2D))
            {
                zipPath = $"{zipPath}.png";

                foreach (var archive in Archives)
                {
                    var entry = archive.GetEntry(zipPath);

                    if (entry != null)
                    {
                        var stream = archive.GetInputStream(entry);

                        result = new AssetBundleLoadAssetOperationSimulation(ResourceRedirector.AssetLoader.LoadTexture(stream, (int)entry.Size));
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
