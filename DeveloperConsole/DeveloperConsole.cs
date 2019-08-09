using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepisPlugins;
using ConfigurationManager;
using System.Linq;
using UnityEngine;

namespace DeveloperConsole
{
    [BepInPlugin(GUID: GUID, Name: "Developer Console", Version: Version)]
    public partial class DeveloperConsole : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.developerconsole";
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;

        private bool showingUI = false;
        private static string TotalLog = "";
        private Rect UI = new Rect(20, 20, 400, 400);
        private static Vector2 scrollPosition = Vector2.zero;

        public SavedKeyboardShortcut ShowKey { get; }

        [Advanced(true)]
        public static ConfigWrapper<int> LogDepth { get; private set; }

        public DeveloperConsole()
        {
            ShowKey = new SavedKeyboardShortcut(Config, "Show developer console", "", new KeyboardShortcut(KeyCode.Pause));
            LogDepth = Config.Wrap("Config", "Log buffer size", "Size of the log buffer in characters", 16300);
            BepInEx.Logging.Logger.Listeners.Add(new LogListener());
            Logger = base.Logger;
        }

        private static void OnEntryLogged(LogEventArgs logEventArgs)
        {
            string current = $"{TotalLog}\r\n{logEventArgs.Data?.ToString()}";
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
        }

        protected void OnGUI()
        {
            if (showingUI)
                UI = GUILayout.Window(589, UI, WindowFunction, "Developer Console");
        }

        protected void Update()
        {
            if (ShowKey.IsDown())
                showingUI = !showingUI;
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