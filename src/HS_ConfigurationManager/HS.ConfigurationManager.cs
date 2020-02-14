using BepInEx;
using BepisPlugins;
using System.ComponentModel;
using UnityEngine;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInProcess(Constants.BattleArenaProcessName)]
    [BepInProcess(Constants.BattleArenaProcessName32bit)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string GUID = "HS_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for HoneySelect";
        internal const float Offset = 0.18f;

        private string AddSceneName = "";
        private bool _noCtrlConditionDone;

        private void Update()
        {
            if (Manager.Scene.Instance.AddSceneName != AddSceneName)
            {
                Logger.LogInfo(Manager.Scene.Instance.AddSceneName);
                AddSceneName = Manager.Scene.Instance.AddSceneName;
                StartCoroutine(SceneChanged());
            }

            if (Input.GetKeyDown(KeyCode.F1) && _isStudio && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.Instance.IsNowLoadingFade)
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
