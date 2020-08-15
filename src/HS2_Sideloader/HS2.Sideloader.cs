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
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, "1.1.0")]
    [BepInIncompatibility("com.bepis.bepinex.resourceredirector")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "hs2", "honeyselect2", "honey select 2" };

        private readonly List<HeadPresetInfo> _gatheredHeadPresetInfos = new List<HeadPresetInfo>();
        private readonly List<FaceSkinInfo> _gatheredFaceSkinInfos = new List<FaceSkinInfo>();

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
