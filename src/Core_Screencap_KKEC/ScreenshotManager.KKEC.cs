using alphaShot;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pngcs.Unity;

namespace Screencap
{
    public partial class ScreenshotManager
    {
        #region Config properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ConfigEntry<int> CardDownscalingRate { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void InitializeGameSpecific()
        {
            SceneManager.sceneLoaded += (s, a) => InstallSceenshotHandler();
            InstallSceenshotHandler();

            CardDownscalingRate = Config.Bind(
                "Render Settings", "Card image upsampling ratio",
                3,
                new ConfigDescription("Capture character card images in a higher resolution and then downscale them to desired size. Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.", new AcceptableValueRange<int>(1, 4)));
        }

        #endregion

#pragma warning disable CS0618 // Type or member is obsolete
        private AlphaShot2 _currentAlphaShot;
        private void InstallSceenshotHandler()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            _currentAlphaShot = Camera.main.gameObject.GetOrAddComponent<AlphaShot2>();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        private RenderTexture DoCaptureRender(int width, int height, int downscaling, AlphaMode transparencyMode)
        {
            if (_currentAlphaShot == null)
            {
                Logger.LogWarning("Capture - No camera found");
                return null;
            }
            return _currentAlphaShot.CaptureTex(width, height, downscaling, transparencyMode);
        }

        private IEnumerator TakeRenderScreenshot(bool in3D)
        {
            if (_currentAlphaShot == null)
            {
                Logger.Log(LogLevel.Message, "Can't render a screenshot here, try UI screenshot instead");
                yield break;
            }

            FirePreCapture();

#if EC || KKS
            var colorMask = FindObjectOfType<CameraEffectorColorMask>();
            var colorMaskDisabled = false;
            if (colorMask && colorMask.Enabled)
            {
                colorMaskDisabled = true;
                colorMask.Enabled = false;
            }
#endif

            var filename = GetUniqueFilename(in3D ? "3D-Render" : "Render", UseJpg.Value);
            LogScreenshotMessage(in3D ? "3D rendered" : "rendered", filename);
            PlayCaptureSound();

            yield return new WaitForEndOfFrame();

            var output = !in3D ? CaptureRender() : Do3DCapture(() => CaptureRender());

#if EC || KKS
            if (colorMaskDisabled && colorMask) colorMask.Enabled = true;
#endif

            FirePostCapture();

            if (output != null)
                yield return WriteToFile(output, filename);
        }

        private static IEnumerator WriteToFile(RenderTexture result, string filename)
        {
            if (UseJpg.Value)
            {
                // TODO Not async
                var t2d = result.CopyToTexture2D();
                RenderTexture.ReleaseTemporary(result);
                yield return null;
                var encoded = t2d.EncodeToJPG(JpgQuality.Value);
                GameObject.DestroyImmediate(t2d);
                yield return null;
                File.WriteAllBytes(filename, encoded);
            }
            else
            {
                var width = result.width;
                var height = result.height;
#if KK || EC
                // Forced to use the slow method in KK, could possibly be avoided with https://github.com/yangrc1234/UnityOpenGLAsyncReadback
                var t2d = result.CopyToTexture2D();
                RenderTexture.ReleaseTemporary(result);
                yield return null;
                var px = t2d.GetPixels();
                GameObject.DestroyImmediate(t2d);
#else
                var req = UnityEngine.Rendering.AsyncGPUReadback.Request(result, 0, 0, result.width, 0, result.height, 0, 1, TextureFormat.RGBA32);
                do yield return null;
                while (!req.done);
                RenderTexture.ReleaseTemporary(result);
                Color32[] px;
                using (var buffer = req.GetData<Color32>())
                    px = buffer.ToArray();
#endif
                yield return PNG.WriteAsync(px, width, height, 8, true, false, filename);
            }
        }
    }
}
