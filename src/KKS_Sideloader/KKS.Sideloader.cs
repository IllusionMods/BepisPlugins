using System;
using System.IO;
using BepInEx;
using BepisPlugins;
using Microsoft.Win32;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "koikatsu sunshine", "koikatu sunshine", "コイカツ sunshine" };
        
        private static string FindKoiZipmodDir()
        {
            try
            {
                // Don't look for the KK modpack if a copy of it is already installed in KKS
                if (Directory.Exists(Path.Combine(Paths.GameRootPath, @"mods\Sideloader Modpack")))
                    return string.Empty;

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Illusion\Koikatu\koikatu"))
                {
                    if (key?.GetValue("INSTALLDIR") is string dir)
                    {
                        dir = Path.Combine(dir, @"mods\Sideloader Modpack");
                        if (Directory.Exists(dir))
                            return dir;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Crash when trying to find Koikatsu mods directory");
                Logger.LogError(e);
            }
            return string.Empty;
        }
    }
}
