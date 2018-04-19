using alphaShot;
using BepInEx;
using Illusion.Game;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screencap
{
    [BepInPlugin(GUID: "com.bepis.bepinex.screenshotmanager", Name: "Screenshot Manager", Version: "2.1")]
    public class ScreenshotManager : BaseUnityPlugin
    {
        private string screenshotDir = Path.Combine(Application.dataPath, "..\\UserData\\cap\\");
        private AlphaShot2 as2 = null;

        private KeyCode CK_Capture = KeyCode.F9;
        private KeyCode CK_CaptureAlpha = KeyCode.F11;

        #region Config properties

        private int ResolutionX
        {
            get => int.Parse(this.GetEntry("resolution-x", "1024"));
            set => this.SetEntry("resolution-x", value.ToString());
        }

        private int ResolutionY
        {
            get => int.Parse(this.GetEntry("resolution-y", "1024"));
            set => this.SetEntry("resolution-y", value.ToString());
        }

        private int DownscalingRate
        {
            get => int.Parse(this.GetEntry("downscalerate", "1"));
            set => this.SetEntry("downscalerate", value.ToString());
        }

        #endregion

        private string filename => Path.Combine(screenshotDir, $"Koikatsu-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.png");

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
            if(Input.GetKeyDown(CK_CaptureAlpha))
            {
                if (Input.GetKey(KeyCode.LeftShift)) showingUI = !showingUI;
                else TakeCharScreenshot();
            }
            if (Input.GetKeyDown(CK_Capture)) StartCoroutine(TakeScreenshot());
        }

        IEnumerator TakeScreenshot()
        {
            Application.CaptureScreenshot(filename);
            Utils.Sound.Play(SystemSE.photo);

            while (!File.Exists(filename))
                yield return new WaitForSeconds(0.01f);

            BepInLogger.Log($"Screenshot saved to {filename}", true);
        }

        void TakeCharScreenshot()
        {
            File.WriteAllBytes(filename, as2.Capture(ResolutionX, ResolutionY, DownscalingRate));

            Utils.Sound.Play(SystemSE.photo);
            BepInLogger.Log($"Character screenshot saved to {filename}", true);

            GC.Collect();
        }


        #region UI
        private Rect UI = new Rect(20, 20, 160, 140);
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


            int downscale = (int)Math.Round(GUI.HorizontalSlider(new Rect(10, 113, 120, 20), DownscalingRate, 1, 4));

            GUI.Label(new Rect(0, 110, 150, 20), $"{downscale}x", new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            });


            if (GUI.changed)
            {
                BepInEx.Config.SaveOnConfigSet = false;

                if (int.TryParse(resX, out int x))
                    ResolutionX = Mathf.Clamp(x, 2, 4096);

                if (int.TryParse(resY, out int y))
                    ResolutionY = Mathf.Clamp(y, 2, 4096);

                if (screenSize)
                {
                    ResolutionX = Screen.width;
                    ResolutionY = Screen.height;
                }

                DownscalingRate = downscale;

                BepInEx.Config.SaveOnConfigSet = true;
                BepInEx.Config.SaveConfig();
            }

            GUI.DragWindow();
        }
        #endregion
    }
}
