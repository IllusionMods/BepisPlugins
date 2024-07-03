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
    /// <summary>
    /// Plugin for taking high quality screenshots.
    /// </summary>
    public partial class ScreenshotManager
    {
        public const string GUID = "com.bepis.bepinex.screenshotmanager";
        public const string PluginName = "Screenshot Manager";
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;
        private static int ScreenshotSizeMax => ResolutionAllowExtreme.Value ? 15360 : 4096;
        private const int ScreenshotSizeMin = 2;

        public static ScreenshotManager Instance { get; private set; }

        private readonly string screenshotDir = Path.Combine(Paths.GameRootPath, @"UserData\cap\");
        internal AlphaShot2 currentAlphaShot;

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

        #region Config properties

        public static ConfigEntry<KeyboardShortcut> KeyCapture { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCaptureAlpha { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCapture360 { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyGui { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCaptureAlphaIn3D { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCapture360in3D { get; private set; }

        public static ConfigEntry<int> ResolutionX { get; private set; }
        public static ConfigEntry<int> ResolutionY { get; private set; }
        public static ConfigEntry<bool> ResolutionAllowExtreme { get; private set; }
        public static ConfigEntry<int> Resolution360 { get; private set; }
        public static ConfigEntry<int> DownscalingRate { get; private set; }
        public static ConfigEntry<int> CardDownscalingRate { get; private set; }
        public static ConfigEntry<bool> CaptureAlpha { get; private set; }
        public static ConfigEntry<AlphaMode> CaptureAlphaMode { get; private set; }
        public static ConfigEntry<bool> ScreenshotMessage { get; private set; }
        public static ConfigEntry<float> EyeSeparation { get; private set; }
        public static ConfigEntry<float> ImageSeparationOffset { get; private set; }
        public static ConfigEntry<bool> FlipEyesIn3DCapture { get; private set; }
        public static ConfigEntry<bool> UseJpg { get; private set; }
        public static ConfigEntry<int> JpgQuality { get; private set; }
        public static ConfigEntry<NameFormat> ScreenshotNameFormat { get; private set; }
        public static ConfigEntry<string> ScreenshotNameOverride { get; private set; }
        public static ConfigEntry<CameraGuideLinesMode> GuideLinesModes { get; private set; }
        public static ConfigEntry<int> UIShotUpscale { get; private set; }

        private void InitializeSettings()
        {
            KeyCapture = Config.Bind(
                "Keyboard shortcuts", "Take UI screenshot",
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription("Capture a simple \"as you see it\" screenshot of the game. Not affected by settings for rendered screenshots."));

            KeyCaptureAlpha = Config.Bind(
                "Keyboard shortcuts", "Take rendered screenshot",
                new KeyboardShortcut(KeyCode.F11),
                new ConfigDescription("Take a screenshot with no interface. Can be configured by other settings to increase quality and turn on transparency."));

            KeyCapture360 = Config.Bind(
                "Keyboard shortcuts",
                "Take 360 screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl),
                new ConfigDescription("Captures a 360 screenshot around current camera. The created image is in equirectangular format and can be viewed by most 360 image viewers (e.g. Google Cardboard)."));

            KeyGui = Config.Bind(
                "Keyboard shortcuts", "Open settings window",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftShift),
                new ConfigDescription("Open a quick access window with the most common settings."));

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

            DownscalingRate = Config.Bind(
                "Render Settings", "Screenshot upsampling ratio",
                2,
                new ConfigDescription("Capture screenshots in a higher resolution and then downscale them to desired size. Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.", new AcceptableValueRange<int>(1, 4)));

            CardDownscalingRate = Config.Bind(
                "Render Settings", "Card image upsampling ratio",
                3,
                new ConfigDescription("Capture character card images in a higher resolution and then downscale them to desired size. Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.", new AcceptableValueRange<int>(1, 4)));

            CaptureAlphaMode = Config.Bind(
                "Render Settings", "Transparency in rendered screenshots",
                AlphaMode.rgAlpha,
                new ConfigDescription("Replaces background with transparency in rendered image. Works only if there are no 3D objects covering the background (e.g. the map). Works well in character creator and studio."));

            CaptureAlpha = Config.Bind("Obsolete", "Transparency in rendered screenshots", CaptureAlphaMode.Value != AlphaMode.None,
                new ConfigDescription("Only for backwards compatibility, use CaptureAlphaMode instead.", null, new BrowsableAttribute(false)));
            CaptureAlpha.SettingChanged += (sender, args) => CaptureAlphaMode.Value = CaptureAlpha.Value ? AlphaMode.rgAlpha : AlphaMode.None;
            CaptureAlphaMode.SettingChanged += (sender, args) => CaptureAlpha.Value = CaptureAlphaMode.Value != AlphaMode.None;

            ScreenshotMessage = Config.Bind(
                "General", "Show messages on screen",
                true,
                new ConfigDescription("Whether screenshot messages will be displayed on screen. Messages will still be written to the log."));

            ResolutionAllowExtreme = Config.Bind(
                "Render Output Resolution", "Allow extreme resolutions",
                false,
                new ConfigDescription("Raise maximum rendered screenshot resolution cap to 16k. Trying to take a screenshot too high above 4k WILL CRASH YOUR GAME. ALWAYS SAVE BEFORE ATTEMPTING A SCREENSHOT AND MONITOR RAM USAGE AT ALL TIMES. Changes take effect after restarting the game."));

            ResolutionX = Config.Bind(
                "Render Output Resolution", "Horizontal",
                Screen.width,
                new ConfigDescription("Horizontal size (width) of rendered screenshots in pixels. Doesn't affect UI and 360 screenshots.", new AcceptableValueRange<int>(ScreenshotSizeMin, ScreenshotSizeMax)));

            ResolutionY = Config.Bind(
                "Render Output Resolution", "Vertical",
                Screen.height,
                new ConfigDescription("Vertical size (height) of rendered screenshots in pixels. Doesn't affect UI and 360 screenshots.", new AcceptableValueRange<int>(ScreenshotSizeMin, ScreenshotSizeMax)));

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

            ScreenshotNameFormat = Config.Bind(
                "General", "Screenshot filename format",
                NameFormat.NameDateType,
                new ConfigDescription("Screenshots will be saved with names of the selected format. Name stands for the current game name (CharaStudio, Koikatu, etc.)"));

            ScreenshotNameOverride = Config.Bind(
                "General", "Screenshot filename Name override",
                "",
                new ConfigDescription("Forces the Name part of the filename to always be this instead of varying depending on the name of the current game. Use \"Koikatsu\" to get the old filename behaviour.", null, "Advanced"));

            GuideLinesModes = Config.Bind(
                "General", "Camera guide lines",
                CameraGuideLinesMode.Framing | CameraGuideLinesMode.GridThirds,
                new ConfigDescription("Draws guide lines on the screen to help with framing rendered screenshots. The guide lines are not captured in the rendered screenshot.\nTo show the guide lines, open the quick access settings window.", null, "Advanced"));

            UIShotUpscale = Config.Bind(
                "UI Screenshots", "Screenshot resolution multiplier",
                1,
                new ConfigDescription("Multiplies the UI screenshot resolution from the current game resolution by this amount.\nWarning: Some elements will still be rendered at the original resolution (most notably the interface).", new AcceptableValueRange<int>(1, 8), "Advanced"));
        }

        #endregion

        protected void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            InitializeSettings();

            ResolutionX.SettingChanged += (sender, args) => ResolutionXBuffer = ResolutionX.Value.ToString();
            ResolutionY.SettingChanged += (sender, args) => ResolutionYBuffer = ResolutionY.Value.ToString();

            SceneManager.sceneLoaded += (s, a) => InstallSceenshotHandler();
            InstallSceenshotHandler();

            if (!Directory.Exists(screenshotDir))
                Directory.CreateDirectory(screenshotDir);

            Hooks.InstallHooks();

            I360Render.Init();
        }

        private string GetUniqueFilename(string capType)
        {
            string filename;

            // Replace needed for Koikatu Party to get ride of the space
            var productName = Application.productName.Replace(" ", "");
            if (!string.IsNullOrEmpty(ScreenshotNameOverride.Value))
                productName = ScreenshotNameOverride.Value;

            var extension = UseJpg.Value ? "jpg" : "png";

            switch (ScreenshotNameFormat.Value)
            {
                case NameFormat.NameDate:
                    filename = $"{productName}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{extension}";
                    break;
                case NameFormat.NameTypeDate:
                    filename = $"{productName}-{capType}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{extension}";
                    break;
                case NameFormat.NameDateType:
                    filename = $"{productName}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-{capType}.{extension}";
                    break;
                case NameFormat.TypeDate:
                    filename = $"{capType}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{extension}";
                    break;
                case NameFormat.TypeNameDate:
                    filename = $"{capType}-{productName}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{extension}";
                    break;
                case NameFormat.Date:
                    filename = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{extension}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unhandled screenshot filename format - " + ScreenshotNameFormat.Value);
            }

            return Path.GetFullPath(Path.Combine(screenshotDir, filename));
        }

        private static byte[] EncodeToFile(Texture2D result) => UseJpg.Value ? result.EncodeToJPG(JpgQuality.Value) : result.EncodeToPNG();

        private static byte[] EncodeToXmpFile(Texture2D result) => UseJpg.Value ? I360Render.InsertXMPIntoTexture2D_JPEG(result, JpgQuality.Value) : I360Render.InsertXMPIntoTexture2D_PNG(result);

        private void InstallSceenshotHandler()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            currentAlphaShot = Camera.main.gameObject.GetOrAddComponent<AlphaShot2>();
        }

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
            Utils.Sound.Play(SystemSE.photo);
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

            Utils.Sound.Play(SystemSE.photo);
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

            Utils.Sound.Play(SystemSE.photo);
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

        #region UI
        private readonly int uiWindowHash = GUID.GetHashCode();
        private Rect uiRect = new Rect(20, Screen.height / 2 - 150, 160, 223);
        private bool uiShow = false;
        private string ResolutionXBuffer = "", ResolutionYBuffer = "";

        protected void OnGUI()
        {
            if (uiShow)
            {
                DrawGuideLines();

                IMGUIUtils.DrawSolidBox(uiRect);
                uiRect = GUILayout.Window(uiWindowHash, uiRect, WindowFunction, "Screenshot settings");
                IMGUIUtils.EatInputInRect(uiRect);
            }
        }

        private void WindowFunction(int windowID)
        {
            var titleStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            };

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Output resolution (W/H)", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    GUI.SetNextControlName("X");
                    ResolutionXBuffer = GUILayout.TextField(ResolutionXBuffer);

                    GUILayout.Label("x", titleStyle, GUILayout.ExpandWidth(false), GUILayout.MinHeight(26));

                    GUI.SetNextControlName("Y");
                    ResolutionYBuffer = GUILayout.TextField(ResolutionYBuffer);

                    var focused = GUI.GetNameOfFocusedControl();
                    if (focused != "X" && focused != "Y" || Event.current.keyCode == KeyCode.Return)
                    {
                        if (!int.TryParse(ResolutionXBuffer, out int x))
                            x = ResolutionX.Value;
                        if (!int.TryParse(ResolutionYBuffer, out int y))
                            y = ResolutionY.Value;
                        ResolutionXBuffer = (ResolutionX.Value = Mathf.Clamp(x, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                        ResolutionYBuffer = (ResolutionY.Value = Mathf.Clamp(y, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("1:1"))
                    {
                        var max = Mathf.Max(ResolutionX.Value, ResolutionY.Value);
                        ResolutionX.Value = max;
                        ResolutionY.Value = max;
                    }
                    if (GUILayout.Button("4:3"))
                    {
                        var max = Mathf.Max(ResolutionX.Value, ResolutionY.Value);
                        ResolutionX.Value = max;
                        ResolutionY.Value = Mathf.RoundToInt(max * (3f / 4f));
                    }
                    if (GUILayout.Button("16:9"))
                    {
                        var max = Mathf.Max(ResolutionX.Value, ResolutionY.Value);
                        ResolutionX.Value = max;
                        ResolutionY.Value = Mathf.RoundToInt(max * (9f / 16f));
                    }
                    if (GUILayout.Button("6:10"))
                    {
                        var max = Mathf.Max(ResolutionX.Value, ResolutionY.Value);
                        ResolutionX.Value = Mathf.RoundToInt(max * (6f / 10f));
                        ResolutionY.Value = max;
                    }
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Set to screen size"))
                {
                    ResolutionX.Value = Screen.width;
                    ResolutionY.Value = Screen.height;
                }

                if (GUILayout.Button("Rotate 90 degrees"))
                {
                    var curerntX = ResolutionX.Value;
                    ResolutionX.Value = ResolutionY.Value;
                    ResolutionY.Value = curerntX;
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Screen upsampling rate", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    int downscale = (int)Math.Round(GUILayout.HorizontalSlider(DownscalingRate.Value, 1, 4));

                    GUILayout.Label($"{downscale}x", new GUIStyle
                    {
                        alignment = TextAnchor.UpperRight,
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    }, GUILayout.ExpandWidth(false));
                    DownscalingRate.Value = downscale;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Card upsampling rate", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    int carddownscale = (int)Math.Round(GUILayout.HorizontalSlider(CardDownscalingRate.Value, 1, 4));

                    GUILayout.Label($"{carddownscale}x", new GUIStyle
                    {
                        alignment = TextAnchor.UpperRight,
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    }, GUILayout.ExpandWidth(false));
                    CardDownscalingRate.Value = carddownscale;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Transparent background", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.None, "No");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.None;

                    GUI.changed = false;
                    val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.blackout, "Cutout");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.blackout;

                    GUI.changed = false;
                    val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.rgAlpha, "Alpha");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.rgAlpha;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Guide lines", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0, "Frame");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.Framing : GuideLinesModes.Value & ~CameraGuideLinesMode.Framing;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0, "3rds");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridThirds : GuideLinesModes.Value & ~CameraGuideLinesMode.GridThirds;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0, "Phi");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridPhi : GuideLinesModes.Value & ~CameraGuideLinesMode.GridPhi;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("Open screenshot dir"))
                Process.Start(screenshotDir);

            GUILayout.Space(2);
            GUILayout.Label("More in Plugin Settings");

            GUI.DragWindow();
        }

        private static void DrawGuideLines()
        {
            var desiredAspect = ResolutionX.Value / (float)ResolutionY.Value;
            var screenAspect = Screen.width / (float)Screen.height;

            if (screenAspect > desiredAspect)
            {
                var actualWidth = Mathf.RoundToInt(Screen.height * desiredAspect);
                var barWidth = Mathf.RoundToInt((Screen.width - actualWidth) / 2f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, barWidth, Screen.height));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - barWidth, 0, barWidth, Screen.height));
                }

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0)
                    DrawGuides(barWidth, 0, actualWidth, Screen.height, 0.3333333f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0)
                    DrawGuides(barWidth, 0, actualWidth, Screen.height, 0.236f);
            }
            else
            {
                var actualHeight = Mathf.RoundToInt(Screen.width / desiredAspect);
                var barHeight = Mathf.RoundToInt((Screen.height - actualHeight) / 2f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, Screen.width, barHeight));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, Screen.height - barHeight, Screen.width, barHeight));
                }

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0)
                    DrawGuides(0, barHeight, Screen.width, actualHeight, 0.3333333f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0)
                    DrawGuides(0, barHeight, Screen.width, actualHeight, 0.236f);
            }

            void DrawGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, float centerRatio)
            {
                var sideRatio = (1 - centerRatio) / 2;
                var secondRatio = sideRatio + centerRatio;

                var firstx = offsetX + viewportWidth * sideRatio;
                var secondx = offsetX + viewportWidth * secondRatio;
                IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(firstx), offsetY, 1, viewportHeight));
                IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(secondx), offsetY, 1, viewportHeight));

                var firsty = offsetY + viewportHeight * sideRatio;
                var secondy = offsetY + viewportHeight * secondRatio;
                IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(firsty), viewportWidth, 1));
                IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(secondy), viewportWidth, 1));
            }
        }

        [Flags]
        public enum CameraGuideLinesMode
        {
            [Description("No guide lines")]
            None = 0,
            [Description("Cropped area")]
            Framing = 1 << 0,
            [Description("Rule of thirds")]
            GridThirds = 1 << 1,
            [Description("Golden ratio")]
            GridPhi = 1 << 2,
        }
        #endregion
    }
}
