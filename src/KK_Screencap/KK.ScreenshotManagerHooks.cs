using HarmonyLib;

namespace Screencap
{
    public static partial class Hooks
    {
        /// <summary>
        /// Cancel the vanilla screenshot
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), "Capture")]
        public static bool StudioCapturePreHook() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        public static bool pre_CreatePngScreen(ref int _width, ref int _height)
        {
            //Multiply up render resolution.
            _width *= CardRenderRate;
            _height *= CardRenderRate;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        public static void post_CreatePngScreen(ref byte[] __result) => DownscaleEncoded(ref __result);
    }
}
