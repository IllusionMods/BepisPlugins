using BepInEx;

namespace BGMLoader
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInProcess("AI-Syoujyo")]
    public partial class BGMLoader : BaseUnityPlugin { }
}
