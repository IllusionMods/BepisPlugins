using BepInEx;
using BepisPlugins;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "HS_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for HoneySelect";
        internal const float Offset = 0.18f;

        private string AddSceneName = "";
        private bool _noCtrlConditionDone;

        private static Texture2D _buttonBackground;
        private Rect _buttonRect;
        private bool _previousWindowState;

        private static bool _isStudio;

        private ConfigurationManager.ConfigurationManager _manager;
        private bool _insideConfigScreen;

        private void Start()
        {
            _isStudio = Application.productName == "StudioNEO";

            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            if (!_isStudio)
            {
                var buttonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                buttonBackground.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f, 0.85f));
                buttonBackground.Apply();
                _buttonBackground = buttonBackground;
            }
        }

        private void OnGUI()
        {
            if (!_insideConfigScreen) return;

            GUI.Box(_buttonRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = _buttonBackground } });
            if (GUI.Button(_buttonRect, new GUIContent("Plugin / mod settings", "Change settings of installed BepInEx plugins.")))
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

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

        private IEnumerator SceneChanged()
        {
            // Wait until everything is initialized
            yield return new WaitForEndOfFrame();

            _insideConfigScreen = Manager.Scene.Instance.AddSceneName == "Config";
            if (_insideConfigScreen)
            {
                CalculateWindowRect();

                _manager.DisplayingWindow = _previousWindowState;

                // Button size fix for Koikatsu Party
                var rootNode = GameObject.Find("ConfigScene/Canvas/Node ShortCut");
                if (rootNode != null)
                {
                    foreach (var hGroup in rootNode.transform.Cast<Transform>().Select(x => x.GetComponent<HorizontalLayoutGroup>()))
                    {
                        // HorizontalLayoutGroup stretching is only used in Koikatsu Party, in KK it's null
                        if (hGroup != null && hGroup.childForceExpandWidth)
                        {
                            hGroup.padding = new RectOffset(30, 30, 0, 0);
                            hGroup.childForceExpandWidth = false;
                        }
                    }
                }
            }
            else
            {
                _previousWindowState = _manager.DisplayingWindow;
                _manager.DisplayingWindow = false;
            }
        }

        private void CalculateWindowRect()
        {
            var buttonOffsetH = Screen.width * Offset;
            var buttonWidth = 215f;
            _buttonRect = new Rect(
                Screen.width - buttonOffsetH - buttonWidth, Screen.height * 0.033f, buttonWidth,
                Screen.height * 0.04f);
        }
    }
}
