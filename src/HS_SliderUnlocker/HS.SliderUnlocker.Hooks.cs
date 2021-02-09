using CustomMenu;
using HarmonyLib;
using System;
using System.Globalization;

namespace SliderUnlocker
{
    public static partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(SubMenuBase), "ChangeTextFromFloat")]
        private static void ConvertTextFromRateHook(ref string __result, float value) => __result = Math.Round(100 * value).ToString(CultureInfo.InvariantCulture);

        [HarmonyPrefix, HarmonyPatch(typeof(SubMenuBase), "ChangeFloatFromText")]
        private static bool ConvertRateFromTextHook(ref float __result, ref string text)
        {
            if (text == null || text == "")
                __result = 0f;
            else
            {
                if (float.TryParse(text, out var val))
                    __result = val / 100;
                else
                    __result = 0f;
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharFile), nameof(CharFile.ClampEx))]
        private static bool ClampExPrefix(float value, ref float __result)
        {
            __result = value;
            return false;
        }
        /// <summary>
        /// Set the shapeValue if outside vanilla range
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CharFemaleCustom), nameof(CharFemaleCustom.SetShapeBodyValue))]
        private static void SetShapeBodyValuePostfix(int index, float value, CharFemaleCustom __instance)
        {
            CharFileInfoCustom charFileInfoCustom = typeof(CharFemaleCustom).GetField("customInfo", AccessTools.all).GetValue(__instance) as CharFileInfoCustom;
            if (index >= charFileInfoCustom.shapeValueBody.Length) return;

            charFileInfoCustom.shapeValueBody[index] = value;
        }
        /// <summary>
        /// Set the shapeValue if outside vanilla range
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CharMaleCustom), nameof(CharMaleCustom.SetShapeBodyValue))]
        private static void SetShapeBodyValuePostfix(int index, float value, CharMaleCustom __instance)
        {
            CharFileInfoCustom charFileInfoCustom = typeof(CharMaleCustom).GetField("customInfo", AccessTools.all).GetValue(__instance) as CharFileInfoCustom;
            if (index >= charFileInfoCustom.shapeValueBody.Length) return;

            charFileInfoCustom.shapeValueBody[index] = value;
        }
        /// <summary>
        /// Set the shapeValue if outside vanilla range
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CharCustom), nameof(CharCustom.SetShapeFaceValue))]
        private static void SetShapeFaceValuePostfix(int index, float value, CharCustom __instance) => (typeof(CharFemaleCustom).GetField("customInfo", AccessTools.all).GetValue(__instance) as CharFileInfoCustom).shapeValueFace[index] = value;

        [HarmonyPrefix, HarmonyPatch(typeof(SubMenuControl), nameof(SubMenuControl.ChangeSubMenu))]
        private static void SmBodyShapeS_FSetCharaInfoSubPrefix() => SliderUnlocker.MaximizeSliders();
        [HarmonyPostfix, HarmonyPatch(typeof(SubMenuControl), nameof(SubMenuControl.ChangeSubMenu))]
        private static void SmBodyShapeS_FSetCharaInfoSubPostfix() => SliderUnlocker.UnlockSliders();
    }
}
