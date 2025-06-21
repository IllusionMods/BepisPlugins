using System;
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
using UnityEngine.Rendering;

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
    public partial class ScreenshotManager : BaseUnityPlugin
    {
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

        internal static new ManualLogSource Logger;
        [Obsolete("Get the instance from Chainloader")]
        public static ScreenshotManager Instance { get; private set; }

        /// <summary>
        /// Maximum allowed screenshot resolution, depends on extreme resolution setting
        /// </summary>
        private int ScreenshotSizeMax => ResolutionAllowExtreme?.Value == true ? 15360 : 4096;

        /// <summary>
        /// Directory where screenshots are saved
        /// </summary>
        public string ScreenshotDir { get; } = Path.Combine(Paths.GameRootPath, @"UserData\cap\");

        /// <summary>
        /// Minimum allowed screenshot resolution
        /// </summary>
        private const int ScreenshotSizeMin = 2;

        #region API

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

        #endregion

        #region Config properties

        /// <summary>
        /// List of saved resolution presets
        /// </summary>
        private List<SavedResolution> _savedResolutions = new List<SavedResolution>();
        private ConfigEntry<string> SavedResolutionsConfig { get; set; }
        public static ConfigEntry<KeyboardShortcut> KeyCapture { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyCaptureAlpha { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyGui { get; private set; }
        public static ConfigEntry<int> ResolutionX { get; set; }
        public static ConfigEntry<int> ResolutionY { get; set; }
        public static ConfigEntry<bool> ResolutionAllowExtreme { get; set; }
        public static ConfigEntry<int> DownscalingRate { get; private set; }
        [Obsolete("Use CaptureAlphaMode")]
        public static ConfigEntry<bool> CaptureAlpha { get; private set; }
        public static ConfigEntry<AlphaMode> CaptureAlphaMode { get; private set; }
        public static ConfigEntry<int> UIShotUpscale { get; set; }
        public static ConfigEntry<bool> ScreenshotMessage { get; private set; }
        public static ConfigEntry<CameraGuideLinesMode> GuideLinesModes { get; set; }
        public static ConfigEntry<int> GuideLineThickness { get; set; }
        public static ConfigEntry<NameFormat> ScreenshotNameFormat { get; private set; }
        public static ConfigEntry<string> ScreenshotNameOverride { get; private set; }

        /// <summary>
        /// Initializes plugin settings and configuration options.
        /// Sets up all configurable parameters like resolution limits, hotkeys,
        /// and screenshot behavior options.
        /// </summary>
        private void InitializeCommon()
        {
            KeyCapture = Config.Bind(
                "Keyboard shortcuts", "Take UI screenshot",
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription("Capture a simple \"as you see it\" screenshot of the game. Not affected by settings for rendered screenshots."));

            KeyCaptureAlpha = Config.Bind(
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
                AlphaMode.Default,
                new ConfigDescription("Replaces background with transparency in rendered image. Works only if there are no 3D objects covering the background (e.g. the map). Works well in character creator and studio."));

            CaptureAlpha = Config.Bind("Obsolete", "Transparency in rendered screenshots", CaptureAlphaMode.Value != AlphaMode.None,
                                       new ConfigDescription("Only for backwards compatibility, use CaptureAlphaMode instead.", null, new BrowsableAttribute(false)));
            CaptureAlpha.SettingChanged += (sender, args) => CaptureAlphaMode.Value = CaptureAlpha.Value ? AlphaMode.Default : AlphaMode.None;
            CaptureAlphaMode.SettingChanged += (sender, args) => CaptureAlpha.Value = CaptureAlphaMode.Value != AlphaMode.None;

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

            GuideLineThickness = Config.Bind(
                "General", "Guide lines thickness",
                1,
                new ConfigDescription("Thickness of the guide lines in pixels.", new AcceptableValueRange<int>(1, 5), "Advanced"));

            UIShotUpscale = Config.Bind(
                "UI Screenshots", "Screenshot resolution multiplier",
                1,
                new ConfigDescription("Multiplies the UI screenshot resolution from the current game resolution by this amount.\nWarning: Some elements will still be rendered at the original resolution (most notably the interface).", new AcceptableValueRange<int>(1, 8), "Advanced"));

            SavedResolutionsConfig = Config.Bind(
                "Rendered screenshots", "Saved Resolutions",
                string.Empty,
                new ConfigDescription("List of saved resolutions in JSON format.", null, "Advanced"));
            SavedResolutionsConfig.SettingChanged += (sender, args) => LoadSavedResolutions();
            LoadSavedResolutions();
        }

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

            ResolutionX.SettingChanged += (sender, args) => ResolutionXBuffer = ResolutionX.Value.ToString();
            ResolutionY.SettingChanged += (sender, args) => ResolutionYBuffer = ResolutionY.Value.ToString();

            if (!Directory.Exists(ScreenshotDir))
                Directory.CreateDirectory(ScreenshotDir);

            Hooks.InstallHooks();
        }

        private string GetUniqueFilename(string capType)
        {
            string filename;

            // Replace needed for Koikatu Party to get ride of the space
            var productName = Application.productName.Replace(" ", "");
            if (!string.IsNullOrEmpty(ScreenshotNameOverride.Value))
                productName = ScreenshotNameOverride.Value;

#if AI || HS2
            var extension = "png";
#else
            var extension = UseJpg.Value ? "jpg" : "png";
#endif
            // Legacy AI/HS2 filenames
            // $"AI_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
            // $"HS2_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"

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


        #region GUI

        private readonly int uiWindowHash = GUID.GetHashCode();
        private Rect uiRect = new Rect(20, Screen.height / 2 - 150, 160, 223);
        private bool uiShow = false;
        private string ResolutionXBuffer = "", ResolutionYBuffer = "";

        /// <summary>
        /// Draws the screenshot settings GUI window.
        /// Includes controls for:
        /// - Resolution settings and presets
        /// - Upsampling options
        /// - Transparency toggle
        /// - Guide line settings
        /// - Screenshot capture buttons
        /// </summary>
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

        /// <summary>
        /// Draws composition guide lines on screen based on current settings.
        /// Supports rule of thirds, golden ratio, and framing guides.
        /// Adjusts for different aspect ratios between screen and target resolution.
        /// </summary>
        private void DrawGuideLines()
        {
            // Calculate aspect ratios for proper guide positioning
            var desiredAspect = ResolutionX.Value / (float)ResolutionY.Value;
            var screenAspect = Screen.width / (float)Screen.height;

            int viewportWidth;
            int viewportHeight;
            int offsetX;
            int offsetY;

            // Handle cases where screen is wider than target
            if (screenAspect > desiredAspect)
            {
                viewportWidth = Mathf.RoundToInt(Screen.height * desiredAspect);
                viewportHeight = Screen.height;
                offsetX = Mathf.RoundToInt((Screen.width - viewportWidth) / 2f);
                offsetY = 0;

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, offsetX, Screen.height));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - offsetX, 0, offsetX, Screen.height));
                }
            }
            else
            {
                viewportWidth = Screen.width;
                viewportHeight = Mathf.RoundToInt(Screen.width / desiredAspect);
                offsetX = 0;
                offsetY = Mathf.RoundToInt((Screen.height - viewportHeight) / 2f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, Screen.width, offsetY));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, Screen.height - offsetY, Screen.width, offsetY));
                }
            }

            // Draw composition guides
            if ((GuideLinesModes.Value & CameraGuideLinesMode.Border) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 1);

            if ((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0.3333333f);

            if ((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0.236f);

            if ((GuideLinesModes.Value & CameraGuideLinesMode.CrossOut) != 0)
                DrawCrossingGuides(offsetX, offsetY, viewportWidth, viewportHeight);

            if ((GuideLinesModes.Value & CameraGuideLinesMode.SideV) != 0)
                DrawSidevGuides(offsetX, offsetY, viewportWidth, viewportHeight);

            if ((GuideLinesModes.Value & CameraGuideLinesMode.CenterLines) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0);
        }
        /// <summary>
        /// Draws guide lines for composition based on specified ratios.
        /// Used for both rule of thirds (0.3333) and golden ratio (0.236) guides.
        /// </summary>
        /// <param name="offsetX">X offset from screen edge</param>
        /// <param name="offsetY">Y offset from screen edge</param>
        /// <param name="viewportWidth">Width of the visible area</param>
        /// <param name="viewportHeight">Height of the visible area</param>
        /// <param name="centerRatio">Ratio for guide placement (0.3333 for thirds, 0.236 for golden ratio)</param>
        private void DrawGridGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, float centerRatio)
        {
            // Calculate ratios for guide line placement:
            // For rule of thirds: centerRatio = 0.3333, resulting in 1/3 divisions
            // For golden ratio: centerRatio = 0.236, resulting in golden section divisions
            // sideRatio determines the position of the first line
            // secondRatio determines the position of the second line
            var sideRatio = (1 - centerRatio) / 2;
            var secondRatio = sideRatio + centerRatio;
            var halfThick = (int)(GuideLineThickness.Value / 2);

            // Calculate actual pixel positions for vertical guide lines
            var firstx = offsetX + Mathf.Max(viewportWidth * sideRatio - halfThick, 0);
            IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(firstx), offsetY, GuideLineThickness.Value, viewportHeight));
            if (centerRatio != 0)
            {
                var secondx = offsetX + Mathf.Min(viewportWidth * secondRatio - halfThick, viewportWidth - GuideLineThickness.Value);
                IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(secondx), offsetY, GuideLineThickness.Value, viewportHeight));
            }

            // Calculate actual pixel positions for horizontal guide lines
            var firsty = offsetY + Mathf.Max(viewportHeight * sideRatio - halfThick, 0);
            IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(firsty), viewportWidth, GuideLineThickness.Value));
            if (centerRatio != 0)
            {
                var secondy = offsetY + Mathf.Min(viewportHeight * secondRatio - halfThick, viewportHeight - GuideLineThickness.Value);
                IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(secondy), viewportWidth, GuideLineThickness.Value));
            }
        }

        private static Material _drawingMaterial;
        internal static Material DrawingMaterial
        {
            get
            {
                if (!_drawingMaterial)
                {
                    // Unity has a built-in shader that is useful for drawing
                    // simple colored things.
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    _drawingMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    // Turn on alpha blending
                    _drawingMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    _drawingMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    _drawingMaterial.SetInt("_Cull", (int)CullMode.Off);
                    _drawingMaterial.SetInt("_ZWrite", 0);
                    _drawingMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                }

                return _drawingMaterial;
            }
        }

        private void DrawCrossingGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight)
        {
            // Draw diagonal lines using GL
            GL.PushMatrix();
            DrawingMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(IMGUIUtils.TransparentBoxColor);

            // Diagonal from top-left to bottom-right
            var angle = Mathf.Atan2(viewportHeight, viewportWidth);
            var perpDist = GuideLineThickness.Value / 2f;
            var dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            var dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);
            
            GL.Vertex3(offsetX + viewportWidth + dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(offsetX + viewportWidth - dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(offsetX - dx, offsetY - dy, 0);
            GL.Vertex3(offsetX + dx, offsetY + dy, 0);

            // Diagonal from top-right to bottom-left
            GL.Vertex3(offsetX + viewportWidth + dx, offsetY - dy, 0);
            GL.Vertex3(offsetX + viewportWidth - dx, offsetY + dy, 0);
            GL.Vertex3(offsetX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(offsetX + dx, offsetY + viewportHeight - dy, 0);

            GL.End();
            GL.PopMatrix();
        }
        private void DrawSidevGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight)
        {
            // Draw V-shaped guides from sides
            GL.PushMatrix();
            DrawingMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(IMGUIUtils.TransparentBoxColor);

            // Lines from left side to right center
            var rightCenterX = offsetX + viewportWidth;
            var centerY = offsetY + viewportHeight / 2f;

            // Top left to right center
            var angle = Mathf.Atan2(centerY - offsetY, rightCenterX - offsetX);
            var perpDist = GuideLineThickness.Value / 2f;
            var dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            var dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(offsetX + dx, offsetY + dy, 0);
            GL.Vertex3(offsetX - dx, offsetY - dy, 0);
            GL.Vertex3(rightCenterX - dx, centerY - dy, 0);
            GL.Vertex3(rightCenterX + dx, centerY + dy, 0);

            // Bottom left to right center
            angle = Mathf.Atan2(offsetY + viewportHeight - centerY, rightCenterX - offsetX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(offsetX + dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(offsetX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(rightCenterX - dx, centerY + dy, 0);
            GL.Vertex3(rightCenterX + dx, centerY - dy, 0);

            // Lines from right side to left center
            var leftCenterX = offsetX;

            // Top right to left center
            angle = Mathf.Atan2(centerY - offsetY, offsetX - rightCenterX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(rightCenterX + dx, offsetY + dy, 0);
            GL.Vertex3(rightCenterX - dx, offsetY - dy, 0);
            GL.Vertex3(leftCenterX - dx, centerY - dy, 0);
            GL.Vertex3(leftCenterX + dx, centerY + dy, 0);

            // Bottom right to left center
            angle = Mathf.Atan2(offsetY + viewportHeight - centerY, offsetX - rightCenterX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(rightCenterX + dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(rightCenterX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(leftCenterX - dx, centerY + dy, 0);
            GL.Vertex3(leftCenterX + dx, centerY - dy, 0);

            GL.End();
            GL.PopMatrix();
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
                    ResolutionXBuffer = GUILayout.TextField(ResolutionXBuffer);

                    GUI.SetNextControlName("Y");
                    ResolutionYBuffer = GUILayout.TextField(ResolutionYBuffer);

                    var focused = GUI.GetNameOfFocusedControl();
                    // Update resolution values when:
                    // - Neither width nor height field is focused (user clicked away)
                    // - User pressed Enter/Return key
                    // Also clamps values to valid range and handles parsing errors
                    if (focused != "X" && focused != "Y" || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
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

                if (GUILayout.Button("Save current resolution"))
                {
                    SaveCurrentResolution();
                }
            }
            GUILayout.EndVertical();

            // Saved resolutions section
            if (_savedResolutions.Count > 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Saved Resolutions", titleStyle);
                    for (var i = 0; i < _savedResolutions.Count; i++)
                    {
                        var resolution = _savedResolutions[i];
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button($"{resolution.Width}x{resolution.Height}"))
                            {
                                ResolutionX.Value = resolution.Width;
                                ResolutionY.Value = resolution.Height;
                            }

                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                DeleteResolution(resolution);
                                uiRect.height -= 30;
                                if (_savedResolutions.Count == 0) uiRect.height -= 40;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }

            // Upsampling settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Screen upsampling rate", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    int downscale = (int)System.Math.Round(GUILayout.HorizontalSlider(DownscalingRate.Value, 1, 4));

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

#if KK || KKS || EC
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
#endif
            // Transparency settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Transparent background", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.None, "No");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.None;
#if AI || HS2                    //TODO more generic way?
                    GUI.changed = false;
                    val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.Default, "Yes");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.Default;
#else
                    GUI.changed = false;
                    val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.blackout, "Cutout");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.blackout;

                    GUI.changed = false;
                    val = GUILayout.Toggle(CaptureAlphaMode.Value == AlphaMode.rgAlpha, "Alpha");
                    if (GUI.changed && val) CaptureAlphaMode.Value = AlphaMode.rgAlpha;
#endif

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
                    GUILayout.Label("Thickness", GUILayout.ExpandWidth(false));
                    GUILayout.Space(2);
                    GuideLineThickness.Value = (int)System.Math.Round(GUILayout.HorizontalSlider(GuideLineThickness.Value, 1, 5));
                    GUILayout.Label($"{GuideLineThickness.Value}px", new GUIStyle
                    {
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    }, GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0, "Frame");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.Framing : GuideLinesModes.Value & ~CameraGuideLinesMode.Framing;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.Border) != 0, "Border");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.Border : GuideLinesModes.Value & ~CameraGuideLinesMode.Border;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0, "3rds");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridThirds : GuideLinesModes.Value & ~CameraGuideLinesMode.GridThirds;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0, "Phi");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridPhi : GuideLinesModes.Value & ~CameraGuideLinesMode.GridPhi;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.SideV) != 0, "SideV");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.SideV : GuideLinesModes.Value & ~CameraGuideLinesMode.SideV;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.CrossOut) != 0, "Crossout");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.CrossOut : GuideLinesModes.Value & ~CameraGuideLinesMode.CrossOut;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.CenterLines) != 0, "Center");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.CenterLines : GuideLinesModes.Value & ~CameraGuideLinesMode.CenterLines;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Action buttons
            if (GUILayout.Button("Open screenshot dir"))
                Process.Start(ScreenshotDir);

            GUILayout.Space(3);
            if (GUILayout.Button($"Capture Normal ({KeyCapture.Value})"))
#if AI || HS2
                CaptureScreenshotNormal();
#else
                TakeScreenshot();
#endif
            if (GUILayout.Button($"Capture Render ({KeyCaptureAlpha.Value})"))
#if AI || HS2
                CaptureScreenshotRender();
#else
                StartCoroutine(TakeCharScreenshot(true));
#endif

            GUILayout.Space(2);
            GUILayout.Label("More in Plugin Settings");

            GUI.DragWindow();
        }

        /// <summary>
        /// Logs a screenshot-related message to the game log.
        /// Uses message or info level based on user preferences.
        /// </summary>
        private static void LogScreenshotMessage(string text)
        {
            Logger.Log(ScreenshotMessage.Value ? BepInEx.Logging.LogLevel.Message : BepInEx.Logging.LogLevel.Info, text);
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