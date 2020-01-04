using BepInEx;
using BepisPlugins;

namespace InputUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class InputUnlocker : BaseUnityPlugin { }
}
