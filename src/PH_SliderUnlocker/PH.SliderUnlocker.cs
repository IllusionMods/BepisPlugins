using BepInEx;
using BepisPlugins;
using System;
using System.Globalization;

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
                x.inputField.characterLimit = 4;
                x.inputField.onEndEdit.AddListener(newVal => UnlockSliderFromInputPH(x, float.TryParse(newVal, out var num) ? num : 0));

                x.defButton.onClick.AddListener(() => UnlockSliderFromInputPH(x, x.Value));
            }
        }

        internal static void UnlockSliderFromInputPH(InputSliderUI x, float value)
        {
            var absoluteMax = Math.Max(SliderMax, 500f);
            var absoluteMin = Math.Min(SliderMin, -500f);
            if (value > absoluteMax)
            {
                x.inputField.text = absoluteMax.ToString(CultureInfo.InvariantCulture);
                value = absoluteMax;
            }
            else if (value < absoluteMin)
            {
                x.inputField.text = absoluteMin.ToString(CultureInfo.InvariantCulture);
                value = absoluteMin;
            }

            UnlockSliderPH(x, value);
            x.slider.value = value;
        }

        internal static void UnlockSliderPH(InputSliderUI x, float value, bool defaultRange = false)
        {
            var valueRoundedUp = (int)Math.Ceiling(Math.Abs(value));
            var max = defaultRange ? 100 : SliderMax;
            var min = defaultRange ? 0 : SliderMin;

            if (value > max)
            {
                x.slider.minValue = min;
                x.slider.maxValue = valueRoundedUp;
            }
            else if (value < min)
            {
                x.slider.minValue = -valueRoundedUp;
                x.slider.maxValue = max;
            }
            else
            {
                x.slider.minValue = min;
                x.slider.maxValue = max;
            }

            // Refresh default value marker position
            x.SetDefPos();
        }
    }
}
