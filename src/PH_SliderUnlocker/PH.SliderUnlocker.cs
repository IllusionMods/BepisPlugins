using BepInEx;
using BepisPlugins;
using HarmonyLib;
using System;
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
                // Skip modded sliders
                if (x.name.EndsWith("(MakerAPI)", StringComparison.Ordinal)) continue;

                UnlockSliderPH(x, x.Value);

                //When the user types a value, unlock the sliders to accomodate
                var inputField = GetInputField(x);
                inputField.characterLimit = 4;
                inputField.onEndEdit.AddListener(newVal => UnlockSliderFromInputPH(x, float.TryParse(newVal, out var num) ? num : 0));

                var defButton = GetDefButton(x);
                defButton.onClick.AddListener(() => UnlockSliderFromInputPH(x, x.Value));
            }
        }

        internal static void UnlockSliderFromInputPH(InputSliderUI x, float value)
        {
            var slider = GetSlider(x);
            var inputField = GetInputField(x);

            var absoluteMax = Math.Max(SliderMax, 500f);
            var absoluteMin = Math.Min(SliderMin, -500f);
            if (value > absoluteMax)
            {
                inputField.text = absoluteMax.ToString(CultureInfo.InvariantCulture);
                value = absoluteMax;
            }
            else if (value < absoluteMin)
            {
                inputField.text = absoluteMin.ToString(CultureInfo.InvariantCulture);
                value = absoluteMin;
            }

            UnlockSliderPH(x, value);
            slider.value = value;
        }

        internal static void UnlockSliderPH(InputSliderUI x, float value, bool defaultRange = false)
        {
            var slider = GetSlider(x);

            var valueRoundedUp = (int)Math.Ceiling(Math.Abs(value));
            var max = defaultRange ? 100 : SliderMax;
            var min = defaultRange ? 0 : SliderMin;

            if (value > max)
            {
                slider.minValue = min;
                slider.maxValue = valueRoundedUp;
            }
            else if (value < min)
            {
                slider.minValue = -valueRoundedUp;
                slider.maxValue = max;
            }
            else
            {
                slider.minValue = min;
                slider.maxValue = max;
            }

            // Refresh default value marker position
            Traverse.Create(x).Method("SetDefPos").GetValue();
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
