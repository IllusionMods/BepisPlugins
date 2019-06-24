using alphaShot;
using ChaCustom;
using Harmony;
using UnityEngine;

namespace Screencap
{
    public static class Hooks
    {
        //Chara card Render/Downsample rate.
        private static int CardRenderRate => ScreenshotManager.CardDownscalingRate.Value;

        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create(ScreenshotManager.GUID);
            harmony.PatchAll(typeof(Hooks));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "Capture")]
        public static bool CapturePreHook()
        {
            //cancel the vanilla screenshot
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), "Capture")]
        public static bool StudioCapturePreHook()
        {
            //cancel the vanilla screenshot
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        public static bool pre_CreatePng(ref int createW, ref int createH)
        {
            //Multiply up render resolution.
            createW *= CardRenderRate;
            createH *= CardRenderRate;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        public static void post_CreatePng(ref byte[] pngData) => DownscaleEncoded(ref pngData);

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

        private static void DownscaleEncoded(ref byte[] encoded)
        {
            if (CardRenderRate <= 1) return;

            //Texture buffer for fullres.
            var t2d = new Texture2D(2, 2);
            t2d.LoadImage(encoded);

            //New width/height after downsampling.
            var nw = t2d.width / CardRenderRate;
            var nh = t2d.height / CardRenderRate;

            //Downsample texture
            var result = ScreenshotManager.Instance.currentAlphaShot.LanczosTex(t2d, nw, nh);
            encoded = result.EncodeToPNG();
            Object.Destroy(result);
        }
    }
}
