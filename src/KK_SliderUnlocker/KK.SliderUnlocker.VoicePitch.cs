/*
 * Code by essu
 * ~ can you hear me now? ~
 */
using ChaCustom;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SliderUnlocker
{
    internal static class VoicePitchUnlocker
    {
        private const float VanillaPitchLower = 0.94f;
        private const float VanillaPitchUpper = 1.06f;
        private const float VanillaPitchRange = VanillaPitchUpper - VanillaPitchLower;

        private const int ExtendedRangeLower = -500;
        private const int ExtendedRangeUpper = 500;

        public static void Init()
        {
            var harmony = Harmony.CreateAndPatchAll(typeof(VoicePitchUnlocker));

            var iteratorType = typeof(CvsChara).GetNestedType("<SetInputText>c__Iterator0", AccessTools.all);
            var iteratorMethod = AccessTools.Method(iteratorType, "MoveNext");
            var transpiler = new HarmonyMethod(typeof(VoicePitchUnlocker), nameof(VoicePitchTpl));
            harmony.Patch(iteratorMethod, null, null, transpiler);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileParameter), "voicePitch", MethodType.Getter)]
        private static bool VoicePitchHook(ChaFileParameter __instance, ref float __result)
        {
            // Replace line return Mathf.Lerp(0.94f, 1.06f, this.voiceRate);
            __result = VanillaPitchLower + __instance.voiceRate * VanillaPitchRange;
            return false;
        }

        public static IEnumerable<CodeInstruction> VoicePitchTpl(IEnumerable<CodeInstruction> _instructions)
        {
            // Changes constants in line this.inpPitchPow.text = CustomBase.ConvertTextFromRate(0, 100, this.param.voiceRate);
            var instructions = new List<CodeInstruction>(_instructions).ToArray();
            instructions[25].opcode = OpCodes.Ldc_I4;
            instructions[25].operand = ExtendedRangeLower;
            instructions[26].opcode = OpCodes.Ldc_I4;
            instructions[26].operand = ExtendedRangeUpper;
            return instructions;
        }
    }
}
