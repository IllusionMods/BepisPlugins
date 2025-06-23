using HarmonyLib;

namespace Screencap
{
    /// <summary>
    /// Disable built-in screenshots
    /// </summary>
    internal static partial class Hooks
    {
        // Hook here instead of hooking GameScreenShot.Capture to not affect the Photo functionality
        [HarmonyPrefix, HarmonyPatch(typeof(AIProject.Scene.MapScene), nameof(AIProject.Scene.MapScene.CaptureSS))]
        private static bool CaptureSSOverride() => false;
    }
}
