using BepInEx;
using BepisPlugins;

namespace ColorCorrector
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ColorCorrector : BaseUnityPlugin { }
}
