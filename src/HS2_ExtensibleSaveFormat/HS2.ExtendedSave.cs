using BepInEx;
using BepisPlugins;

namespace ExtensibleSaveFormat
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin { }
}