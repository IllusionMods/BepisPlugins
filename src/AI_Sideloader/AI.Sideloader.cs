using BepInEx;
using BepisPlugins;
using Sideloader.AutoResolver;
using System.Collections.Generic;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, "1.1.0")]
    [BepInIncompatibility("com.bepis.bepinex.resourceredirector")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "aigirl", "ai girl" };

        private readonly List<HeadPresetInfo> _gatheredHeadPresetInfos = new List<HeadPresetInfo>();
        private readonly List<FaceSkinInfo> _gatheredFaceSkinInfos = new List<FaceSkinInfo>();

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
