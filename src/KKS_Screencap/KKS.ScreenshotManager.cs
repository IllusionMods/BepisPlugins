using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    //[BepInProcess(Constants.StudioProcessName)]
    //[BepInProcess(Constants.VRProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ScreenshotManager : BaseUnityPlugin
    {
    }
}
