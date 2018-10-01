using alphaShot;
using BepInEx;
using BepInEx.Logging;
using Illusion.Game;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screencap
{
    [BepInPlugin(GUID: GUID, Name: "Screenshot Manager", Version: "2.3")]
    public class ScreenshotManager : BaseUnityPlugin
    {
        internal const string GUID = "com.bepis.bepinex.screenshotmanager";

        private string screenshotDir = Path.Combine(Paths.GameRootPath, "UserData\\cap\\");
        private AlphaShot2 currentAlphaShot;

        #region Config properties

        [Description("Capture a simple \"as you see it\" screenshot of the game.\n" +
                     "Not affected by settings for rendered screenshots.")]
        public static SavedKeyboardShortcut CK_Capture { get; private set; }
        public static SavedKeyboardShortcut CK_CaptureAlpha { get; private set; }
        public static SavedKeyboardShortcut CK_Gui { get; private set; }

        [Category("Rendered screenshot output resolution")]
        [DisplayName("Horizontal (Width in px)")]
        [AcceptableValueRange(2, 4096, false)]
        public static ConfigWrapper<int> ResolutionX { get; private set; }

        [Category("Rendered screenshot output resolution")]
        [DisplayName("Vertical (Height in px)")]
        [AcceptableValueRange(2, 4096, false)]
        public static ConfigWrapper<int> ResolutionY { get; private set; }

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

        #endregion
        
        private string GetUniqueFilename()
        {
            return Path.GetFullPath(Path.Combine(screenshotDir, $"Koikatsu-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png"));
        }

        protected void Awake()
        {
            CK_Capture = new SavedKeyboardShortcut("Take UI screenshot", this, new KeyboardShortcut(KeyCode.F9));
            CK_CaptureAlpha = new SavedKeyboardShortcut("Take rendered screenshot", this, new KeyboardShortcut(KeyCode.F11));
            CK_Gui = new SavedKeyboardShortcut("Open settings window", this, new KeyboardShortcut(KeyCode.F11, KeyCode.LeftShift));

            ResolutionX = new ConfigWrapper<int>("resolution-x", this, Screen.width);
            ResolutionY = new ConfigWrapper<int>("resolution-y", this, Screen.height);
            DownscalingRate = new ConfigWrapper<int>("downscalerate", this, 2);
            CardDownscalingRate = new ConfigWrapper<int>("carddownscalerate", this, 2);
            CaptureAlpha = new ConfigWrapper<bool>("capturealpha", this, true);

            SceneManager.sceneLoaded += (s, a) => InstallSceenshotHandler();
            InstallSceenshotHandler();

            if (!Directory.Exists(screenshotDir))
                Directory.CreateDirectory(screenshotDir);

            Hooks.InstallHooks();
        }

        private void InstallSceenshotHandler()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            currentAlphaShot = Camera.main.gameObject.GetOrAddComponent<AlphaShot2>();
        }

        protected void Update()
        {
            if (CK_Gui.IsDown()) uiShow = !uiShow;
            else if (CK_CaptureAlpha.IsDown()) StartCoroutine(TakeCharScreenshot());
            else if (CK_Capture.IsDown()) TakeScreenshot();
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
            BepInEx.Logger.Log(LogLevel.Message, $"UI screenshot saved to {filename}");
        }

        private IEnumerator TakeCharScreenshot()
        {
            yield return new WaitForEndOfFrame();

            if (currentAlphaShot != null)
            {
                var filename = GetUniqueFilename();
                File.WriteAllBytes(filename, currentAlphaShot.Capture(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlpha.Value));

                Utils.Sound.Play(SystemSE.photo);
                BepInEx.Logger.Log(LogLevel.Message, $"Character screenshot saved to {filename}");
            }

            //GC.Collect();
        }

        #region UI
        private readonly int uiWindowHash = GUID.GetHashCode();
        private Rect uiRect = new Rect(20, 20, 160, 200);
        private bool uiShow = false;

        protected void OnGUI()
        {
            if (uiShow)
                uiRect = GUI.Window(uiWindowHash, uiRect, WindowFunction, "Rendering settings");
        }

        private void WindowFunction(int windowID)
        {
            GUI.Label(new Rect(0, 20, 160, 20), "Output resolution", new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });

            GUI.Label(new Rect(0, 40, 160, 20), "x", new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });

            string resX = GUI.TextField(new Rect(10, 40, 60, 20), ResolutionX.Value.ToString());

            string resY = GUI.TextField(new Rect(90, 40, 60, 20), ResolutionY.Value.ToString());

            bool screenSize = GUI.Button(new Rect(10, 65, 140, 20), "Set to screen size");


            GUI.Label(new Rect(0, 90, 160, 20), "Downscaling rate", new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });


            int downscale = (int)Math.Round(GUI.HorizontalSlider(new Rect(10, 113, 120, 20), DownscalingRate.Value, 1, 4));

            GUI.Label(new Rect(0, 110, 150, 20), $"{downscale}x", new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });


            GUI.Label(new Rect(0, 130, 160, 20), "Card downscaling rate", new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });


            int carddownscale = (int)Math.Round(GUI.HorizontalSlider(new Rect(10, 153, 120, 20), CardDownscalingRate.Value, 1, 4));

            GUI.Label(new Rect(0, 150, 150, 20), $"{carddownscale}x", new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });

            bool capturealpha = GUI.Toggle(new Rect(10, 173, 120, 20), CaptureAlpha.Value, "Capture alpha");


            if (GUI.changed)
            {
                BepInEx.Config.SaveOnConfigSet = false;

                if (int.TryParse(resX, out int x))
                    ResolutionX.Value = Mathf.Clamp(x, 2, 4096);

                if (int.TryParse(resY, out int y))
                    ResolutionY.Value = Mathf.Clamp(y, 2, 4096);

                if (screenSize)
                {
                    ResolutionX.Value = Screen.width;
                    ResolutionY.Value = Screen.height;
                }

                DownscalingRate.Value = downscale;

                CardDownscalingRate.Value = carddownscale;

                CaptureAlpha.Value = capturealpha;

                BepInEx.Config.SaveOnConfigSet = true;
                BepInEx.Config.SaveConfig();
            }

            GUI.DragWindow();
        }
        #endregion
    }
}
