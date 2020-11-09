using HarmonyLib;

namespace Screencap
{
    public static partial class Hooks
    {
        /// <summary>
        /// Cancel the vanilla screenshot
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), "Capture")]
        private static bool StudioCapturePreHook() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        private static bool CreatePngScreenPrefix(ref int _width, ref int _height)
        {
            //Multiply up render resolution.
            _width *= CardRenderRate;
            _height *= CardRenderRate;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        private static void CreatePngScreenPostfix(ref byte[] __result) => DownscaleEncoded(ref __result);
    }
}
