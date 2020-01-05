using HarmonyLib;
using BepInEx.Harmony;
using AIChara;

namespace SliderUnlocker
{
    internal static class VoicePitchUnlocker
    {
        private const float VanillaPitchLower = 0.94f;
        private const float VanillaPitchUpper = 1.06f;
        private const float VanillaPitchRange = VanillaPitchUpper - VanillaPitchLower;

        public static void Init()
        {
            HarmonyWrapper.PatchAll(typeof(VoicePitchUnlocker));
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileParameter), "get_voicePitch")]
        public static bool VoicePitchHook(ChaFileParameter __instance, ref float __result)
        {
            // Replace line return Mathf.Lerp(0.94f, 1.06f, this.voiceRate);
            __result = VanillaPitchLower + __instance.voiceRate * VanillaPitchRange;
            return false;
        }
    }
}