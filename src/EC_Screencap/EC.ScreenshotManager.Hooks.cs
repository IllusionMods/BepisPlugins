using HarmonyLib;

namespace Screencap
{
    public static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameScreenShot), "CreateCaptureFileName")]
        public static bool CreateCaptureFileNamePreHook() => false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameScreenShot), "UnityCapture")]
        public static bool UnityCapturePreHook() => false;
    }
}
