using System;
using BepInEx;
using BepisPlugins;
using HarmonyLib;
using System.Globalization;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class SliderUnlocker : BaseUnityPlugin
    {
        /// <summary>
        /// Set all sliders to their default min/max and set up events
        /// </summary>
        internal static void SetAllSliders(EditMode editMode)
        {
            foreach (var x in editMode.GetComponentsInChildren<InputSliderUI>(true))
            {
                var slider = GetSlider(x);
                var inputField = GetInputField(x);
                var defButton = GetDefButton(x);

                UnlockSliderFromInputPH(slider, inputField, slider.value);

                inputField.characterLimit = 4;

                //When the user types a value, unlock the sliders to accomodate
                inputField.onEndEdit.AddListener(newVal => UnlockSliderFromInputPH(slider, inputField, float.TryParse(newVal, out var num) ? num : 0));

                //When the button is clicked set a flag used by InputFieldOnValueChanged
                defButton.onClick.AddListener(() => UnlockSliderFromInputPH(slider, inputField, x.Value));
            }
        }

        internal static void UnlockSliderFromInputPH(Slider _slider, InputField _inputField, float value)
        {
            var absoluteMax = Math.Max(SliderMax, 500f);
            var absoluteMin = Math.Min(SliderMin, -500f);
            if (value > absoluteMax)
            {
                _inputField.text = absoluteMax.ToString(CultureInfo.InvariantCulture);
                value = absoluteMax;
            }
            else if (value < absoluteMin)
            {
                _inputField.text = absoluteMin.ToString(CultureInfo.InvariantCulture);
                value = absoluteMin;
            }

            UnlockSliderPH(_slider, value);
            _slider.value = value;
        }

        internal static void UnlockSliderPH(Slider _slider, float value, bool defaultRange = false)
        {
            var valueRoundedUp = (int) Math.Ceiling(Math.Abs(value));
            var max = defaultRange ? 100 : SliderMax;
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

        internal static Button GetDefButton(InputSliderUI x)
        {
            var defButton = Traverse.Create(x).Field("defButton").GetValue() as Button;
            if (defButton == null) throw new ArgumentNullException(nameof(defButton));
            return defButton;
        }

        internal static InputField GetInputField(InputSliderUI x)
        {
            var inputField = Traverse.Create(x).Field("inputField").GetValue() as InputField;
            if (inputField == null) throw new ArgumentNullException(nameof(inputField));
            return inputField;
        }

        internal static CheckableSlider GetSlider(InputSliderUI x)
        {
            var slider = Traverse.Create(x).Field("slider").GetValue() as CheckableSlider;
            if (slider == null) throw new ArgumentNullException(nameof(slider));
            return slider;
        }
    }
}
