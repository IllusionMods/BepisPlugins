using BepInEx;
using BepisPlugins;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, "1.1.0")]
    [BepInIncompatibility("com.bepis.bepinex.resourceredirector")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "koikatsu sunshine", "koikatu sunshine", "コイカツ sunshine" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
