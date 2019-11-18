using BepInEx;

namespace BGMLoader
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("KoikatuVR")]
    public partial class BGMLoader : BaseUnityPlugin { }
}
