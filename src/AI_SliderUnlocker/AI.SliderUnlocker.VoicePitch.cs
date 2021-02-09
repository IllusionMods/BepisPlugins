using AIChara;
using HarmonyLib;

namespace SliderUnlocker
{
    internal static class VoicePitchUnlocker
    {
        private const float VanillaPitchLower = 0.94f;
        private const float VanillaPitchUpper = 1.06f;
        private const float VanillaPitchRange = VanillaPitchUpper - VanillaPitchLower;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(VoicePitchUnlocker));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileParameter), nameof(ChaFileParameter.voicePitch), MethodType.Getter)]
        private static bool VoicePitchHook(ChaFileParameter __instance, ref float __result)
        {
            // Replace line return Mathf.Lerp(0.94f, 1.06f, this.voiceRate);
            __result = VanillaPitchLower + __instance.voiceRate * VanillaPitchRange;
            return false;
        }
    }
}