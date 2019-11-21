using BepInEx;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        /// <summary> Nuget version for this game specific plugin </summary>
        public const string PluginNugetVersion = "0";
    }
}