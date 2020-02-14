using BepInEx;
using BepisPlugins;

namespace ColorCorrector
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ColorCorrector : BaseUnityPlugin { }
}