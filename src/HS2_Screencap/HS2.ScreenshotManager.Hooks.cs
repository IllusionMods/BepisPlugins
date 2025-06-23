using HarmonyLib;

namespace Screencap
{
    /// <summary>
    /// Disable built-in screenshots
    /// </summary>
    internal static partial class Hooks
    {
        public static bool SoundWasPlayed;

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.Capture), typeof(string))]
        private static bool CaptureOverride()
        {
            SoundWasPlayed = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture), typeof(string))]
        private static bool CaptureOverride2()
        {
            SoundWasPlayed = true;
            return false;
        }
    }
}
