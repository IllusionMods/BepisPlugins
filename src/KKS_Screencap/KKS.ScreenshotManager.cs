using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    public partial class ScreenshotManager { }
}
