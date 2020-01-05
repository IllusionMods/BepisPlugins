using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using BepInEx.Harmony;
using AIChara;
using CharaCustom;
using UnityEngine;

namespace SliderUnlocker
{
    internal static class HairUnlocker
    {
        public static void Init()
        {
            HarmonyWrapper.PatchAll(typeof(HairUnlocker));
        }
        
        // Fix first pos value limit
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetHairCorrectPosValue")]
        public static IEnumerable<CodeInstruction> SetHairCorrectPosValueHook(IEnumerable<CodeInstruction> instructions)
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
        public static IEnumerable<CodeInstruction> SetHairCorrectRotValueHook(IEnumerable<CodeInstruction> instructions)
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
        public static IEnumerable<CodeInstruction> CmpHairHook(IEnumerable<CodeInstruction> instructions)
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
        public static bool LateUpdateHook(CustomHairBundleSet __instance)
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
        public static bool InitializeHook()
        {
            foreach (var c in Object.FindObjectsOfType<CustomHairBundleSet>())
            {
                foreach (var x in c.GetComponentsInChildren<CustomSliderSet>())
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

            return true;
        }

        // Fix the random rollback of pos but rot still (dirty fix)
        [HarmonyTranspiler, HarmonyPatch(typeof(CustomHairBundleSet), "UpdateCustomUI")]
        public static IEnumerable<CodeInstruction> UpdateCustomUIHook(IEnumerable<CodeInstruction> instructions)
        {
            var methodInfo1 = AccessTools.Method(typeof(CustomBase), "ConvertTextFromRate");
            var methodInfo2 = AccessTools.Method(typeof(CustomSliderSet), "SetInputTextValue");

            var instructionsList = instructions.ToList();
            foreach (var inst in instructionsList)
            {
                //this.ssMove[0].SetSliderValue(bundleInfo.moveRate.x);
                //this.ssMove[1].SetInputTextValue(CustomBase.ConvertTextFromRate(0, 100, bundleInfo.moveRate.y));
                if (inst.opcode == OpCodes.Callvirt && inst.operand is MethodInfo m)
                {
                    switch (m.Name)
                    {
                        case "SetSliderValue":
                            yield return new CodeInstruction(OpCodes.Call, methodInfo1);
                            yield return new CodeInstruction(OpCodes.Callvirt, methodInfo2);
                            break;
                        case "get_moveRate":
                        case "get_rotRate":
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                            yield return new CodeInstruction(OpCodes.Ldc_I4_S, 100);
                            yield return new CodeInstruction(OpCodes.Ldloc_0);
                            yield return inst;
                            break;
                        default:
                            yield return inst;
                            break;
                    }
                }
                else
                    yield return inst;
            }
        }

        // Fix the random rollback of rot (dirty fix)
        [HarmonyTranspiler, HarmonyPatch(typeof(CustomHairBundleSet), "SetHairTransform")]
        public static IEnumerable<CodeInstruction> SetHairTransformHook(IEnumerable<CodeInstruction> instructions)
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