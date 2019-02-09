using BepInEx;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerKK
{
    [BepInPlugin(ConfigurationManager.ConfigurationManager.GUID + "KK", "Configuration Manager wrapper for Koikatsu", ConfigurationManager.ConfigurationManager.Version)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    public class ConfigurationManagerKk : BaseUnityPlugin
    {
        private static Texture2D _buttonBackground;
        private Rect _buttonRect;
        private bool _displayingButton;
        private bool _previousWindowState;

        private static bool _isStudio;
        private bool _noCtrlConditionDone;

        private ConfigurationManager.ConfigurationManager _manager;
        
        public bool DisplayingButton
        {
            get => _displayingButton;
            set
            {
                if (_displayingButton == value) return;

                _displayingButton = value;

                if (_displayingButton)
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
        }

        public static void SetGameCanvasInputsEnabled(bool mouseInputEnabled)
        {
            foreach (var c in FindObjectsOfType<GraphicRaycaster>())
                c.enabled = mouseInputEnabled;
        }

        protected void OnGUI()
        {
            if (DisplayingButton)
            {
                GUI.Box(_buttonRect, GUIContent.none, new GUIStyle {normal = new GUIStyleState {background = _buttonBackground}});
                if (GUI.Button(
                    _buttonRect, new GUIContent(
                        "Plugin / mod settings",
                        "Change settings of installed BepInEx plugins.")))
                    _manager.DisplayingWindow = !_manager.DisplayingWindow;
            }
        }

        protected void Start()
        {
            _isStudio = Application.productName == "CharaStudio";

            var buttonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            buttonBackground.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f, 0.85f));
            buttonBackground.Apply();
            _buttonBackground = buttonBackground;

            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.KeyPressedOverride = KeyPressedOverride;
            _manager.DisplayingWindowChanged += (sender, args) => SetGameCanvasInputsEnabled(!args.NewValue);
        }

        protected void Update()
        {
            DisplayingButton = IsConfigOpened();
        }

        private void CalculateWindowRect()
        {
            var buttonOffsetH = Screen.width * 0.12f;
            var buttonWidth = 215f;
            _buttonRect = new Rect(
                Screen.width - buttonOffsetH - buttonWidth, Screen.height * 0.033f, buttonWidth,
                Screen.height * 0.04f);
        }

        private static bool IsConfigOpened()
        {
            return Scene.Instance.AddSceneName == "Config";
        }

        private void KeyPressedOverride()
        {
            if (_isStudio && !Scene.Instance.IsNowLoadingFade && Singleton<StudioScene>.Instance)
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
