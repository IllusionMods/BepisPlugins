using System.Collections;
using System.Linq;
using BepisPlugins;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    public partial class ConfigurationManagerWrapper
    {
        public const string Version = Metadata.PluginsVersion;

        private static Texture2D _buttonBackground;
        private Rect _buttonRect;
        private bool _previousWindowState;

        private static bool _isStudio;

        private ConfigurationManager.ConfigurationManager _manager;
        private bool _insideConfigScreen;

        private void OnGUI()
        {
            if (!_insideConfigScreen) return;

            GUI.Box(_buttonRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = _buttonBackground } });
            if (GUI.Button(_buttonRect, new GUIContent("Plugin / mod settings", "Change settings of installed BepInEx plugins.")))
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        private void Start()
        {
            _isStudio = Application.productName == "CharaStudio" || Application.productName == "StudioNEO";

            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            if (!_isStudio)
            {
                var buttonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                buttonBackground.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f, 0.85f));
                buttonBackground.Apply();
                _buttonBackground = buttonBackground;

#if !HS
                SceneManager.sceneLoaded += (arg0, mode) => StartCoroutine(SceneChanged());
                SceneManager.sceneUnloaded += arg0 => StartCoroutine(SceneChanged());
#endif
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
