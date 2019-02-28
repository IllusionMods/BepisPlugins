using BepInEx;
using BepisPlugins;
using ChaCustom;
using System;
using System.Collections;
using System.Collections.Generic;
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
        internal static float SliderMax = 5;
        internal static float SliderMin = -5;

        private static readonly List<Target> _targets = new List<Target>();

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

                    _targets.Add(new Target(type, inputFields, sliders));
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

            //Set all sliders to maximum values
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

                            slider.maxValue = SliderMax;
                            slider.minValue = SliderMin;
                        }
                    }
                }
            }

            //Wait for next frame so the character is loaded and values set
            yield return null;

            //Set all slider min/max to the unlocked state
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

                            inputField.onEndEdit.AddListener(delegate
                            { InputFieldOnEndEdit(inputField, slider); });
                            void InputFieldOnEndEdit(TMP_InputField _inputField, Slider _slider) => UnlockSlider(_slider, _inputField);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Make sure the entered value is within range
        /// </summary>
        private static void UnlockSlider(Slider _slider, TMP_InputField _inputField)
        {
            float value = float.TryParse(_inputField.text, out float num) ? num / 100 : 0;
            if (value > SliderMax)
            {
                _inputField.text = (SliderMax * 100).ToString();
                value = SliderMax;
            }
            else if (value < SliderMin)
            {
                _inputField.text = (SliderMin * 100).ToString();
                value = SliderMin;
            }
            UnlockSlider(_slider, value);
        }
        /// <summary>
        /// Unlock or lock the slider depending on the entered value
        /// </summary>
        private static void UnlockSlider(Slider _slider, float value)
        {
            int valueRoundedUp = (int)Math.Ceiling(Math.Abs(value));

            if (value == 0f)
            {
                _slider.minValue = 0;
                _slider.maxValue = 1;
            }
            else if (value < 0)
            {
                _slider.minValue = -valueRoundedUp;
                _slider.maxValue = 1;
            }
            else
            {
                _slider.minValue = 0;
                _slider.maxValue = valueRoundedUp;
            }
        }

        #region MonoBehaviour
        protected void OnEnable() => SceneManager.sceneLoaded += LevelFinishedLoading;
        protected void OnDisable() => SceneManager.sceneLoaded -= LevelFinishedLoading;
        #endregion

        private sealed class Target
        {
            public Target(Type type, List<FieldInfo> fields, List<FieldInfo> sliders)
            {
                Type = type;
                Fields = fields;
                Sliders = sliders;
            }
            public readonly Type Type;
            public readonly List<FieldInfo> Fields;
            public readonly List<FieldInfo> Sliders;
        }
    }
}