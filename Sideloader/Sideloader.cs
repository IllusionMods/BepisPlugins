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
    public class Sideloader : BaseUnityPlugin
    {
        public override string ID => "com.bepis.bepinex.sideloader";
        public override string Name => "Mod Sideloader";
        public override Version Version => new Version("1.0");

        protected List<ZipFile> archives = new List<ZipFile>();

        protected List<ChaListData> lists = new List<ChaListData>();

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
                
                archives.Add(archive);

                LoadAllLists(archive);
            }

            //add hook
            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);
        }

        protected void LoadAllLists(ZipFile arc)
        {
            foreach (ZipEntry entry in arc)
            {
                if (entry.Name.StartsWith("list/characustom") && entry.Name.EndsWith(".csv"))
                {
                    var stream = arc.GetInputStream(entry);

                    ListLoader.LoadCSV(stream);
                }
            }
        }

        protected bool RedirectHook(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            string zipPath = $"{assetBundleName.Replace(".unity3d", "")}/{assetName}";


            if (type == typeof(Texture2D))
            {
                zipPath = $"{zipPath}.png";

                foreach (var archive in archives)
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

            result = null;
            return false;
        }
    }
}
