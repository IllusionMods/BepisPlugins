using BepInEx;
using BepisPlugins;
using System.ComponentModel;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string GUID = "EC_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for EmotionCreators";
        internal const float Offset = 0.12f;
    }
}
