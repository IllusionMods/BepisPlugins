using BepInEx;
using UnityEngine;
using System.Linq;
using BepInEx.Logging;
using Logger = BepInEx.Logger;

namespace DeveloperConsole
{
    [BepInPlugin(GUID: "com.bepis.bepinex.developerconsole", Name: "Developer Console", Version: "1.0.1")]
    public class DeveloperConsole : BaseUnityPlugin
    {
        private bool showingUI = false;
        private string TotalLog = "";
        private Rect UI = new Rect(20, 20, 400, 400);
        private Vector2 scrollPosition = Vector2.zero;

        private void Awake()
        {
            Logger.EntryLogged += (level, log) =>
            {
                string current = $"{TotalLog}\r\n{log}";
                if (current.Length > 3000)
                {
                    var trimmed = current.Remove(0, 500);

                    // Trim until the first newline to avoid partial line
                    var newlineHit = false;
                    trimmed = new string(trimmed.SkipWhile(x => !newlineHit && !(newlineHit = (x == '\n'))).ToArray());

                    current = "--LOG TRIMMED--\n" + trimmed;
                }
                TotalLog = current;

                scrollPosition = new Vector2(0, float.MaxValue);
            };
        }

        private void OnGUI()
        {
            if (showingUI)
                UI = GUILayout.Window("com.bepis.bepinex.developerconsole".GetHashCode() + 0, UI, WindowFunction, "Developer Console");
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F12))
            {
                showingUI = !showingUI;
            }
        }

        private void WindowFunction(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Clear console"))
                    TotalLog = "Log cleared";
                if (GUILayout.Button("Dump scene"))
                    SceneDumper.DumpScene();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.TextArea(TotalLog, GUI.skin.label);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}