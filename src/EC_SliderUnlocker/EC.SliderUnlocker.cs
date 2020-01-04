using BepInEx;
using BepisPlugins;

namespace SliderUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class SliderUnlocker : BaseUnityPlugin { }
}
