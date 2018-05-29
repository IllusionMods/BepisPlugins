using alphaShot;
using BepInEx;
using Illusion.Game;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screencap
{
    [BepInPlugin(GUID: "com.bepis.bepinex.screenshotmanager", Name: "Screenshot Manager", Version: "2.2")]
    public class ScreenshotManager : BaseUnityPlugin
    {
        private string screenshotDir = Path.Combine(Application.dataPath, "..\\UserData\\cap\\");
        private AlphaShot2 as2 = null;

        #region Config properties

        public ScreenshotManager()
        {
            CK_Capture = new SavedKeyboardShortcut("Take screenshot", this, new KeyboardShortcut(KeyCode.F9));
            CK_CaptureAlpha = new SavedKeyboardShortcut("Take character screenshot", this, new KeyboardShortcut(KeyCode.F11));
            CK_Gui = new SavedKeyboardShortcut("Open settings window", this, new KeyboardShortcut(KeyCode.F11, false, false, true));

            ResolutionX = new ConfigWrapper<int>("resolution-x", this, Screen.width);
            ResolutionY = new ConfigWrapper<int>("resolution-y", this, Screen.height);
            DownscalingRate = new ConfigWrapper<int>("downscalerate", this, 1);
            CardDownscalingRate = new ConfigWrapper<int>("carddownscalerate", this, 1);
            CaptureAlpha = new ConfigWrapper<bool>("capturealpha", this, true);
        }

        public SavedKeyboardShortcut CK_Capture { get; }
        public SavedKeyboardShortcut CK_CaptureAlpha { get; }
        public SavedKeyboardShortcut CK_Gui { get; }

        [Category("Output resolution")]
        [DisplayName("Horizontal (Width in px)")]
        [AcceptableValueRange(2, 4096, false)]
        public ConfigWrapper<int> ResolutionX { get; }

        [Category("Output resolution")]
        [DisplayName("Vertical (Height in px)")]
        [AcceptableValueRange(2, 4096, false)]
        public ConfigWrapper<int> ResolutionY { get; }

        [DisplayName("!Downscaling rate (x times)")]
        [AcceptableValueRange(1, 4, false)]
        public ConfigWrapper<int> DownscalingRate { get; }

        [DisplayName("Card downscaling rate (x times)")]
        [AcceptableValueRange(1, 4, false)]
        public ConfigWrapper<int> CardDownscalingRate { get; }

        [DisplayName("!Capture alpha")]
        public ConfigWrapper<bool> CaptureAlpha { get; }

        #endregion

        private string filename => Path.GetFullPath(Path.Combine(screenshotDir, $"Koikatsu-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png"));

        void Awake()
        {
            SceneManager.sceneLoaded += (s, a) => Install();
            Install();

            if (!Directory.Exists(screenshotDir))
                Directory.CreateDirectory(screenshotDir);

            Hooks.InstallHooks();
        }

        private void Install()
        {
            if (!Camera.main || !Camera.main.gameObject) return;
            as2 = Camera.main.gameObject.AddComponent<AlphaShot2>();
        }

        void Update()
        {
            if (CK_Gui.IsDown()) showingUI = !showingUI;
            else if (CK_CaptureAlpha.IsDown()) TakeCharScreenshot();
            else if (CK_Capture.IsDown()) StartCoroutine(TakeScreenshot());
        }

        IEnumerator TakeScreenshot()
        {
            Application.CaptureScreenshot(filename);
            Utils.Sound.Play(SystemSE.photo);

            yield return new WaitUntil(() => File.Exists(filename));

            BepInLogger.Log($"Screenshot saved to {filename}", true);
        }

        void TakeCharScreenshot()
        {
            if (as2 == null) return;

            File.WriteAllBytes(filename, as2.Capture(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, CaptureAlpha.Value));
            
            Utils.Sound.Play(SystemSE.photo);
            BepInLogger.Log($"Character screenshot saved to {filename}", true);

            GC.Collect();
        }


        #region UI
        private Rect UI = new Rect(20, 20, 160, 200);
        private bool showingUI = false;

        void OnGUI()
        {
            if (showingUI)
                UI = GUI.Window("com.bepis.bepinex.screenshotmanager".GetHashCode() + 0, UI, WindowFunction, "Rendering settings");
        }

        void WindowFunction(int windowID)
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

            string resX = GUI.TextField(new Rect(10, 40, 60, 20), ResolutionX.ToString());

            string resY = GUI.TextField(new Rect(90, 40, 60, 20), ResolutionY.ToString());

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
