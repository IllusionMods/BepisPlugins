using HarmonyLib;

namespace Screencap
{
    public static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "CreateCaptureFileName")]
        private static bool CreateCaptureFileNamePreHook() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "UnityCapture")]
        private static bool UnityCapturePreHook() => false;
    }
}
