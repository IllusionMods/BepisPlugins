using HarmonyLib;
using System;
using System.Globalization;

namespace SliderUnlocker
{
    public static partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomBase), "ConvertTextFromRate")]
        public static void ConvertTextFromRateHook(ref string __result, int min, int max, float value)
        {
            if (min == 0 && max == 100)
                __result = Math.Round(100 * value).ToString(CultureInfo.InvariantCulture);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomBase), "ConvertRateFromText")]
        public static void ConvertRateFromTextHook(ref float __result, int min, int max, string buf)
        {
            if (min == 0 && max == 100)
            {
                if (buf.IsNullOrEmpty())
                    __result = 0f;
                else
                {
                    if (!float.TryParse(buf, out float val))
                        __result = 0f;
                    else
                        __result = val / 100;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), "CheckDataRange")]
        public static bool CheckDataRangePreHook(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance) => __instance.StartCoroutine(SliderUnlocker.ResetAllSliders());
    }
}
