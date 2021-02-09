using ChaCustom;
using HarmonyLib;
using UnityEngine;

namespace Screencap
{
    public static partial class Hooks
    {
        /// <summary> Chara card Render/Downsample rate.</summary>
        private static int CardRenderRate => ScreenshotManager.CardDownscalingRate.Value;

        public static void InstallHooks() => Harmony.CreateAndPatchAll(typeof(Hooks));
        /// <summary>
        /// Cancel the vanilla screenshot
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "Capture")]
        private static bool CapturePrefix() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        private static bool CreatePngPrefix(ref int createW, ref int createH)
        {
            //Multiply up render resolution.
            createW *= CardRenderRate;
            createH *= CardRenderRate;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        private static void CreatePngPostfix(ref byte[] pngData) => DownscaleEncoded(ref pngData);

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
