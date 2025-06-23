using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepisPlugins;
using Shared;
using UnityEngine;

namespace Screencap
{
    /// <summary>
    /// Plugin for taking high quality screenshots with optional transparency.
    /// Provides features like:
    /// - Custom resolution screenshots
    /// - Transparency support
    /// - Upsampling for higher quality
    /// - Guide lines for composition
    /// - Saved resolution presets
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ScreenshotManager : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static ScreenshotManager Instance;

        #region Public API

        /// <summary>
        /// GUID of the plugin, use with BepInDependency
        /// </summary>
        public const string GUID = "com.bepis.bepinex.screenshotmanager";
        /// <summary>
        /// Display name of the plugin
        /// </summary>
        public const string PluginName = "Screenshot Manager";
        /// <summary>
        /// Version of the plugin, use with BepInDependency
        /// </summary>
        public const string Version = Metadata.PluginsVersion;

        /// <summary>
        /// Directory where screenshots are saved.
        /// If the default directory is being overridden, it must already exist.
        /// </summary>
        public static string ScreenshotDir { get; set; } = Path.Combine(Paths.GameRootPath, @"UserData\cap\");

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;
        /// <summary>
        /// Triggers the pre-capture event, allowing other plugins to perform actions before a screenshot is taken.
        /// Usually used to disable effects that might interfere with the screenshot capture.
        /// Call this before any <see cref="CaptureRender"/> or <see cref="Capture360"/> calls.
        /// Always remember to call <see cref="FirePostCapture"/> after the capture is done to restore any changes made by plugins.
        /// </summary>
        public static void FirePreCapture()
        {
            if (OnPreCapture != null)
            {
                try { OnPreCapture.Invoke(); }
                catch (Exception ex) { Logger.LogError(ex); }
            }
        }
        /// <summary>
        /// Triggers the post-capture event, allowing other plugins to perform actions after a screenshot is taken.
        /// Usually used to re-enable effects that were disabled before the screenshot capture.
        /// If you are doing multiple captures in a row, you can wait with this until all captures are done instead of calling Pre/Post for each individual capture.
        /// </summary>
        public static void FirePostCapture()
        {
            if (OnPostCapture != null)
            {
                try { OnPostCapture.Invoke(); }
                catch (Exception ex) { Logger.LogError(ex); }
            }
        }

        /// <summary>
        /// Capture the screen in a simple UI screenshot (exactly as the player can see it) and write it to file.
        /// </summary>
        /// <param name="filename">Where to save the screenshot</param>
        /// <param name="superSize">Multiplies the UI screenshot resolution from the current game resolution by this amount.
        /// Warning: Some elements will still be rendered at the original resolution (most notably the interface).</param>
        public static void CaptureUI(string filename, int? superSize = null)
        {
#if KK
            Application.CaptureScreenshot(filename, superSize ?? UIShotUpscale.Value);
#else
            ScreenCapture.CaptureScreenshot(filename, superSize ?? UIShotUpscale.Value);
#endif
        }

        /// <summary>
        /// Capture the screen into a texture based on supplied arguments. By default values from current config are used.
        /// You have to pass the output texture to RenderTexture.ReleaseTemporary after you are done with it.
        /// Remember to call <see cref="FirePreCapture"/> and <see cref="FirePostCapture"/> before and after the capture (can do multiple captures in-between).
        /// </summary>
        /// <param name="width">Width of the resulting capture, after downscaling</param>
        /// <param name="height">Height of the resulting capture, after downscaling</param>
        /// <param name="downscaling">How much to oversize and then downscale. 1 for none.</param>
        /// <param name="transparencyMode">Should the capture be transparent</param>
        /// <returns>A RenderTexture containing the captured screenshot. Returns null if the capture fails.</returns>
        public static RenderTexture CaptureRender(int? width = null, int? height = null, int? downscaling = null, AlphaMode? transparencyMode = null)
        {
            try
            {
                return Instance.DoCaptureRender(width ?? ResolutionX.Value, height ?? ResolutionY.Value, downscaling ?? DownscalingRate.Value, transparencyMode ?? CaptureAlphaMode.Value);
            }
            catch (Exception ex)
            {
                if (ScreenshotMessage.Value)
                    Logger.LogMessage("Render capture failed: " + ex.Message);

                Logger.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Captures a 360-degree screenshot around the current camera. By default values from current config are used.
        /// The created image is in equirectangular format and can be viewed by most 360 image viewers.
        /// You have to pass the output texture to RenderTexture.ReleaseTemporary after you are done with it.
        /// Remember to call <see cref="FirePreCapture"/> and <see cref="FirePostCapture"/> before and after the capture (can do multiple captures in-between).
        /// </summary>
        /// <param name="resolution">Optional resolution for the screenshot. Defaults to the configured 360 resolution.</param>
        /// <param name="faceCameraDirection">If true, the capture will face the camera's current direction.</param>
        /// <returns>A RenderTexture containing the captured 360-degree screenshot. Returns null if the capture fails.</returns>
        public static RenderTexture Capture360(int? resolution = null, bool faceCameraDirection = true)
        {
            try
            {
                return I360Render.CaptureTex(resolution ?? Resolution360.Value, faceCameraDirection: faceCameraDirection);
            }
            catch (Exception ex)
            {
                if (ScreenshotMessage.Value)
                    Logger.LogMessage("360 capture failed: " + ex.Message);

                Logger.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Create a stereoscopic 3D screenshot by taking two separate screenshots for each eye. By default values from current config are used.
        /// You have to pass the output texture to RenderTexture.ReleaseTemporary after you are done with it.
        /// Remember to call <see cref="FirePreCapture"/> and <see cref="FirePostCapture"/> before and after the capture (can do multiple captures in-between).
        /// </summary>
        /// <param name="captureAction">A function that performs the actual screenshot capture and returns a RenderTexture.</param>
        /// <param name="eyeSeparation">Optional override for the distance between the two eyes. Defaults to the configured value.</param>
        /// <param name="flipEyes">Optional override to flip the left and right eye images. Defaults to the configured value.</param>
        /// <param name="overlapOffset">Optional override for the overlap offset between the two images. Defaults to the configured value. Should be 0 for 360 screenshots.</param>
        /// <returns>A RenderTexture containing the combined stereoscopic 3D image. Returns null if the capture fails.</returns>
        public static RenderTexture Do3DCapture(Func<RenderTexture> captureAction, float? eyeSeparation = null, bool? flipEyes = null, float? overlapOffset = null)
        {
            RenderTexture capture = null;
            RenderTexture capture2 = null;
            RenderTexture result = null;

            try
            {
                var eyeSeparationValue = eyeSeparation ?? EyeSeparation.Value;
                var flipEyesValue = flipEyes ?? FlipEyesIn3DCapture.Value;
                var overlapOffsetValue = overlapOffset ?? ImageSeparationOffset.Value;

                var targetTr = Camera.main.transform;
                var origPos = targetTr.position;

                // Move the camera to simulate the left eye and capture the image
                targetTr.position += targetTr.right * eyeSeparationValue / 2;
                capture = captureAction();
                if (capture == null) throw new Exception("Capture failed, see previous errors");

                // Move the camera to simulate the right eye and capture the image
                targetTr.position -= targetTr.right * eyeSeparationValue;
                capture2 = captureAction();
                if (capture2 == null) throw new Exception("Capture failed, see previous errors");

                // Reset the camera position to its original state
                targetTr.position = origPos;

                // Combine the two images into a single stereoscopic image
                result = flipEyesValue ? BundleLoader.StitchImages(capture, capture2, overlapOffsetValue) : BundleLoader.StitchImages(capture2, capture, overlapOffsetValue);
            }
            catch (Exception e)
            {
                Logger.LogMessage("Failed to generate 3D screenshot: " + e.Message);
                Logger.LogError(e);
            }
            finally
            {
                // Release the temporary RenderTextures
                RenderTexture.ReleaseTemporary(capture);
                RenderTexture.ReleaseTemporary(capture2);
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Maximum allowed screenshot resolution, depends on extreme resolution setting
        /// </summary>
        private static int ScreenshotSizeMax => ResolutionAllowExtreme?.Value == true ? 15360 : 4096;

        /// <summary>
        /// Minimum allowed screenshot resolution
        /// </summary>
        private const int ScreenshotSizeMin = 2;

        #region Config properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ConfigEntry<KeyboardShortcut> KeyCaptureUI { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCaptureRender { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyGui { get; private set; }
        public static ConfigEntry<int> ResolutionX { get; private set; }
        public static ConfigEntry<int> ResolutionY { get; private set; }
        public static ConfigEntry<bool> ResolutionAllowExtreme { get; private set; }
        public static ConfigEntry<int> DownscalingRate { get; private set; }
        public static ConfigEntry<AlphaMode> CaptureAlphaMode { get; private set; }
        public static ConfigEntry<int> UIShotUpscale { get; private set; }
        public static ConfigEntry<bool> ScreenshotMessage { get; private set; }
        public static ConfigEntry<CameraGuideLinesType> GuideLinesModes { get; private set; }
        public static ConfigEntry<int> GuideLineThickness { get; private set; }
        public static ConfigEntry<NameFormat> ScreenshotNameFormat { get; private set; }
        public static ConfigEntry<string> ScreenshotNameOverride { get; private set; }

        public static ConfigEntry<KeyboardShortcut> KeyCapture360 { get; private set; }
        public static ConfigEntry<int> Resolution360 { get; private set; }

        public static ConfigEntry<KeyboardShortcut> KeyCaptureAlphaIn3D { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCapture360in3D { get; private set; }
        public static ConfigEntry<float> EyeSeparation { get; private set; }
        public static ConfigEntry<float> ImageSeparationOffset { get; private set; }
        public static ConfigEntry<bool> FlipEyesIn3DCapture { get; private set; }

        public static ConfigEntry<bool> UseJpg { get; private set; }
        public static ConfigEntry<int> JpgQuality { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Initializes plugin settings and configuration options.
        /// Sets up all configurable parameters like resolution limits, hotkeys,
        /// and screenshot behavior options.
        /// </summary>
        private void InitializeCommon()
        {
            KeyCaptureUI = Config.Bind(
                "Keyboard shortcuts", "Take UI screenshot",
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription("Capture a simple \"as you see it\" screenshot of the game. Not affected by settings for rendered screenshots."));

            KeyCaptureRender = Config.Bind(
                "Keyboard shortcuts", "Take rendered screenshot",
                new KeyboardShortcut(KeyCode.F11),
                new ConfigDescription("Take a screenshot with no interface. Can be configured by other settings to increase quality and turn on transparency."));

            KeyGui = Config.Bind(
                "Keyboard shortcuts", "Open settings window",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftShift),
                new ConfigDescription("Open a quick access window with the most common settings."));

            DownscalingRate = Config.Bind(
                "Render Settings", "Screenshot upsampling ratio",
                2,
                new ConfigDescription("Capture screenshots in a higher resolution and then downscale them to desired size. Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.", new AcceptableValueRange<int>(1, 4)));

            ScreenshotMessage = Config.Bind(
                "General", "Show messages on screen",
                true,
                new ConfigDescription("Whether screenshot messages will be displayed on screen. Messages will still be written to the log."));

            // Must be initialized before ResolutionX and ResolutionY
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

            CaptureAlphaMode = Config.Bind(
                "Render Settings", "Transparency in rendered screenshots",
                AlphaModeUtils.Default,
                new ConfigDescription("Replaces background with transparency in rendered image. Works only if there are no 3D objects covering the background (e.g. the map). Works well in character creator and studio."));

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
                CameraGuideLinesType.Framing | CameraGuideLinesType.GridThirds,
                new ConfigDescription("Draws guide lines on the screen to help with framing rendered screenshots. The guide lines are not captured in the rendered screenshot.\nTo show the guide lines, open the quick access settings window.", null, "Advanced"));

            GuideLineThickness = Config.Bind(
                "General", "Guide lines thickness",
                1,
                new ConfigDescription("Thickness of the guide lines in pixels.", new AcceptableValueRange<int>(1, 5), "Advanced"));

            UIShotUpscale = Config.Bind(
                "UI Screenshots", "Screenshot resolution multiplier",
                1,
                new ConfigDescription("Multiplies the UI screenshot resolution from the current game resolution by this amount.\nWarning: Some elements will still be rendered at the original resolution (most notably the interface).", new AcceptableValueRange<int>(1, 8), "Advanced"));

            KeyCapture360 = Config.Bind(
                "Keyboard shortcuts", "Take 360 screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl),
                new ConfigDescription("Captures a 360 screenshot around current camera. The created image is in equirectangular format and can be viewed by most 360 image viewers (e.g. Google Cardboard)."));

            Resolution360 = Config.Bind(
                "360 Screenshots", "360 screenshot resolution",
                4096,
                new ConfigDescription("Horizontal resolution (width) of 360 degree/panorama screenshots. Decrease if you have issues. WARNING: Memory usage can get VERY high - 4096 needs around 4GB of free RAM/VRAM to create, 8192 will need much more.", new AcceptableValueList<int>(1024, 2048, 4096, 8192)));

            KeyCaptureAlphaIn3D = Config.Bind(
                "Keyboard shortcuts", "Take rendered 3D screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftAlt),
                new ConfigDescription("Capture a high quality screenshot without UI in stereoscopic 3D (2 captures for each eye in one image). These images can be viewed by crossing your eyes or any stereoscopic image viewer."));

            KeyCapture360in3D = Config.Bind(
                "Keyboard shortcuts", "Take 360 3D screenshot",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl, KeyCode.LeftShift),
                new ConfigDescription("Captures a 360 screenshot around current camera in stereoscopic 3D (2 captures for each eye in one image). These images can be viewed by image viewers supporting 3D stereo format (e.g. VR Media Player - 360° Viewer)."));

            EyeSeparation = Config.Bind(
                "3D Settings", "3D screenshot eye separation",
                0.18f,
                new ConfigDescription("Distance between the two captured stereoscopic screenshots in arbitrary units.", new AcceptableValueRange<float>(0.01f, 0.5f)));

            ImageSeparationOffset = Config.Bind(
                "3D Settings", "3D screenshot image separation offset",
                0.25f,
                new ConfigDescription("Move images in stereoscopic screenshots closer together by this percentage (discards overlapping parts). Useful for viewing with crossed eyes. Does not affect 360 stereoscopic screenshots.", new AcceptableValueRange<float>(0f, 0.9f)));

            FlipEyesIn3DCapture = Config.Bind(
                "3D Settings", "Flip left and right eye",
                true,
                new ConfigDescription("Flip left and right eye for cross-eyed viewing. Disable to use the screenshots in most VR image viewers."));

            UseJpg = Config.Bind(
                "JPG Settings", "Save screenshots as .jpg instead of .png",
                false,
                new ConfigDescription("Save screenshots in lower quality in return for smaller file sizes.\nWARNING: This is not supported by some screenshot types, they will save as png anyways. Transparency is NOT supported.\nStrongly consider not using this option if you want to share your work.", null, "Advanced"));

            JpgQuality = Config.Bind(
                "JPG Settings", "Quality of .jpg files",
                100,
                new ConfigDescription("Lower quality = lower file sizes. Even 100 is worse than a .png file.", new AcceptableValueRange<int>(1, 100), "Advanced"));

            SavedResolutionsConfig = Config.Bind(
                "General", "Saved Resolutions",
                string.Empty,
                new ConfigDescription("List of saved resolutions in JSON format.", null, "Advanced", new BrowsableAttribute(false)));
            SavedResolutionsConfig.SettingChanged += (sender, args) => LoadSavedResolutions();
            LoadSavedResolutions();
        }

        #endregion

        #region Resolution presets

        /// <summary>
        /// List of saved resolution presets
        /// </summary>
        private List<SavedResolution> _savedResolutions = new List<SavedResolution>();
        private ConfigEntry<string> SavedResolutionsConfig { get; set; }

        /// <summary>
        /// Loads previously saved screenshot resolution presets from config.
        /// Parses the saved string format "(width,height)" into Vector2Int values.
        /// </summary>
        private void LoadSavedResolutions()
        {
            if (!string.IsNullOrEmpty(SavedResolutionsConfig.Value))
            {
                _savedResolutions = new List<SavedResolution>();

                // Regex pattern to match (x,y) format
                Regex regex = new Regex(@"\((\-?\d+),(\-?\d+)\)");

                foreach (Match match in regex.Matches(SavedResolutionsConfig.Value))
                {
                    int x = int.Parse(match.Groups[1].Value);
                    int y = int.Parse(match.Groups[2].Value);
                    _savedResolutions.Add(new SavedResolution(x, y));
                }
            }
        }

        private void SaveSavedResolutions()
        {
            SavedResolutionsConfig.Value = "[" + string.Join(", ", _savedResolutions.Select(v => $"({v.Width},{v.Height})").ToArray()) + "]";
        }

        private void SaveCurrentResolution()
        {
            var resolution = new SavedResolution(ResolutionX.Value, ResolutionY.Value);
            if (!_savedResolutions.Contains(resolution))
            {
                _savedResolutions.Add(resolution);
                SaveSavedResolutions();
            }
        }

        private void DeleteResolution(SavedResolution resolution)
        {
            _savedResolutions.Remove(resolution);
            SaveSavedResolutions();
        }

        #endregion

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            InitializeCommon();
            InitializeGameSpecific();

            ResolutionX.SettingChanged += (sender, args) => _resolutionXBuffer = ResolutionX.Value.ToString();
            ResolutionY.SettingChanged += (sender, args) => _resolutionYBuffer = ResolutionY.Value.ToString();

            if (!Directory.Exists(ScreenshotDir))
                Directory.CreateDirectory(ScreenshotDir);

            Hooks.InstallHooks();
        }

        private void Update()
        {
            // Only allow a single type of screenshot in one frame
            if (KeyGui.Value.IsDown())
            {
                _uiShow = !_uiShow;
                _resolutionXBuffer = ResolutionX.Value.ToString();
                _resolutionYBuffer = ResolutionY.Value.ToString();
            }
            else if (KeyCaptureUI.Value.IsDown()) StartCoroutine(TakeUIScreenshot());
            else if (KeyCapture360.Value.IsDown()) StartCoroutine(Take360Screenshot(false));
            else if (KeyCapture360in3D.Value.IsDown()) StartCoroutine(Take360Screenshot(true));
            else if (KeyCaptureRender.Value.IsDown()) StartCoroutine(TakeRenderScreenshot(false));
            else if (KeyCaptureAlphaIn3D.Value.IsDown()) StartCoroutine(TakeRenderScreenshot(true));
        }

        #region Capture methods

        private static string GetUniqueFilename(string capType, bool useJpg)
        {
            var productName = !string.IsNullOrEmpty(ScreenshotNameOverride.Value)
                ? ScreenshotNameOverride.Value
                : Application.productName.Replace(" ", "");

            var extension = useJpg ? "jpg" : "png";

            // Legacy AI/HS2 filenames
            // $"AI_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
            // $"HS2_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
            string filename;
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

            return Path.GetFullPath(Path.Combine(ScreenshotDir, filename));
        }

        private static IEnumerator TakeUIScreenshot()
        {
            // Do not call events, everything is supposed to be as seen on screen
            PlayCaptureSound();
            var filename = GetUniqueFilename("UI", false);
            CaptureUI(filename);
            // Prevent saving text from showing on screenshot
            yield return new WaitForEndOfFrame();
            LogScreenshotMessage("UI", filename);
        }
        private static IEnumerator Take360Screenshot(bool in3D)
        {
            FirePreCapture();

            PlayCaptureSound();
            var filename = GetUniqueFilename(in3D ? "3D-360" : "360", UseJpg.Value);
            LogScreenshotMessage(in3D ? "3D 360" : "360", filename);

            yield return new WaitForEndOfFrame();

            // Overlap offset is not useful in 360 captures, so force it to 0
            var output = !in3D ? Capture360() : Do3DCapture(() => Capture360(), overlapOffset: 0);

            FirePostCapture();

            if (output != null)
                yield return WriteToXmpFile(output, filename);
        }

        private static IEnumerator WriteToXmpFile(RenderTexture result, string filename)
        {
            // TODO This path is not async at all
            var t2d = result.CopyToTexture2D();
            RenderTexture.ReleaseTemporary(result);
            yield return null;

            var bytes = UseJpg.Value ? I360Render.InsertXMPIntoTexture2D_JPEG(t2d.EncodeToJPG(JpgQuality.Value), t2d.width, t2d.height) : I360Render.InsertXMPIntoTexture2D_PNG(t2d.EncodeToPNG(), t2d.width, t2d.height);
            DestroyImmediate(t2d);
            yield return null;
            File.WriteAllBytes(filename, bytes);
        }

        #endregion

        #region GUI

        private readonly int _uiWindowHash = GUID.GetHashCode();
        private Rect _uiRect = new Rect(20, Mathf.RoundToInt(Screen.height / 2f) - 250, 160, 500);
        private bool _uiShow;
        private string _resolutionXBuffer = "", _resolutionYBuffer = "";

        private void OnGUI()
        {
            if (_uiShow)
            {
                CameraGuideLines.DrawGuideLines(GuideLinesModes.Value, GuideLineThickness.Value, ResolutionX.Value, ResolutionY.Value);

                IMGUIUtils.DrawSolidBox(_uiRect);
                _uiRect = GUILayout.Window(_uiWindowHash, _uiRect, WindowFunction, "Screenshot settings");
                IMGUIUtils.EatInputInRect(_uiRect);
            }
        }

        /// <summary>
        /// Draws the screenshot settings GUI window.
        /// Includes controls for:
        /// - Resolution settings and presets
        /// - Upsampling options
        /// - Transparency toggle
        /// - Guide line settings
        /// - Screenshot capture buttons
        /// </summary>
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

            // Resolution settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Output resolution (W/H)", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("HD"))
                    {
                        ResolutionX.Value = 1280;
                        ResolutionY.Value = 720;
                    }
                    if (GUILayout.Button("FHD"))
                    {
                        ResolutionX.Value = 1920;
                        ResolutionY.Value = 1080;
                    }
                    if (GUILayout.Button("WQHD"))
                    {
                        ResolutionX.Value = 2560;
                        ResolutionY.Value = 1440;
                    }
                    if (GUILayout.Button("4K"))
                    {
                        ResolutionX.Value = 3840;
                        ResolutionY.Value = 2160;
                    }
                    if (ResolutionAllowExtreme.Value)
                    {
                        if (GUILayout.Button("5K"))
                        {
                            ResolutionX.Value = 5120;
                            ResolutionY.Value = 2880;
                        }
                        if (GUILayout.Button("8K"))
                        {
                            ResolutionX.Value = 7680;
                            ResolutionY.Value = 4320;
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUI.SetNextControlName("X");
                    _resolutionXBuffer = GUILayout.TextField(_resolutionXBuffer);

                    GUI.SetNextControlName("Y");
                    _resolutionYBuffer = GUILayout.TextField(_resolutionYBuffer);

                    var focused = GUI.GetNameOfFocusedControl();
                    // Update resolution values when:
                    // - Neither width nor height field is focused (user clicked away)
                    // - User pressed Enter/Return key
                    // Also clamps values to valid range and handles parsing errors
                    if (focused != "X" && focused != "Y" || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        if (!int.TryParse(_resolutionXBuffer, out int x))
                            x = ResolutionX.Value;
                        if (!int.TryParse(_resolutionYBuffer, out int y))
                            y = ResolutionY.Value;
                        _resolutionXBuffer = (ResolutionX.Value = Mathf.Clamp(x, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                        _resolutionYBuffer = (ResolutionY.Value = Mathf.Clamp(y, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                    }
                }
                GUILayout.EndHorizontal();

                // Common aspect ratio buttons
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
                    var currentX = ResolutionX.Value;
                    ResolutionX.Value = ResolutionY.Value;
                    ResolutionY.Value = currentX;
                }

                // Saved resolutions section
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    if (GUILayout.Button("Save current resolution"))
                        SaveCurrentResolution();

                    for (var i = 0; i < _savedResolutions.Count; i++)
                    {
                        var resolution = _savedResolutions[i];
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button($"{resolution.Width} x {resolution.Height}"))
                            {
                                ResolutionX.Value = resolution.Width;
                                ResolutionY.Value = resolution.Height;
                            }

                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                DeleteResolution(resolution);
                                _uiRect.height -= 30;
                                if (_savedResolutions.Count == 0) _uiRect.height -= 40;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            var slidebarTextStyle = new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                normal = new GUIStyleState { textColor = Color.white }
            };

            // Upsampling settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Screen upsampling rate", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    int downscale = (int)System.Math.Round(GUILayout.HorizontalSlider(DownscalingRate.Value, 1, 4));

                    GUILayout.Label($"{downscale}x", slidebarTextStyle, GUILayout.ExpandWidth(false));
                    DownscalingRate.Value = downscale;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(3);

                GUILayout.Label("Transparent background", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    for (var modeIdx = 0; modeIdx < AlphaModeUtils.AllModes.Length; modeIdx++)
                    {
                        GUI.changed = false;
                        var mode = (AlphaMode)modeIdx;
                        var val = GUILayout.Toggle(CaptureAlphaMode.Value == mode, AlphaModeUtils.AllModes[modeIdx]);
                        if (GUI.changed && val) CaptureAlphaMode.Value = mode;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Guide line settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Guide lines", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Thickness", slidebarTextStyle, GUILayout.ExpandWidth(false));
                    GUILayout.Space(2);
                    GuideLineThickness.Value = (int)System.Math.Round(GUILayout.HorizontalSlider(GuideLineThickness.Value, 1, 5));
                    GUILayout.Label($"{GuideLineThickness.Value}px", slidebarTextStyle, GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.Framing) != 0, "Frame");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.Framing : GuideLinesModes.Value & ~CameraGuideLinesType.Framing;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.Border) != 0, "Border");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.Border : GuideLinesModes.Value & ~CameraGuideLinesType.Border;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.CenterLines) != 0, "Center");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.CenterLines : GuideLinesModes.Value & ~CameraGuideLinesType.CenterLines;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.GridThirds) != 0, "3rds");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.GridThirds : GuideLinesModes.Value & ~CameraGuideLinesType.GridThirds;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.GridPhi) != 0, "Phi");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.GridPhi : GuideLinesModes.Value & ~CameraGuideLinesType.GridPhi;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.SideV) != 0, "SideV");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.SideV : GuideLinesModes.Value & ~CameraGuideLinesType.SideV;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesType.CrossOut) != 0, "X-out");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesType.CrossOut : GuideLinesModes.Value & ~CameraGuideLinesType.CrossOut;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Action buttons
            if (GUILayout.Button("Open screenshot dir"))
                Process.Start(ScreenshotDir);

            GUILayout.Label("Hotkeys (check plugin settings)");

            if (GUILayout.Button(new GUIContent("Capture Normal / UI", "Hotkey: " + KeyCaptureUI.Value)))
                StartCoroutine(TakeUIScreenshot());
            if (GUILayout.Button(new GUIContent("Capture Render", "Hotkey: " + KeyCaptureRender.Value)))
                StartCoroutine(TakeRenderScreenshot(false));
            if (GUILayout.Button(new GUIContent("Capture Render 3D", "Hotkey: " + KeyCaptureAlphaIn3D.Value)))
                StartCoroutine(TakeRenderScreenshot(true));
            if (GUILayout.Button(new GUIContent("Capture 360", "Hotkey: " + KeyCapture360.Value)))
                StartCoroutine(Take360Screenshot(false));
            if (GUILayout.Button(new GUIContent("Capture 360 3D", "Hotkey: " + KeyCapture360in3D.Value)))
                StartCoroutine(Take360Screenshot(true));

            GUI.DragWindow();
            IMGUIUtils.DrawTooltip(_uiRect, 170);
        }

        /// <summary>
        /// Logs a screenshot-related message to the game log.
        /// Uses message or info level based on user preferences.
        /// </summary>
        private static void LogScreenshotMessage(string kind, string filename)
        {
            if (filename.StartsWith(Paths.GameRootPath, StringComparison.OrdinalIgnoreCase))
                filename = filename.Substring(Paths.GameRootPath.Length);

            Logger.Log(ScreenshotMessage.Value ? BepInEx.Logging.LogLevel.Message : BepInEx.Logging.LogLevel.Info, $"Writing {kind} screenshot to {filename}");
        }

        private static void PlayCaptureSound()
        {
#if AI
            Singleton<Manager.Resources>.Instance.SoundPack.Play(AIProject.SoundPack.SystemSE.Photo);
#elif HS2
            if (Hooks.SoundWasPlayed)
                Hooks.SoundWasPlayed = false;
            else
                Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.photo);
#else
            Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.photo);
#endif
        }

        #endregion
    }
}
