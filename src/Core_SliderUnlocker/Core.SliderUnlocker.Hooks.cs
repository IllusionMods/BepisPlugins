﻿using BepInEx.Harmony;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
#if KK || EC
using ChaCustom;
#elif AI || HS2
using CharaCustom;
using AIChara;
#endif

namespace SliderUnlocker
{
    public static partial class Hooks
    {
        public static void InstallHooks() => HarmonyWrapper.PatchAll(typeof(Hooks));

        private static readonly FieldInfo akf_dictInfo = typeof(AnimationKeyInfo).GetField("dictInfo", AccessTools.all);

        [HarmonyPostfix, HarmonyPatch(typeof(Mathf), "Clamp", typeof(float), typeof(float), typeof(float))]
        public static void MathfClampHook(ref float __result, float value, float min, float max)
        {
            if (min == 0f && max == 100f)
                __result = value;
        }

        [ParameterByRef(2)]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AnimationKeyInfo), "GetInfo", typeof(string), typeof(float), typeof(Vector3), typeof(byte))]
        public static void GetInfoSingularPreHook(ref float __state, string name, ref float rate, ref Vector3 value, byte type)
        {
            __state = rate;

            if (rate > 1)
                rate = 1f;


            if (rate < 0)
                rate = 0f;
        }

        [ParameterByRef(2)]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AnimationKeyInfo), "GetInfo", typeof(string), typeof(float), typeof(Vector3), typeof(byte))]
        public static void GetInfoSingularPostHook(AnimationKeyInfo __instance, bool __result, float __state, string name, float rate, ref Vector3 value, byte type)
        {
            if (!__result)
                return;

            rate = __state;

            if (rate < 0f || rate > 1f)
            {
                var dictInfo = (Dictionary<string, List<AnimationKeyInfo.AnmKeyInfo>>)akf_dictInfo.GetValue(__instance);

                List<AnimationKeyInfo.AnmKeyInfo> list = dictInfo[name];

                switch (type)
                {
                    case 0:
                        value = SliderMath.CalculatePosition(list, rate);
                        break;
                    case 1:
                        value = SliderMath.SafeCalculateRotation(value, name, list, rate);
                        break;
                    default:
                        value = SliderMath.CalculateScale(list, rate);
                        break;
                }
            }
        }

        [ParameterByRef(2)]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AnimationKeyInfo), "GetInfo", typeof(string), typeof(float), typeof(Vector3[]), typeof(bool[]))]
        public static void GetInfoPreHook(ref float __state, string name, ref float rate, ref Vector3[] value, bool[] flag)
        {
            __state = rate;

            if (rate > 1)
                rate = 1f;


            if (rate < 0)
                rate = 0f;
        }

        [ParameterByRef(2)]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AnimationKeyInfo), "GetInfo", typeof(string), typeof(float), typeof(Vector3[]), typeof(bool[]))]
        public static void GetInfoPostHook(AnimationKeyInfo __instance, bool __result, float __state, string name, float rate, ref Vector3[] value, bool[] flag)
        {
            if (!__result)
                return;

            rate = __state;

            if (rate < 0f || rate > 1f)
            {
                var dictInfo = (Dictionary<string, List<AnimationKeyInfo.AnmKeyInfo>>)akf_dictInfo.GetValue(__instance);

                List<AnimationKeyInfo.AnmKeyInfo> list = dictInfo[name];


                if (flag[0])
                    value[0] = SliderMath.CalculatePosition(list, rate);

                if (flag[1])
                    value[1] = SliderMath.SafeCalculateRotation(value[1], name, list, rate);

                if (flag[2])
                    value[2] = SliderMath.CalculateScale(list, rate);
            }
        }

#if KK || EC || AI || HS2
        [HarmonyPostfix, HarmonyPatch(typeof(CustomBase), "ConvertTextFromRate")]
        public static void ConvertTextFromRateHook(ref string __result, int min, int max, float value)
        {
            if (min == 0 && max == 100)
                __result = Math.Round(100 * value).ToString(CultureInfo.InvariantCulture);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomBase), "ConvertRateFromText")]
        public static void ConvertRateFromTextHook(ref float __result, int min, int max, string buf)
        {
            if (min == 0 && max == 100)
            {
                if (buf == null || buf == "")
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
#endif

#if KK  // Prevent slider values from getting clamped to 0.2 - 0.8 range in school mode
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitShapeBody))]
        public static void InitShapeBodySliderFixPre(ChaControl __instance, out bool __state)
        {
            __state = __instance.hiPoly;
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, true, null);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitShapeBody))]
        public static void InitShapeBodySliderFixPost(ChaControl __instance, bool __state)
        {
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, __state, null);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateShapeBodyValueFromCustomInfo))]
        public static void UpdateShapeBodyValueFromCustomInfoSliderFixPre(ChaControl __instance, out bool __state)
        {
            __state = __instance.hiPoly;
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, true, null);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateShapeBodyValueFromCustomInfo))]
        public static void UpdateShapeBodyValueFromCustomInfoSliderFixPost(ChaControl __instance, bool __state)
        {
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, __state, null);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetShapeBodyValue))]
        public static void SetShapeBodyValueSliderFixPre(ChaControl __instance, out bool __state)
        {
            __state = __instance.hiPoly;
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, true, null);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetShapeBodyValue))]
        public static void SetShapeBodyValueInfoSliderFixPost(ChaControl __instance, bool __state)
        {
            AccessTools.Property(typeof(ChaControl), nameof(ChaControl.hiPoly)).SetValue(__instance, __state, null);
        }
#endif
    }
}