using BepInEx;
using BepisPlugins;

namespace InputUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class InputUnlocker : BaseUnityPlugin { }
}