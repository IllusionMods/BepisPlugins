using BepInEx;
using BepisPlugins;

namespace InputUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class InputUnlocker : BaseUnityPlugin { }
}