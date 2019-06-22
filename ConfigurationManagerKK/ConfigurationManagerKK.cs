using System.Collections;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConfigurationManagerKK
{
    [BepInPlugin(ConfigurationManager.ConfigurationManager.GUID + "KK", "Configuration Manager wrapper for Koikatsu", ConfigurationManager.ConfigurationManager.Version)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    public class ConfigurationManagerKk : BaseUnityPlugin
    {
        private static Texture2D _buttonBackground;
        private Rect _buttonRect;
        private bool _previousWindowState;

        private static bool _isStudio;
        private bool _noCtrlConditionDone;

        private ConfigurationManager.ConfigurationManager _manager;

        private void SetIsDisplaying(bool value)
        {
            if (enabled == value) return;

            enabled = value;

            if (value)
            {
                CalculateWindowRect();

                _manager.DisplayingWindow = _previousWindowState;

                if (_manager.DisplayingWindow)
                    SetGameCanvasInputsEnabled(false);
            }
            else
            {
                SetGameCanvasInputsEnabled(true);
                _previousWindowState = _manager.DisplayingWindow;
                _manager.DisplayingWindow = false;
            }
        }

        public static void SetGameCanvasInputsEnabled(bool mouseInputEnabled)
        {
            foreach (var c in FindObjectsOfType<GraphicRaycaster>())
                c.enabled = mouseInputEnabled;
        }

        private void OnGUI()
        {
            GUI.Box(_buttonRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = _buttonBackground } });
            if (GUI.Button(_buttonRect, new GUIContent("Plugin / mod settings", "Change settings of installed BepInEx plugins.")))
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        private void Start()
        {
            _isStudio = Application.productName == "CharaStudio";

            var buttonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            buttonBackground.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f, 0.85f));
            buttonBackground.Apply();
            _buttonBackground = buttonBackground;

            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.KeyPressedOverride = KeyPressedOverride;
            _manager.DisplayingWindowChanged += (sender, args) => SetGameCanvasInputsEnabled(!args.NewValue);

            SceneManager.sceneLoaded += (arg0, mode) => StartCoroutine(SceneChanged());
            SceneManager.sceneUnloaded += arg0 => StartCoroutine(SceneChanged());
        }

        private IEnumerator SceneChanged()
        {
            // Wait until everything is initialized
            yield return new WaitForEndOfFrame();

            var isConfig = Manager.Scene.Instance.AddSceneName == "Config";

            SetIsDisplaying(isConfig);

            if (isConfig)
            {
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
        }

        private void CalculateWindowRect()
        {
            var buttonOffsetH = Screen.width * 0.12f;
            var buttonWidth = 215f;
            _buttonRect = new Rect(
                Screen.width - buttonOffsetH - buttonWidth, Screen.height * 0.033f, buttonWidth,
                Screen.height * 0.04f);
        }

        private void KeyPressedOverride()
        {
            if (_isStudio && !Manager.Scene.Instance.IsNowLoadingFade && Singleton<StudioScene>.Instance)
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
