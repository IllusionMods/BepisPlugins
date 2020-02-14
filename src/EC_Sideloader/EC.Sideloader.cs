using BepInEx;
using BepisPlugins;
using Microsoft.Win32;
using System;
using System.IO;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, "1.1.0")]
    [BepInIncompatibility("com.bepis.bepinex.resourceredirector")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "emotioncreators", "emotion creators" };

        private static string FindKoiZipmodDir()
        {
            try
            {
                // Don't look for the KK modpack if a copy of it is already installed in EC
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
