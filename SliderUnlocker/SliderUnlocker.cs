using BepInEx;
using BepisPlugins;
using ChaCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using TMPro;
using UniRx;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInPlugin(GUID, "Slider Unlocker", Version)]
    public class SliderUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.sliderunlocker";
        internal const string Version = Metadata.PluginsVersion;
        /// <summary> Maximum value sliders can possibly extend </summary>
        internal static float SliderAbsoluteMax => Math.Max(SliderMax, 5f);
        /// <summary> Minimum value sliders can possibly extend </summary>
        internal static float SliderAbsoluteMin => Math.Min(SliderMin, -5f);
        /// <summary> Maximum value of sliders when not dynamically unlocked </summary>
        internal static float SliderMax => (Maximum.Value < 100 ? 100 : Maximum.Value) / 100;
        /// <summary> Minimum value of sliders when not dynamically unlocked </summary>
        internal static float SliderMin => (Minimum.Value > 0 ? 0 : Minimum.Value) / 100;

        private static readonly List<Target> _targets = new List<Target>();

        #region Settings
        [DisplayName("Minimum slider value")]
        [Description("Changes will take effect next time the editor is loaded or a character is loaded.")]
        [AcceptableValueRange(-500, 0, false)]
        public static ConfigWrapper<int> Minimum { get; private set; }
        [DisplayName("Maximum slider value")]
        [Description("Changes will take effect next time the editor is loaded or a character is loaded.")]
        [AcceptableValueRange(100, 500, false)]
        public static ConfigWrapper<int> Maximum { get; private set; }
        #endregion

        public SliderUnlocker()
        {
            Minimum = new ConfigWrapper<int>("wideslider-minimum", this, -100);
            Maximum = new ConfigWrapper<int>("wideslider-maximum", this, 200);
        }

        protected void Awake()
        {
            Hooks.InstallHooks();

            foreach (var type in typeof(CvsAccessory).Assembly.GetTypes())
            {
                if (type.Name.StartsWith("Cvs", StringComparison.OrdinalIgnoreCase) &&
                    type != typeof(CvsDrawCtrl) &&
                    type != typeof(CvsColor))
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    var inputFields = fields.Where(x => typeof(TMP_InputField).IsAssignableFrom(x.FieldType)).ToList();
                    if (inputFields.Count == 0)
                        continue;

                    var sliders = fields.Where(x => typeof(Slider).IsAssignableFrom(x.FieldType)).ToList();
                    if (sliders.Count == 0)
                        continue;

                    var buttons = fields.Where(x => typeof(Button).IsAssignableFrom(x.FieldType)).ToList();

                    _targets.Add(new Target(type, inputFields, sliders, buttons));
                }
            }
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode) => SetAllSliders(scene);
        /// <summary>
        /// Sliders that don't work or have issues outside of the 0-100 limit
        /// </summary>
        private static readonly string[] SliderBlacklist = { "sldWaistLowW", "sldHairLength", "sldPitchPow" };

        internal static IEnumerator ResetAllSliders()
        {
            var sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            //Set all sliders to maximum values so when the character is loaded they can be set correctly
            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                        {
                            if (SliderBlacklist.Contains(x.Name))
                                continue;

                            slider.maxValue = SliderAbsoluteMax;
                            slider.minValue = SliderAbsoluteMin;
                        }
                    }
                }
            }

            //Wait for next frame so the character is loaded and values set
            yield return null;

            //Set all slider min/max back to the default unlocked state
            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                        {
                            if (SliderBlacklist.Contains(x.Name))
                                continue;

                            UnlockSlider(slider, slider.value);
                        }
                    }
                }
            }
        }

        private void SetAllSliders(Scene scene)
        {
            var sceneObjects = scene.GetRootGameObjects();

            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                        {
                            if (SliderBlacklist.Contains(x.Name))
                                continue;

                            //Set all sliders to the default unlock state
                            slider.maxValue = SliderMax;
                            slider.minValue = SliderMin;
                        }
                    }

                    bool buttonClicked = false;
                    foreach (var x in target.Fields)
                    {
                        var inputField = (TMP_InputField)x.GetValue(cvs);
                        if (inputField != null)
                        {
                            inputField.characterLimit = 4;

                            //Find the slider that matches this input field
                            FieldInfo sliderFieldInfo = target.Sliders.Where(y => y.Name.Substring(3) == x.Name.Substring(3)).FirstOrDefault();
                            if (sliderFieldInfo == null || SliderBlacklist.Contains(sliderFieldInfo.Name))
                                continue;

                            Slider slider = (Slider)sliderFieldInfo?.GetValue(cvs);
                            if (slider == null)
                                continue;

                            //After reset button click reset the slider unlock state
                            inputField.onValueChanged.AddListener(delegate
                            { InputFieldOnValueChanged(slider, inputField); });
                            void InputFieldOnValueChanged(Slider _slider, TMP_InputField _inputField)
                            {
                                if (buttonClicked)
                                {
                                    buttonClicked = false;
                                    UnlockSliderFromInput(_slider, _inputField);
                                }
                            }

                            //When the user types a value, unlock the sliders to accomodate
                            inputField.onEndEdit.AddListener(delegate
                            { InputFieldOnEndEdit(slider, inputField); });
                            void InputFieldOnEndEdit(Slider _slider, TMP_InputField _inputField) => UnlockSliderFromInput(_slider, _inputField);
                        }
                    }

                    foreach (var x in target.Buttons)
                    {
                        var button = (Button)x.GetValue(cvs);
                        if (button != null)
                        {
                            //Find the slider that matches this button
                            FieldInfo sliderFieldInfo = target.Sliders.Where(y => y.Name.Substring(3) == x.Name.Substring(3)).FirstOrDefault();
                            if (sliderFieldInfo == null || SliderBlacklist.Contains(sliderFieldInfo.Name))
                                continue;

                            Slider slider = (Slider)sliderFieldInfo?.GetValue(cvs);
                            if (slider == null)
                                continue;

                            //When the button is clicked set a flag used by InputFieldOnValueChanged
                            button.onClick.AddListener(delegate
                            { ButtonOnClick(); });
                            void ButtonOnClick() => buttonClicked = true;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Make sure the entered value is within range
        /// </summary>
        private static void UnlockSliderFromInput(Slider _slider, TMP_InputField _inputField)
        {
            float value = float.TryParse(_inputField.text, out float num) ? num / 100 : 0;

            if (value > SliderAbsoluteMax)
            {
                _inputField.text = (SliderAbsoluteMax * 100).ToString();
                value = SliderAbsoluteMax;
            }
            else if (value < SliderAbsoluteMin)
            {
                _inputField.text = (SliderAbsoluteMin * 100).ToString();
                value = SliderAbsoluteMin;
            }
            UnlockSlider(_slider, value);
        }
        /// <summary>
        /// Unlock or lock the slider depending on the entered value
        /// </summary>
        private static void UnlockSlider(Slider _slider, float value)
        {
            int valueRoundedUp = (int)Math.Ceiling(Math.Abs(value));

            if (value > SliderMax)
            {
                _slider.minValue = SliderMin;
                _slider.maxValue = valueRoundedUp;
            }
            else if (value < SliderMin)
            {
                _slider.minValue = -valueRoundedUp;
                _slider.maxValue = SliderMax;
            }
            else
            {
                _slider.minValue = SliderMin;
                _slider.maxValue = SliderMax;
            }
        }

        #region MonoBehaviour
        protected void OnEnable() => SceneManager.sceneLoaded += LevelFinishedLoading;
        protected void OnDisable() => SceneManager.sceneLoaded -= LevelFinishedLoading;
        #endregion

        private sealed class Target
        {
            public Target(Type type, List<FieldInfo> fields, List<FieldInfo> sliders, List<FieldInfo> buttons)
            {
                Type = type;
                Fields = fields;
                Sliders = sliders;
                Buttons = buttons;
            }
            public readonly Type Type;
            public readonly List<FieldInfo> Fields;
            public readonly List<FieldInfo> Sliders;
            public readonly List<FieldInfo> Buttons;
        }
    }
}