using alphaShot;
using BepInEx;
using BepInEx.Logging;
using BepisPlugins;
using Illusion.Game;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screencap
{
    [BepInPlugin(GUID: GUID, Name: "Screenshot Manager", Version: Version)]
    public class ScreenshotManager : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.screenshotmanager";
        public const string Version = Metadata.PluginsVersion;
        private const int ScreenshotSizeMax = 4096;
        private const int ScreenshotSizeMin = 2;

        public static ScreenshotManager Instance { get; private set; }

        private readonly string screenshotDir = Path.Combine(Paths.GameRootPath, "UserData\\cap\\");
        internal AlphaShot2 currentAlphaShot;

        #region Config properties

        [Description("Capture a simple \"as you see it\" screenshot of the game.\n" +
                     "Not affected by settings for rendered screenshots.")]
        public static SavedKeyboardShortcut CK_Capture { get; private set; }

        public static SavedKeyboardShortcut CK_CaptureAlpha { get; private set; }

        [Description("Captures a 360 screenshot around current camera. The created image is in equirectangular " +
                     "format and can be viewed by most 360 image viewers (e.g. Google Cardboard).")]
        public static SavedKeyboardShortcut CK_Capture360 { get; private set; }

        public static SavedKeyboardShortcut CK_Gui { get; private set; }

        [Category("Rendered screenshot output resolution")]
        [DisplayName("Horizontal (Width in px)")]
        [AcceptableValueRange(ScreenshotSizeMin, ScreenshotSizeMax, false)]
        public static ConfigWrapper<int> ResolutionX { get; private set; }

        [Category("Rendered screenshot output resolution")]
        [DisplayName("Vertical (Height in px)")]
        [AcceptableValueRange(ScreenshotSizeMin, ScreenshotSizeMax, false)]
        public static ConfigWrapper<int> ResolutionY { get; private set; }

        [DisplayName("360 screenshot resolution")]
        [Description("Horizontal resolution of 360 screenshots. Decrease if you have issues.\n\n" +
                     "WARNING: Memory usage can get VERY high - 4096 needs around 4GB of free RAM/VRAM to take, 8192 will need much more.")]
        [AcceptableValueList(new object[] { 1024, 2048, 4096, 8192 })]
        public static ConfigWrapper<int> Resolution360 { get; private set; }

        [DisplayName("Rendered screenshot upsampling ratio")]
        [Description("Capture screenshots in a higher resolution and then downscale them to desired size. " +
                     "Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.")]
        [AcceptableValueRange(1, 4, false)]
        public static ConfigWrapper<int> DownscalingRate { get; private set; }

        [DisplayName("Card image upsampling ratio")]
        [Description("Capture character card images in a higher resolution and then downscale them to desired size. " +
                     "Prevents aliasing, perserves small details and gives a smoother result, but takes longer to create.")]
        [AcceptableValueRange(1, 4, false)]
        public static ConfigWrapper<int> CardDownscalingRate { get; private set; }

        [DisplayName("Transparency in rendered screenshots")]
        [Description("Replaces background with transparency in rendered image. Works only if there are no 3D objects covering the " +
                     "background (e.g. the map). Works well in character creator and studio.")]
        public static ConfigWrapper<bool> CaptureAlpha { get; private set; }

        [DisplayName("Show messages on screen")]
        [Description("Whether screenshot messages will be displayed on screen. Messages will still be written to the log.")]
        public static ConfigWrapper<bool> ScreenshotMessage { get; private set; }

        #endregion

        private string GetUniqueFilename()
        {
            return Path.GetFullPath(Path.Combine(screenshotDir, $"Koikatsu-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png"));
        }

        protected void Awake()
        {
            if (Instance)
            {
                GameObject.DestroyImmediate(this);
                return;
            }
            Instance = this;
            CK_Capture = new SavedKeyboardShortcut("Take UI screenshot", this, new KeyboardShortcut(KeyCode.F9));
            CK_CaptureAlpha = new SavedKeyboardShortcut("Take rendered screenshot", this, new KeyboardShortcut(KeyCode.F11));
            CK_Capture360 = new SavedKeyboardShortcut("Take 360 screenshot", this, new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl));
            CK_Gui = new SavedKeyboardShortcut("Open settings window", this, new KeyboardShortcut(KeyCode.F11, KeyCode.LeftShift));

            ResolutionX = new ConfigWrapper<int>("resolution-x", this, Screen.width);
            ResolutionY = new ConfigWrapper<int>("resolution-y", this, Screen.height);
            Resolution360 = new ConfigWrapper<int>("resolution-360", this, 4096);

            ResolutionX.SettingChanged += (sender, args) => ResolutionXBuffer = ResolutionX.Value.ToString();
            ResolutionY.SettingChanged += (sender, args) => ResolutionYBuffer = ResolutionY.Value.ToString();

            DownscalingRate = new ConfigWrapper<int>("downscalerate", this, 2);
            CardDownscalingRate = new ConfigWrapper<int>("carddownscalerate", this, 3);
            CaptureAlpha = new ConfigWrapper<bool>("capturealpha", this, true);
            ScreenshotMessage = new ConfigWrapper<bool>("screenshotmessage", this, true);

            SceneManager.sceneLoaded += (s, a) => InstallSceenshotHandler();
            InstallSceenshotHandler();

            if (!Directory.Exists(screenshotDir))
                Directory.CreateDirectory(screenshotDir);

            Hooks.InstallHooks();

            I360Render.Init();
        }

        private void InstallSceenshotHandler()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            currentAlphaShot = Camera.main.gameObject.GetOrAddComponent<AlphaShot2>();
        }

        protected void Update()
        {
            if (CK_Gui.IsDown())
            {
                uiShow = !uiShow;
                ResolutionXBuffer = ResolutionX.Value.ToString();
                ResolutionYBuffer = ResolutionY.Value.ToString();
            }
            else if (CK_CaptureAlpha.IsDown()) StartCoroutine(TakeCharScreenshot());
            else if (CK_Capture.IsDown()) TakeScreenshot();
            else if (CK_Capture360.IsDown()) StartCoroutine(Take360Screenshot());
        }

        private void TakeScreenshot()
        {
            var filename = GetUniqueFilename();
            Application.CaptureScreenshot(filename);

            StartCoroutine(TakeScreenshotLog(filename));
        }

        private IEnumerator TakeScreenshotLog(string filename)
        {
            yield return new WaitForEndOfFrame();
            Utils.Sound.Play(SystemSE.photo);
            BepInEx.Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"UI screenshot saved to {filename}");
        }

        private IEnumerator TakeCharScreenshot()
        {
            yield return new WaitForEndOfFrame();

            if (currentAlphaShot != null)
            {
                var filename = GetUniqueFilename();
                File.WriteAllBytes(filename, currentAlphaShot.Capture(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlpha.Value));

                Utils.Sound.Play(SystemSE.photo);
                BepInEx.Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"Character screenshot saved to {filename}");
            }
            else
            {
                BepInEx.Logger.Log(LogLevel.Message, "Can't render a screenshot here, try UI screenshot instead");
            }
        }

        private IEnumerator Take360Screenshot()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                var filename = GetUniqueFilename();
                File.WriteAllBytes(filename, I360Render.Capture(Resolution360.Value, false));

                Utils.Sound.Play(SystemSE.photo);
                BepInEx.Logger.Log(ScreenshotMessage.Value ? LogLevel.Message : LogLevel.Info, $"360 screenshot saved to {filename}");
            }
            catch (Exception e)
            {
                BepInEx.Logger.Log(LogLevel.Message | LogLevel.Error, "Failed to take a 360 screenshot - " + e.Message);
                BepInEx.Logger.Log(LogLevel.Error, e.StackTrace);
            }
        }

        #region UI
        private readonly int uiWindowHash = GUID.GetHashCode();
        private Rect uiRect = new Rect(20, Screen.height / 2 - 150, 160, 223);
        private bool uiShow = false;
        private string ResolutionXBuffer = "", ResolutionYBuffer = "";

        protected void OnGUI()
        {
            if (uiShow)
                uiRect = GUILayout.Window(uiWindowHash, uiRect, WindowFunction, "Screenshot settings");
        }

        private void WindowFunction(int windowID)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Output resolution (W/H)", new GUIStyle
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    });

                GUILayout.BeginHorizontal();
                {
                    GUI.SetNextControlName("X");
                    ResolutionXBuffer = GUILayout.TextField(ResolutionXBuffer);

                    GUILayout.Label("x", new GUIStyle
                        {
                            alignment = TextAnchor.LowerCenter,
                            normal = new GUIStyleState
                            {
                                textColor = Color.white
                            }
                        }, GUILayout.ExpandWidth(false));

                    GUI.SetNextControlName("Y");
                    ResolutionYBuffer = GUILayout.TextField(ResolutionYBuffer);

                    var focused = GUI.GetNameOfFocusedControl();
                    if (focused != "X" && focused != "Y")
                    {
                        if (!int.TryParse(ResolutionXBuffer, out int x))
                            x = ResolutionX.Value;
                        if (!int.TryParse(ResolutionYBuffer, out int y))
                            y = ResolutionY.Value;
                        ResolutionXBuffer = (ResolutionX.Value = Mathf.Clamp(x, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                        ResolutionYBuffer = (ResolutionY.Value = Mathf.Clamp(y, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(2);

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
                    GUILayout.Label("Screen upsampling rate", new GUIStyle
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = new GUIStyleState
                            {
                                textColor = Color.white
                            }
                        });

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
                    GUILayout.Label("Card upsampling rate", new GUIStyle
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = new GUIStyleState
                            {
                                textColor = Color.white
                            }
                        });

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

                CaptureAlpha.Value = GUILayout.Toggle(CaptureAlpha.Value, "Transparent background");

                if (GUILayout.Button("Open screenshot dir"))
                    Process.Start(screenshotDir);

                GUI.DragWindow();
            }
            #endregion
        }
    }
}
