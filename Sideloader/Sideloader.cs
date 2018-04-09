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
using UnityEngine;

namespace Sideloader
{
    [BepInDependency("com.bepis.bepinex.resourceredirector")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin(GUID: "com.bepis.bepinex.sideloader", Name: "Mod Sideloader", Version: "1.0")]
    public class Sideloader : BaseUnityPlugin
    {
        protected List<ZipFile> archives = new List<ZipFile>();

        protected List<ChaListData> lists = new List<ChaListData>();

        

        public static Dictionary<Manifest, List<ChaListData>> LoadedData = new Dictionary<Manifest, List<ChaListData>>();

        public Sideloader()
        {
            //only required for ILMerge
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name == "I18N, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756" ||
                    args.Name == "I18N.West, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
                    return Assembly.GetExecutingAssembly();
                else
                    return null;
            };

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

                archives.Add(archive);

                LoadAllLists(archive, manifest);
            }

            //add hook
            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);
            AutoResolver.Hooks.InstallHooks();
        }

        private void modifyList(ChaListData data, ZipFile arc, ZipEntry entry)
        {
            foreach (var kv in data.dictList)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    string item = kv.Value[i];

                    if (item.EndsWith(".unity3d"))
                    {
                        string uid = BundleManager.CreateAndAddUID(() => 
                        {
                            var stream = arc.GetInputStream(entry);

                            byte[] buffer = new byte[entry.Size];

                            stream.Read(buffer, 0, (int)entry.Size);

                            return AssetBundle.LoadFromMemory(buffer);
                        });

                        kv.Value[i] = BundleManager.DummyPath;

                        //i++;

                        //kv.Value[i] = BundleManager.CreateUniquePath(uid)
                    }
                }
            }
        }

        protected void LoadAllLists(ZipFile arc, Manifest manifest)
        {
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.StartsWith("abdata/list/characustom") && entry.Name.EndsWith(".csv"))
                {
                    //BepInLogger.Log(entry.Name);

                    var stream = arc.GetInputStream(entry);
                    
                    var chaListData = ListLoader.LoadCSV(stream);
                    ListLoader.ExternalDataList.Add(chaListData);

                    if (!LoadedData.ContainsKey(manifest))
                    {
                        LoadedData[manifest] = new List<ChaListData> { chaListData };
                    }
                    else
                    {
                        LoadedData[manifest].Add(chaListData);
                    }

                    //int length = (int)entry.Size;
                    //byte[] buffer = new byte[length];

                    //stream.Read(buffer, 0, length);

                    //string text = Encoding.UTF8.GetString(buffer);
                }
            }
        }

        protected bool RedirectHook(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            string zipPath = $"{manifestAssetBundleName}/{assetBundleName.Replace(".unity3d", "")}/{assetName}";

            //if (zipPath.Contains("cw_t_hitomi_hi_u_9999"))
            

            if (type == typeof(Texture2D))
            {
                zipPath = $"{zipPath}.png";

                //if (zipPath.Contains("cw_t_hitomi_hi_u_9999"))
                //  BepInLogger.Log(zipPath);

                foreach (var archive in archives)
                {
                    var entry = archive.GetEntry(zipPath);

                    if (entry != null)
                    {
                        //BepInLogger.Log(entry.Name);
                        var stream = archive.GetInputStream(entry);

                        result = new AssetBundleLoadAssetOperationSimulation(ResourceRedirector.AssetLoader.LoadTexture(stream, (int)entry.Size));
                        return true;
                    }
                }
            }
            
            //if (!bundles.ContainsKey(assetBundleName))
            //{
            //    foreach (var archive in archives)
            //    {
            //        var entry = archive.GetEntry(assetBundleName);

            //        if (entry != null)
            //        {
            //            BepInLogger.Log("Found in " + archive.Name);
            //            var stream = archive.GetInputStream(entry);

            //            byte[] buffer = new byte[entry.Size];

            //            stream.Read(buffer, 0, (int)entry.Size);

            //            bundles[assetBundleName] = AssetBundle.LoadFromMemory(buffer);
                        
            //            //result = new AssetBundleLoadAssetOperationSimulation(ResourceRedirector.AssetLoader.LoadTexture(stream, (int)entry.Size));
            //        }
            //    }
            //}

            if (BundleManager.TryGetObjectFromName(assetName, out UnityEngine.Object obj))
            {
                result = new AssetBundleLoadAssetOperationSimulation(obj);
                    return true;
            }

            result = null;
            return false;
        }
    }
}
