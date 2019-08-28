using BepInEx;
using System.ComponentModel;
using UnityEngine;

namespace ConfigurationManagerWrapper
{
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string GUID = "KK_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for Koikatsu";
        internal const float Offset = 0.12f;

        private bool _noCtrlConditionDone;

        private void Update()
        {
            // Main game is handled in SceneChanged and OnGUI
            if (!_isStudio) return;

            if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.Instance.IsNowLoadingFade)
            {
                _manager.DisplayingWindow = !_manager.DisplayingWindow;

                if (!_noCtrlConditionDone)
                {
                    var oldCondition = Studio.Studio.Instance.cameraCtrl.noCtrlCondition;
                    Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => _manager.DisplayingWindow || oldCondition();
                    _noCtrlConditionDone = true;
                }
            }
        }
    }
}
