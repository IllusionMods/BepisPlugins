using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ScreenshotManager : BaseUnityPlugin { }
}
