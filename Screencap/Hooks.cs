using ChaCustom;
using Harmony;
using UnityEngine;

namespace Screencap
{
    public static class Hooks
    {
        //Chara card Render/Downsample rate.
        private const int RenderRate = 3;

        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.screenshotmanager");
            harmony.PatchAll(typeof(Hooks));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "Capture")]
        public static bool CapturePreHook()
        {
            //cancel the vanilla screenshot
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        public static bool pre_CreatePng(ref int createW, ref int createH)
        {
            //Multiply up render resolution.
            createW *= RenderRate;
            createH *= RenderRate;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        public static void post_CreatePng(ref byte[] pngData)
        {
            //Texture buffer for fullres.
            var t2d = new Texture2D(2, 2);
            t2d.LoadImage(pngData);

            //New width/height after downsampling.
            var nw = t2d.width / RenderRate;
            var nh = t2d.height / RenderRate;

            //Downsample texture
            var pixels = ScaleUnityTexture.ScaleLanczos(t2d.GetPixels32(), t2d.width, nw, nh);
            GameObject.Destroy(t2d);

            //Load pixel data into a new texture, encode to PNG and overwrite original result.
            var np = new Texture2D(nw, nh);
            np.SetPixels32(pixels);
            pngData = np.EncodeToPNG();
            GameObject.Destroy(np);
        }
    }
}
