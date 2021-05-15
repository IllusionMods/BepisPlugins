using HarmonyLib;

namespace SliderUnlocker
{
    public static partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EditMode), nameof(EditMode.Setup))]
        private static void EditModeSetup(EditMode __instance)
        {
            SliderUnlocker.SetAllSliders(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InputSliderUI), nameof(InputSliderUI.OnChanged), typeof(float), typeof(bool))]
        private static void EditModeSetup(InputSliderUI __instance, float val)
        {
            if (__instance.slider.maxValue >= val) return;
            SliderUnlocker.UnlockSliderPH(__instance, val);
        }
    }
}
