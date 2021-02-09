using AIChara;
using CharaCustom;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SliderUnlocker
{
    internal static class HairUnlocker
    {
        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(HairUnlocker));
        }

        // Fix first pos value limit
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetHairCorrectPosValue")]
        private static IEnumerable<CodeInstruction> SetHairCorrectPosValueHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo = AccessTools.Method(typeof(SliderMath), nameof(SliderMath.InverseLerp));
            var instructionsList = instructions.ToList();

            foreach (var inst in instructionsList)
                if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo m && m.Name == "InverseLerp")
                    yield return new CodeInstruction(OpCodes.Call, methodInfo);
                else
                    yield return inst;
        }

        // Fix first rot value limit
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetHairCorrectRotValue")]
        private static IEnumerable<CodeInstruction> SetHairCorrectRotValueHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo = AccessTools.Method(typeof(SliderMath), nameof(SliderMath.InverseLerp));
            var instructionsList = instructions.ToList();

            foreach (var inst in instructionsList)
                if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo m && m.Name == "InverseLerp")
                    yield return new CodeInstruction(OpCodes.Call, methodInfo);
                else
                    yield return inst;
        }

        // Fix last value limit
        [HarmonyTranspiler, HarmonyPatch(typeof(CmpHair), "Update")]
        private static IEnumerable<CodeInstruction> CmpHairHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo = AccessTools.Method(typeof(SliderMath), nameof(SliderMath.Lerp));
            var instructionsList = instructions.ToList();

            foreach (var inst in instructionsList)
                if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo m && m.Name == "Lerp")
                    yield return new CodeInstruction(OpCodes.Call, methodInfo);
                else
                    yield return inst;
        }

        // Fix guide limit
        [HarmonyPrefix, HarmonyPatch(typeof(CustomHairBundleSet), "LateUpdate")]
        private static bool LateUpdateHook(CustomHairBundleSet __instance)
        {
            if (null == __instance.cmpGuid || !__instance.cmpGuid.gameObject.activeInHierarchy)
                return true;

            var o = __instance.cmpGuid.gameObject.GetComponentsInChildren<CustomGuideLimit>();
            foreach (var guideLimit in o)
                guideLimit.limited = false;

            return true;
        }

        // Fix the dynamic slider
        [HarmonyPrefix, HarmonyPatch(typeof(CustomHairBundleSet), "Initialize")]
        private static void InitializeHook(CustomHairBundleSet __instance)
        {
            foreach (var x in __instance.GetComponentsInChildren<CustomSliderSet>())
            {
                SliderUnlocker.UnlockSlider(x.slider, x.slider.value);

                bool buttonClicked = false;

                x.input.characterLimit = 4;

                //After reset button click, reset the slider unlock state
                x.input.onValueChanged.AddListener(
                    _ =>
                    {
                        if (buttonClicked)
                        {
                            buttonClicked = false;
                            SliderUnlocker.UnlockSliderFromInput(x.slider, x.input);
                        }
                    });


                //When the user types a value, unlock the sliders to accomodate
                x.input.onEndEdit.AddListener(_ => SliderUnlocker.UnlockSliderFromInput(x.slider, x.input));

                //When the button is clicked set a flag used by InputFieldOnValueChanged
                x.button.onClick.AddListener(() => buttonClicked = true);
            }
        }

        private static void UnlockAndSetSliderValue(CustomSliderSet slider, float value)
        {
            SliderUnlocker.UnlockSlider(slider.slider, value);
            slider.SetSliderValue(value);
        }

        // Prevent reapplying slider limits in some situations
        [HarmonyTranspiler, HarmonyPatch(typeof(CustomHairBundleSet), "UpdateCustomUI")]
        private static IEnumerable<CodeInstruction> UpdateCustomUIHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo1 = AccessTools.Method(typeof(HairUnlocker), nameof(UnlockAndSetSliderValue));

            var instructionsList = instructions.ToList();
            foreach (var inst in instructionsList)
            {
                if (inst.opcode == OpCodes.Callvirt && inst.operand is MethodInfo m)
                {
                    if (m.Name == "SetSliderValue")
                    {
                        inst.opcode = OpCodes.Call;
                        inst.operand = methodInfo1;
                    }
                }
                yield return inst;
            }
        }

        // Fix the random rollback of rot (dirty fix)
        [HarmonyTranspiler, HarmonyPatch(typeof(CustomHairBundleSet), "SetHairTransform")]
        private static IEnumerable<CodeInstruction> SetHairTransformHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo = AccessTools.Method(typeof(HairUnlocker), nameof(CorrectHairRot));

            var instructionsList = instructions.ToList();
            for (var index = 0; index < instructionsList.Count; index++)
            {
                var inst = instructionsList[index];
                yield return inst;

                if (inst.opcode != OpCodes.Callvirt || !(inst.operand is MethodInfo m) || m.Name != "ChangeSettingHairCorrectRot")
                    continue;

                yield return instructionsList[++index];  //original pop
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, methodInfo);
            }
        }

        private static void CorrectHairRot(CustomHairBundleSet instance)
        {
            var customBase = Singleton<CustomBase>.Instance;
            var chaCtrl = customBase.chaCtrl;

            var boneInfo = chaCtrl.cmpHair[instance.parts].boneInfo[instance.idx];
            var trfCorrect = boneInfo.trfCorrect;
            trfCorrect.eulerAngles = instance.cmpGuid.amount.rotation;

            var localEulerAngles = trfCorrect.localEulerAngles;

            boneInfo.rotRate = new Vector3(
                SliderMath.InverseLerp(boneInfo.rotMin.x, boneInfo.rotMax.x, localEulerAngles.x),
                SliderMath.InverseLerp(boneInfo.rotMin.y, boneInfo.rotMax.y, localEulerAngles.y),
                SliderMath.InverseLerp(boneInfo.rotMin.z, boneInfo.rotMax.z, localEulerAngles.z));

            if (chaCtrl.chaFile.custom.hair.parts[instance.parts].dictBundle.TryGetValue(instance.idx, out var bundleInfo))
                bundleInfo.rotRate = boneInfo.rotRate;
        }
    }
}