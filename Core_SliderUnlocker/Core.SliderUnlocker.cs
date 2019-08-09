using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepisPlugins;
using System;
using UnityEngine.UI;

namespace SliderUnlocker
{
    public partial class SliderUnlocker
    {
        public const string GUID = "com.bepis.bepinex.sliderunlocker";
        internal const string Version = Metadata.PluginsVersion;
        public const string PluginName = "Slider Unlocker";
        internal static new ManualLogSource Logger;

        /// <summary> Maximum value sliders can possibly extend </summary>
        internal static float SliderAbsoluteMax => Math.Max(SliderMax, 5f);
        /// <summary> Minimum value sliders can possibly extend </summary>
        internal static float SliderAbsoluteMin => Math.Min(SliderMin, -5f);
        /// <summary> Maximum value of sliders when not dynamically unlocked </summary>
        internal static float SliderMax => (Maximum.Value < 100 ? 100 : Maximum.Value) / 100f;
        /// <summary> Minimum value of sliders when not dynamically unlocked </summary>
        internal static float SliderMin => (Minimum.Value > 0 ? 0 : Minimum.Value) / 100f;


        [AcceptableValueRange(-500, 0, false)]
        public static ConfigWrapper<int> Minimum { get; private set; }

        [AcceptableValueRange(100, 500, false)]
        public static ConfigWrapper<int> Maximum { get; private set; }

        public SliderUnlocker()
        {
            Minimum = Config.Wrap("Slider Limits", "Minimum slider value", "Changes will take effect next time the editor is loaded or a character is loaded.", 0);
            Maximum = Config.Wrap("Slider Limits", "Maximum slider value", "Changes will take effect next time the editor is loaded or a character is loaded.", 100);
        }

        protected void Awake()
        {
            Hooks.InstallHooks();
            Logger = base.Logger;
        }
        /// <summary>
        /// Unlock or lock the slider depending on the entered value
        /// </summary>
        private static void UnlockSlider(Slider _slider, float value, bool defaultRange = false)
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
    }
}