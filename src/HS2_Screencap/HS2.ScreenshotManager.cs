using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInIncompatibility("Screencap")]
    [BepInIncompatibility("EdgeDestroyer")]
    public partial class ScreenshotManager { }
}
