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
        private int showCounter = 0;
        private bool showingUI = false;
        private string TotalLog = "";
        private string TotalShowingLog = "";
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

                if ((level & LogLevel.Message) != 0)
                {
                    if (showCounter == 0)
                        TotalShowingLog = "";

                    showCounter = 400;
                    TotalShowingLog = $"{TotalShowingLog}\r\n{log}";
                }
            };
        }

        private void OnGUI()
        {
            ShowLog();

            if (showingUI)
                UI = GUILayout.Window("com.bepis.bepinex.developerconsole".GetHashCode() + 0, UI, WindowFunction, "Developer Console");
        }

        private void ShowLog()
        {
            if (showCounter != 0)
            {
                showCounter--;

                GUI.Label(new Rect(40, 0, 600, 160), TotalShowingLog, new GUIStyle
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 26,
                    normal = new GUIStyleState
                    {
                        textColor = Color.white
                    }
                });
            }
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