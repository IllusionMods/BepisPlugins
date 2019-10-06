using BepInEx;
using System.ComponentModel;

namespace ConfigurationManagerWrapper
{
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
