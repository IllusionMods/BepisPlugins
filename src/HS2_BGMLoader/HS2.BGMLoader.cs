using BepInEx;
using BepisPlugins;

namespace BGMLoader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    public partial class BGMLoader : BaseUnityPlugin { }
}
