using BepInEx;
using BepisPlugins;

namespace InputUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class InputUnlocker : BaseUnityPlugin { }
}