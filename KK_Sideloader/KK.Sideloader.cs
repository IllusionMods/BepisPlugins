using BepInEx;

namespace Sideloader
{
    [BepInDependency(ResourceRedirector.ResourceRedirector.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "koikatsu", "koikatu", "コイカツ" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
