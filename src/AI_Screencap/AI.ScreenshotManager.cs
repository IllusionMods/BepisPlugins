using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInIncompatibility("Screencap")]
    [BepInIncompatibility("EdgeDestroyer")]
    public partial class ScreenshotManager
    {
        private const string PluginName = "Screenshot Manager";
    }
}
