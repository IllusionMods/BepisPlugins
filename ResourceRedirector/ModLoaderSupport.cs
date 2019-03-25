using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;

namespace ResourceRedirector
{
    public class ModLoaderSupport
    {
        readonly string configSybarisSection = "sybaris-patches-location";
        readonly string configSybarisKey = "org.bepinex.patchers.sybarisloader";

        string[] ModLoaderABFiles;

        public ModLoaderSupport()
        {
            ModLoaderABFiles = GetModLoaderAssetBundleFiles();
        }

        public string[] GetModLoaderAssetBundleFiles()
        {

            string sybarisPathesDirectoryRelative = BepInEx.Config.GetEntry(configSybarisSection, null, configSybarisKey);

            if (sybarisPathesDirectoryRelative != null)
            {
                string sybarisPathesDirectoryAbsolute = Path.Combine(Paths.GameRootPath, sybarisPathesDirectoryRelative);
                if (!Directory.Exists(sybarisPathesDirectoryAbsolute)) return null;

                string ModLoaderDirectory = Path.Combine(sybarisPathesDirectoryAbsolute, "..\\GameData");
                if (!Directory.Exists(ModLoaderDirectory)) return null;

                SearchOption option = SearchOption.AllDirectories;

                string[] assetBundleFiles = Directory.GetFiles(ModLoaderDirectory, "*.unity3d", option);

                return assetBundleFiles;
            }

            return null;
        }

        public bool AssetBubdleFileExist(string assetBundleName)
        {
            if (ModLoaderABFiles != null && assetBundleName != null)
            {
                string pattern = assetBundleName.Replace("/", "\\");
                pattern = Regex.Escape(pattern);
                pattern += "$";
                for (int i = 0; i < ModLoaderABFiles.Count(); i++)
                {
                    if (Regex.IsMatch(ModLoaderABFiles[i], pattern))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}