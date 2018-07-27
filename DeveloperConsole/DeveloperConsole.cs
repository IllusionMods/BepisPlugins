using System.ComponentModel;
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

                LogDebug = GUILayout.Toggle(LogDebug, "Debug");
                LogInfo = GUILayout.Toggle(LogInfo, "Info");
                LogUnity = GUILayout.Toggle(LogUnity, "Unity");
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

        #region LogSettings

        [Browsable(true)]
        [DisplayName("Enable Unity message logging")]
        [DefaultValue(false)]
        [Description("Needs BepInEx later than v4.0\nChanges take effect after game reload.")]
        public static bool LogUnity
        {
            get => bool.Parse(Config.GetEntry("log_unity_messages", "false", "Global"));
            set => Config.SetEntry("log_unity_messages", value.ToString(), "Global");
        }

        [Browsable(true)]
        [DisplayName("Enable DEBUG logging")]
        [DefaultValue(false)]
        public static bool LogDebug
        {
            get => GetDebugFlag(LogLevel.Debug);
            set => SetDebugFlag(value, LogLevel.Debug);
        }
        [Browsable(true)]
        [DisplayName("Enable INFO logging")]
        [DefaultValue(false)]
        public static bool LogInfo
        {
            get => GetDebugFlag(LogLevel.Info);
            set => SetDebugFlag(value, LogLevel.Info);
        }
        [Browsable(true)]
        [DisplayName("Enable MESSAGE logging")]
        [DefaultValue(true)]
        public static bool LogMessage
        {
            get => GetDebugFlag(LogLevel.Message);
            set => SetDebugFlag(value, LogLevel.Message);
        }

        [DisplayName("Size of the log buffor in characters")]
        [AcceptableValueRange(4000, 16300, false)]
        public ConfigWrapper<int> LogDepth { get; }

        public DeveloperConsole()
        {
            LogDepth = new ConfigWrapper<int>("LogDepth", this, 16300);
        }

        private static void SetDebugFlag(bool value, LogLevel level)
        {
            if (value)
                Logger.CurrentLogger.DisplayedLevels |= level;
            else
                Logger.CurrentLogger.DisplayedLevels &= ~level;
            
            Config.SetEntry("logger-displayed-levels", ((int)Logger.CurrentLogger.DisplayedLevels).ToString(), "");
        }

        private static bool GetDebugFlag(LogLevel level)
        {
            return (Logger.CurrentLogger.DisplayedLevels & level) == level;
        }

        #endregion
    }
}