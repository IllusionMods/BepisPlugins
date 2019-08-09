using BepInEx;
using BepInEx.Configuration;
using BepisPlugins;
using ChaCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
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
        internal static float SliderMax => (Maximum.Value < 100 ? 100 : Maximum.Value) / 100f;
        /// <summary> Minimum value of sliders when not dynamically unlocked </summary>
        internal static float SliderMin => (Minimum.Value > 0 ? 0 : Minimum.Value) / 100f;

        private static readonly List<Target> _targets = new List<Target>();

        #region Settings
        [AcceptableValueRange(-500, 0, false)]
        public static ConfigWrapper<int> Minimum { get; private set; }

        [AcceptableValueRange(100, 500, false)]
        public static ConfigWrapper<int> Maximum { get; private set; }
        #endregion

        public SliderUnlocker()
        {
            Minimum = Config.Wrap("Slider Limits", "Minimum slider value", "Changes will take effect next time the editor is loaded or a character is loaded.", 0);
            Maximum = Config.Wrap("Slider Limits", "Maximum slider value", "Changes will take effect next time the editor is loaded or a character is loaded.", 100);
        }

        protected void Awake()
        {
            Hooks.InstallHooks();

            VoicePitchUnlocker.Init();

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

        private static void LevelFinishedLoading(Scene scene, LoadSceneMode mode) => SetAllSliders(scene);

        /// <summary>
        /// Sliders that don't work or have issues outside of the 0-100 limit
        /// </summary>
        private static readonly string[] SliderBlacklist = { "sldWaistLowW", "sldHairLength" };

        internal static IEnumerator ResetAllSliders()
        {
            var sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            //Set all sliders to maximum values so when the character is loaded they can be set correctly
            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type, true));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                        {
                            slider.maxValue = SliderAbsoluteMax;
                            slider.minValue = SliderAbsoluteMin;
                        }
                    }
                }
            }

            //Wait for next frame so the character is loaded and values set
            yield return null;
            sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            //Set all slider min/max back to the default unlocked state
            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type, true));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                        {
                            UnlockSlider(slider, slider.value, SliderBlacklist.Contains(x.Name));
                        }
                    }
                }
            }
        }

        private static void SetAllSliders(Scene scene)
        {
            var sceneObjects = scene.GetRootGameObjects();

            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type, true));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    ResetAllRangesToDefault(target, cvs);

                    var buttonClicked = false;
                    foreach (var x in target.Fields)
                    {
                        var inputField = (TMP_InputField)x.GetValue(cvs);
                        if (inputField == null)
                            continue;

                        inputField.characterLimit = 4;

                        //Find the slider that matches this input field
                        var sliderFieldInfo = target.Sliders.FirstOrDefault(y => y.Name.Substring(3) == x.Name.Substring(3));
                        if (sliderFieldInfo == null)
                            continue;

                        var slider = (Slider)sliderFieldInfo.GetValue(cvs);
                        if (slider == null)
                            continue;

                        //After reset button click reset the slider unlock state
                        inputField.onValueChanged.AddListener(
                            _ =>
                            {
                                if (buttonClicked)
                                {
                                    buttonClicked = false;
                                    UnlockSliderFromInput(slider, inputField, SliderBlacklist.Contains(sliderFieldInfo.Name));
                                }
                            });

                        //When the user types a value, unlock the sliders to accomodate
                        inputField.onEndEdit.AddListener(_ => UnlockSliderFromInput(slider, inputField, SliderBlacklist.Contains(sliderFieldInfo.Name)));
                    }

                    foreach (var x in target.Buttons)
                    {
                        var button = (Button)x.GetValue(cvs);
                        if (button == null)
                            continue;

                        //Find the slider that matches this button
                        var sliderFieldInfo = target.Sliders.FirstOrDefault(y => y.Name.Substring(3) == x.Name.Substring(3));
                        if (sliderFieldInfo == null)
                            continue;

                        var slider = (Slider)sliderFieldInfo.GetValue(cvs);
                        if (slider == null)
                            continue;

                        //When the button is clicked set a flag used by InputFieldOnValueChanged
                        button.onClick.AddListener(() => buttonClicked = true);
                    }
                }
            }
        }

        private static void ResetAllRangesToDefault(Target target, UnityEngine.Component cvs)
        {
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
        }
        /// <summary>
        /// Make sure the entered value is within range
        /// </summary>
        private static void UnlockSliderFromInput(Slider _slider, TMP_InputField _inputField, bool defaultRange)
        {
            var value = float.TryParse(_inputField.text, out var num) ? num / 100 : 0;

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
            UnlockSlider(_slider, value, defaultRange);
        }
        /// <summary>
        /// Unlock or lock the slider depending on the entered value
        /// </summary>
        private static void UnlockSlider(Slider _slider, float value, bool defaultRange)
        {
            var valueRoundedUp = (int)Math.Ceiling(Math.Abs(value));
            var max = defaultRange ? 1 : SliderMax;
            var min = defaultRange ? 0 : SliderMin;

            if (value > max)
            {
                _slider.minValue = min;
                _slider.maxValue = valueRoundedUp;
            }
            else if (value < min)
            {
                _slider.minValue = -valueRoundedUp;
                _slider.maxValue = max;
            }
            else
            {
                _slider.minValue = min;
                _slider.maxValue = max;
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