using HarmonyLib;

namespace Screencap
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(EmocreScreenShot), nameof(EmocreScreenShot.ScreenShot))]
        private static bool EmocreScreenShotPreHook() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture))]
        private static bool UnityCapturePreHook() => false;
    }
}
