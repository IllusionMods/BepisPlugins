using System.ComponentModel;
using BepInEx;
using UnityEngine;
using System.Linq;
using BepisPlugins;
using ConfigurationManager;
using Logger = BepInEx.Logger;

namespace DeveloperConsole
{
    [BepInPlugin(GUID: GUID, Name: "Developer Console", Version: Version)]
    public class DeveloperConsole : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.developerconsole";
        public const string Version = Metadata.PluginsVersion;

        private bool showingUI = false;
        private string TotalLog = "";
        private Rect UI = new Rect(20, 20, 400, 400);
        private Vector2 scrollPosition = Vector2.zero;

        [DisplayName("Show developer console")]
        public SavedKeyboardShortcut ShowKey { get; }

        [DisplayName("Size of the log buffer in characters")]
        [AcceptableValueRange(4000, 16300, false)]
        [Advanced(true)]
        public ConfigWrapper<int> LogDepth { get; }

        public DeveloperConsole()
        {
            LogDepth = new ConfigWrapper<int>("LogDepth", this, 16300);
            ShowKey = new SavedKeyboardShortcut("ShowKey", this, new KeyboardShortcut(KeyCode.F12));
        }

        protected void Awake()
        {
            Logger.EntryLogged += (level, log) =>
            {
                string current = $"{TotalLog}\r\n{log}";
                if (current.Length > LogDepth.Value)
                {
                    var trimmed = current.Remove(0, 1000);

                    // Trim until the first newline to avoid partial line
                    var newlineHit = false;
                    trimmed = new string(trimmed.SkipWhile(x => !newlineHit && !(newlineHit = (x == '\n'))).ToArray());

                    current = "--LOG TRIMMED--\n" + trimmed;
                }
                TotalLog = current;

                scrollPosition = new Vector2(0, float.MaxValue);
            };
        }

        protected void OnGUI()
        {
            if (showingUI)
                UI = GUILayout.Window(589, UI, WindowFunction, "Developer Console");
        }

        protected void Update()
        {
            if (ShowKey.IsDown())
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

                BepInExSettings.LogDebug = GUILayout.Toggle(BepInExSettings.LogDebug, "Debug");
                BepInExSettings.LogInfo = GUILayout.Toggle(BepInExSettings.LogInfo, "Info");
                BepInExSettings.LogUnity = GUILayout.Toggle(BepInExSettings.LogUnity, "Unity");
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