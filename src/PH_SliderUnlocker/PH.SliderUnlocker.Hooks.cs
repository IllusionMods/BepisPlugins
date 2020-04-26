using HarmonyLib;
using System;
using System.Globalization;

namespace SliderUnlocker
{
    public static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EditMode), nameof(EditMode.Setup))]
        public static void EditModeSetup(EditMode __instance)
        {
            SliderUnlocker.SetAllSliders(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputSliderUI), "OnChanged", typeof(float), typeof(bool))]
        public static void EditModeSetup(InputSliderUI __instance, float val)
        {
            var slider = SliderUnlocker.GetSlider(__instance);
            if (slider.maxValue >= val) return;
            SliderUnlocker.UnlockSliderPH(__instance, val);
        }
    }
}
