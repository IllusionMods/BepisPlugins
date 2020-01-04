using BepInEx;
using BepisPlugins;

namespace Screencap
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ScreenshotManager : BaseUnityPlugin { }
}
