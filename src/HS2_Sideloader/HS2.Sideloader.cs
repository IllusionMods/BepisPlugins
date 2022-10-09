using BepInEx;
using BepisPlugins;
using Sideloader.AutoResolver;
using System.Collections.Generic;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        internal static readonly string[] GameNameList = { "hs2", "honeyselect2", "honey select 2" };
        
        private static string FindKoiZipmodDir() => string.Empty;
    }
}
