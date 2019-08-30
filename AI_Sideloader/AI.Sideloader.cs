using BepInEx;

namespace Sideloader
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "aigirl", "ai girl" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
