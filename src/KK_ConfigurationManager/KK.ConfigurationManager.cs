using BepInEx;
using BepisPlugins;
using System.ComponentModel;
using UnityEngine;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string GUID = "KK_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for Koikatsu";
        internal const float Offset = 0.12f;

        private void Update()
        {
            // Main game is handled in SceneChanged and OnGUI
            if (!_isStudio) return;

            if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.Instance.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }
    }
}
