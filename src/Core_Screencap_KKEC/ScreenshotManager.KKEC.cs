using alphaShot;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepisPlugins;
using Illusion.Game;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
#if KK || KKS
using StrayTech;
#endif

namespace Screencap
{
    public partial class ScreenshotManager
    {
        #region Config properties

        public static ConfigEntry<KeyboardShortcut> KeyCapture360 { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCaptureAlphaIn3D { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCapture360in3D { get; private set; }

        public static ConfigEntry<int> Resolution360 { get; private set; }
        public static ConfigEntry<int> CardDownscalingRate { get; private set; }
        public static ConfigEntry<float> EyeSeparation { get; private set; }
        public static ConfigEntry<float> ImageSeparationOffset { get; private set; }
        public static ConfigEntry<bool> FlipEyesIn3DCapture { get; private set; }
        public static ConfigEntry<bool> UseJpg { get; private set; }
        public static ConfigEntry<int> JpgQuality { get; private set; }
        

        private void InitializeGameSpecific()
        {
            SceneManager.sceneLoaded += (s, a) => InstallSceenshotHandler();
            InstallSceenshotHandler();

            I360Render.Init();

            KeyCapture360 = Config.Bind(
                "Keyboard shortcuts",
                "Take 360 screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl),
                new ConfigDescription("Captures a 360 screenshot around current camera. The created image is in equirectangular format and can be viewed by most 360 image viewers (e.g. Google Cardboard)."));

            KeyCaptureAlphaIn3D = Config.Bind(
                "Keyboard shortcuts", "Take rendered 3D screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftAlt),
                new ConfigDescription("Capture a high quality screenshot without UI in stereoscopic 3D (2 captures for each eye in one image). These images can be viewed by crossing your eyes or any stereoscopic image viewer."));

            KeyCapture360in3D = Config.Bind(
                "Keyboard shortcuts", "Take 360 3D screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl, KeyCode.LeftShift),
                new ConfigDescription("Captures a 360 screenshot around current camera in stereoscopic 3D (2 captures for each eye in one image). These images can be viewed by image viewers supporting 3D stereo format (e.g. VR Media Player - 360° Viewer)."));

            Resolution360 = Config.Bind(
                "360 Screenshots", "360 screenshot resolution",
                4096,
                new ConfigDescription("Horizontal resolution (width) of 360 degree/panorama screenshots. Decrease if you have issues. WARNING: Memory usage can get VERY high - 4096 needs around 4GB of free RAM/VRAM to create, 8192 will need much more.", new AcceptableValueList<int>(1024, 2048, 4096, 8192)));

            CardDownscalingRate = Config.Bind(
                "Render Settings", "Card image upsampling ratio",
                3,
                new ConfigDescription("Capture character card images in a higher resolution and then downscale them to desired size. Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.", new AcceptableValueRange<int>(1, 4)));

            EyeSeparation = Config.Bind(
                "3D Settings", "3D screenshot eye separation",
                0.18f,
                new ConfigDescription("Distance between the two captured stereoscopic screenshots in arbitrary units.", new AcceptableValueRange<float>(0.01f, 0.5f)));

            ImageSeparationOffset = Config.Bind(
                "3D Settings", "3D screenshot image separation offset",
                0.25f,
                new ConfigDescription("Move images in stereoscopic screenshots closer together by this percentage (discards overlapping parts). Useful for viewing with crossed eyes. Does not affect 360 stereoscopic screenshots.", new AcceptableValueRange<float>(0f, 1f)));

            FlipEyesIn3DCapture = Config.Bind(
                "3D Settings", "Flip left and right eye",
                true,
                new ConfigDescription("Flip left and right eye for cross-eyed viewing. Disable to use the screenshots in most VR image viewers."));

            UseJpg = Config.Bind(
                "JPG Settings", "Save screenshots as .jpg instead of .png",
                false,
                new ConfigDescription("Save screenshots in lower quality in return for smaller file sizes. Transparency is NOT supported in .jpg screenshots. Strongly consider not using this option if you want to share your work."));

            JpgQuality = Config.Bind(
                "3D Settings", "Quality of .jpg files",
                100,
                new ConfigDescription("Lower quality = lower file sizes. Even 100 is worse than a .png file.", new AcceptableValueRange<int>(1, 100)));
        }

        #endregion
        
        internal AlphaShot2 currentAlphaShot;
        private void InstallSceenshotHandler()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            currentAlphaShot = Camera.main.gameObject.GetOrAddComponent<AlphaShot2>();
        }

        private static byte[] EncodeToFile(Texture2D result) => UseJpg.Value ? result.EncodeToJPG(JpgQuality.Value) : result.EncodeToPNG();

        private static byte[] EncodeToXmpFile(Texture2D result) => UseJpg.Value ? I360Render.InsertXMPIntoTexture2D_JPEG(result, JpgQuality.Value) : I360Render.InsertXMPIntoTexture2D_PNG(result);

        protected void Update()
        {
            if (KeyGui.Value.IsDown())
            {
                uiShow = !uiShow;
                ResolutionXBuffer = ResolutionX.Value.ToString();
                ResolutionYBuffer = ResolutionY.Value.ToString();
            }
            else if (KeyCaptureAlpha.Value.IsDown()) StartCoroutine(TakeCharScreenshot(false));
            else if (KeyCapture.Value.IsDown()) TakeScreenshot();
            else if (KeyCapture360.Value.IsDown()) StartCoroutine(Take360Screenshot(false));
            else if (KeyCaptureAlphaIn3D.Value.IsDown()) StartCoroutine(TakeCharScreenshot(true));
            else if (KeyCapture360in3D.Value.IsDown()) StartCoroutine(Take360Screenshot(true));
        }

        /// <summary>
        /// Capture the screen into a texture based on supplied arguments. Remember to destroy the texture when done with it.
        /// Can return null if there no 3D camera was found to take the picture with.
        /// </summary>
        /// <param name="width">Width of the resulting capture, after downscaling</param>
        /// <param name="height">Height of the resulting capture, after downscaling</param>
        /// <param name="downscaling">How much to oversize and then downscale. 1 for none.</param>
        /// <param name="transparent">Should the capture be transparent</param>
        public Texture2D Capture(int width, int height, int downscaling, bool transparent)
        {
            if (currentAlphaShot == null)
            {
                Logger.LogDebug("Capture - No camera found");
                return null;
            }

            try { OnPreCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }
            var capture = currentAlphaShot.CaptureTex(width, height, downscaling, transparent ? AlphaMode.rgAlpha : AlphaMode.None);
            try { OnPostCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            return capture;
        }

        private void TakeScreenshot()
        {
            var filename = GetUniqueFilename("UI");
#if KK
            Application.CaptureScreenshot(filename, UIShotUpscale.Value);
#else
            ScreenCapture.CaptureScreenshot(filename, UIShotUpscale.Value);
#endif

            StartCoroutine(TakeScreenshotLog(filename));
        }

        private IEnumerator TakeScreenshotLog(string filename)
        {
            yield return new WaitForEndOfFrame();
            PlayCaptureSound();
            Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"UI screenshot saved to {filename}");
        }

        private IEnumerator TakeCharScreenshot(bool in3D)
        {
            if (currentAlphaShot == null)
            {
                Logger.Log(LogLevel.Message, "Can't render a screenshot here, try UI screenshot instead");
                yield break;
            }

            try { OnPreCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

#if EC || KKS
            var colorMask = FindObjectOfType<CameraEffectorColorMask>();
            var colorMaskDisabled = false;
            if (colorMask && colorMask.Enabled)
            {
                colorMaskDisabled = true;
                colorMask.Enabled = false;
            }
#endif

            if (!in3D)
            {
                yield return new WaitForEndOfFrame();
                var capture = currentAlphaShot.CaptureTex(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlphaMode.Value);

                var filename = GetUniqueFilename("Render");
                File.WriteAllBytes(filename, EncodeToFile(capture));
                Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"Character screenshot saved to {filename}");

                Destroy(capture);
            }
            else
            {
                var targetTr = Camera.main.transform;

                ToggleCameraControllers(targetTr, false);
                Time.timeScale = 0.01f;
                yield return new WaitForEndOfFrame();

                targetTr.position += targetTr.right * EyeSeparation.Value / 2;
                // Let the game render at the new position
                yield return new WaitForEndOfFrame();
                var capture = currentAlphaShot.CaptureTex(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlphaMode.Value);

                targetTr.position -= targetTr.right * EyeSeparation.Value;
                yield return new WaitForEndOfFrame();
                var capture2 = currentAlphaShot.CaptureTex(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlphaMode.Value);

                targetTr.position += targetTr.right * EyeSeparation.Value / 2;

                ToggleCameraControllers(targetTr, true);
                Time.timeScale = 1;

                var result = FlipEyesIn3DCapture.Value ? StitchImages(capture, capture2, ImageSeparationOffset.Value) : StitchImages(capture2, capture, ImageSeparationOffset.Value);

                var filename = GetUniqueFilename("3D-Render");
                File.WriteAllBytes(filename, EncodeToFile(result));

                Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"3D Character screenshot saved to {filename}");

                Destroy(capture);
                Destroy(capture2);
                Destroy(result);
            }

#if EC || KKS
            if (colorMaskDisabled && colorMask) colorMask.Enabled = true;
#endif

            try { OnPostCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            PlayCaptureSound();
        }

        private IEnumerator Take360Screenshot(bool in3D)
        {
            try { OnPreCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            yield return new WaitForEndOfFrame();

            if (!in3D)
            {
                yield return new WaitForEndOfFrame();

                var output = I360Render.CaptureTex(Resolution360.Value);
                var capture = EncodeToXmpFile(output);

                var filename = GetUniqueFilename("360");
                File.WriteAllBytes(filename, capture);

                Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"360 screenshot saved to {filename}");

                Destroy(output);
            }
            else
            {
                var targetTr = Camera.main.transform;

                ToggleCameraControllers(targetTr, false);
                Time.timeScale = 0.01f;
                yield return new WaitForEndOfFrame();

                targetTr.position += targetTr.right * EyeSeparation.Value / 2;
                // Let the game render at the new position
                yield return new WaitForEndOfFrame();
                var capture = I360Render.CaptureTex(Resolution360.Value);

                targetTr.position -= targetTr.right * EyeSeparation.Value;
                yield return new WaitForEndOfFrame();
                var capture2 = I360Render.CaptureTex(Resolution360.Value);

                targetTr.position += targetTr.right * EyeSeparation.Value / 2;

                ToggleCameraControllers(targetTr, true);
                Time.timeScale = 1;

                // Overlap is useless for these so don't use
                var result = FlipEyesIn3DCapture.Value ? StitchImages(capture, capture2, 0) : StitchImages(capture2, capture, 0);

                var filename = GetUniqueFilename("3D-360");
                File.WriteAllBytes(filename, EncodeToXmpFile(result));

                Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"3D 360 screenshot saved to {filename}");

                Destroy(result);
                Destroy(capture);
                Destroy(capture2);
            }

            try { OnPostCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            PlayCaptureSound();
        }

        /// <summary>
        /// Need to disable camera controllers because they prevent changes to position
        /// </summary>
        private static void ToggleCameraControllers(Transform targetTr, bool enabled)
        {
#if KK || KKS
            foreach (var controllerType in new[] { typeof(Studio.CameraControl), typeof(BaseCameraControl_Ver2), typeof(BaseCameraControl) })
            {
                var cc = targetTr.GetComponent(controllerType);
                if (cc is MonoBehaviour mb)
                    mb.enabled = enabled;
            }

            var actionScene = GameObject.Find("ActionScene/CameraSystem");
            if (actionScene != null) actionScene.GetComponent<CameraSystem>().ShouldUpdate = enabled;
#endif
        }

        private static Texture2D StitchImages(Texture2D capture, Texture2D capture2, float overlapOffset)
        {
            var xAdjust = (int)(capture.width * overlapOffset);
            var result = new Texture2D((capture.width - xAdjust) * 2, capture.height, TextureFormat.ARGB32, false);

            int width = result.width / 2;
            int height = result.height;
            result.SetPixels(0, 0, width, height, capture.GetPixels(0, 0, width, height));
            result.SetPixels(width, 0, width, height, capture2.GetPixels(xAdjust, 0, width, height));

            result.Apply();
            return result;
        }
    }
}
