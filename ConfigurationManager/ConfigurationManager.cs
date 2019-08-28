// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepisPlugins;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using BepInEx.Configuration;

namespace ConfigurationManager
{
    [BepInPlugin(GUID, "Configuration Manager", Version)]
    [Browsable(false)]
    public class ConfigurationManager : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.configurationmanager";
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;

        private static readonly GUIContent KeyboardShortcutsCategoryName = new GUIContent("Keyboard shortcuts",
            "The first key is the main key, while the rest are modifiers.\n" +
            "The shortcut will only fire when you press \n" +
            "the main key while all modifiers are already pressed.");

        private const string SearchBoxName = "searchBox";
        private const int WindowId = -68;
        private bool _focusSearchBox;

        public event EventHandler<ValueChangedEventArgs<bool>> DisplayingWindowChanged;
        public bool OverrideHotkey;

        private bool _displayingWindow;

        private readonly SettingFieldDrawer _fieldDrawer;
        private string _modsWithoutSettings;

        private List<SettingEntryBase> _allSettings;
        private List<IGrouping<BepInPlugin, SettingEntryBase>> _filteredSetings;

        internal Rect SettingWindowRect { get; private set; }
        private Rect _screenRect;
        private Vector2 _settingWindowScrollPos;

        public static Texture2D TooltipBg;
        public static Texture2D WindowBackground;

        private readonly ConfigWrapper<bool> _showAdvanced;
        private readonly ConfigWrapper<bool> _showKeybinds;
        private readonly ConfigWrapper<bool> _showSettings;
        private readonly ConfigWrapper<BepInEx.Configuration.KeyboardShortcut> _keybind;
        private bool _showDebug;
        private string _searchString = string.Empty;

        public ConfigurationManager()
        {
            Logger = base.Logger;
            _fieldDrawer = new SettingFieldDrawer(this);

            _showAdvanced = Config.GetSetting("Filtering", "Show advanced", false);
            _showKeybinds = Config.GetSetting("Filtering", "Show keybinds", true);
            _showSettings = Config.GetSetting("Filtering", "Show settings", true);
            _keybind = Config.GetSetting("General", "Show config manager", new BepInEx.Configuration.KeyboardShortcut(KeyCode.F1),
                new ConfigDescription("The shortcut used to toggle the config manager window on and off. " +
                                      "The key can be overridden by a game-specific plugin if necessary, in that case this setting is ignored."));
        }

        public bool DisplayingWindow
        {
            get => _displayingWindow;
            set
            {
                if (_displayingWindow == value) return;
                _displayingWindow = value;

                _fieldDrawer.ClearCache();

                if (_displayingWindow)
                {
                    CalculateWindowRect();

                    BuildSettingList();

                    _focusSearchBox = true;
                }

                DisplayingWindowChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }

        private void BuildSettingList()
        {
            SettingSearcher.CollectSettings(out var results, out var modsWithoutSettings, _showDebug);

            _modsWithoutSettings = string.Join(", ", modsWithoutSettings.Select(x => x.TrimStart('!')).OrderBy(x => x).ToArray());
            _allSettings = results.ToList();

            BuildFilteredSettingList();
        }

        private void BuildFilteredSettingList()
        {
            IEnumerable<SettingEntryBase> results = _allSettings;

            if (!string.IsNullOrEmpty(_searchString))
            {
                results = results.Where(x => ContainsSearchString(x, _searchString));
            }
            else
            {
                if (!_showAdvanced.Value)
                    results = results.Where(x => x.IsAdvanced != true);
                if (!_showKeybinds.Value)
                    results = results.Where(x => !IsKeyboardShortcut(x));
                if (!_showSettings.Value)
                    results = results.Where(x => x.IsAdvanced == true || IsKeyboardShortcut(x));
            }

            _filteredSetings = results.GroupBy(x => x.PluginInfo).OrderBy(x => x.Key.Name).ToList();
        }

        private static bool IsKeyboardShortcut(SettingEntryBase x)
        {
            return x.SettingType == typeof(BepInEx.KeyboardShortcut) || x.SettingType == typeof(BepInEx.Configuration.KeyboardShortcut);
        }

        private static bool ContainsSearchString(SettingEntryBase setting, string searchString)
        {
            foreach (var target in new[]
            {
                setting.PluginInfo.Name,
                setting.PluginInfo.GUID,
                setting.DispName,
                setting.Category,
                setting.Description ,
                setting.DefaultValue?.ToString(),
                setting.Get()?.ToString()
            })
            {
                if (target != null && target.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        internal int LeftColumnWidth { get; private set; }
        internal int RightColumnWidth { get; private set; }

        private void CalculateWindowRect()
        {
            var width = Mathf.Min(Screen.width, 650);
            var height = Screen.height < 560 ? Screen.height : Screen.height - 100;
            var offsetX = Mathf.RoundToInt((Screen.width - width) / 2f);
            var offsetY = Mathf.RoundToInt((Screen.height - height) / 2f);
            SettingWindowRect = new Rect(offsetX, offsetY, width, height);

            _screenRect = new Rect(0, 0, Screen.width, Screen.height);

            LeftColumnWidth = Mathf.RoundToInt(SettingWindowRect.width / 2.5f);
            RightColumnWidth = (int)SettingWindowRect.width - LeftColumnWidth - 115;
        }

        protected void OnGUI()
        {
            if (DisplayingWindow)
            {
                if (GUI.Button(_screenRect, string.Empty, GUI.skin.box) &&
                    !SettingWindowRect.Contains(Input.mousePosition))
                    DisplayingWindow = false;

                GUI.Box(SettingWindowRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = WindowBackground } });

                GUILayout.Window(WindowId, SettingWindowRect, SettingsWindow, "Plugin / mod settings");
            }
        }

        private static void DrawTooltip(Rect area)
        {
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var currentEvent = Event.current;

                var style = new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.white, background = TooltipBg },
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };

                const int width = 400;
                var height = style.CalcHeight(new GUIContent(GUI.tooltip), 400) + 10;

                var x = currentEvent.mousePosition.x + width > area.width
                    ? area.width - width
                    : currentEvent.mousePosition.x;

                var y = currentEvent.mousePosition.y + 25 + height > area.height
                    ? currentEvent.mousePosition.y - height
                    : currentEvent.mousePosition.y + 25;

                GUI.Box(new Rect(x, y, width, height), GUI.tooltip, style);
            }
        }

        private void SettingsWindow(int id)
        {
            DrawWindowHeader();

            _settingWindowScrollPos = GUILayout.BeginScrollView(_settingWindowScrollPos, false, true);
            GUILayout.BeginVertical();
            {
                foreach (var plugin in _filteredSetings)
                    DrawSinglePlugin(plugin);

                if (_showDebug)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Plugins with no options available: " + _modsWithoutSettings);
                }
                else
                {
                    // Always leave some space in case there's a dropdown box at the very bottom of the list
                    GUILayout.Space(70);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (!SettingFieldDrawer.DrawCurrentDropdown())
                DrawTooltip(SettingWindowRect);
        }

        private void DrawWindowHeader()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUILayout.Label("Show: ", GUILayout.ExpandWidth(false));

                GUI.enabled = SearchString == string.Empty;

                var newVal = GUILayout.Toggle(_showSettings.Value, "Normal settings");
                if (_showSettings.Value != newVal)
                {
                    _showSettings.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showKeybinds.Value, "Keyboard shortcuts");
                if (_showKeybinds.Value != newVal)
                {
                    _showKeybinds.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showAdvanced.Value, "Advanced settings");
                if (_showAdvanced.Value != newVal)
                {
                    _showAdvanced.Value = newVal;
                    BuildFilteredSettingList();
                }

                GUI.enabled = true;

                newVal = GUILayout.Toggle(_showDebug, "Debug mode");
                if (_showDebug != newVal)
                {
                    _showDebug = newVal;
                    BuildSettingList();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUILayout.Label("Search settings: ", GUILayout.ExpandWidth(false));

                GUI.SetNextControlName(SearchBoxName);
                SearchString = GUILayout.TextField(SearchString, GUILayout.ExpandWidth(true));

                if (_focusSearchBox)
                {
                    GUI.FocusWindow(WindowId);
                    GUI.FocusControl(SearchBoxName);
                    _focusSearchBox = false;
                }

                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    SearchString = string.Empty;
            }
            GUILayout.EndHorizontal();
        }

        public string SearchString
        {
            get { return _searchString; }
            private set
            {
                if (string.IsNullOrEmpty(_searchString))
                    _searchString = string.Empty;

                if (_searchString == value)
                    return;

                _searchString = value;

                BuildFilteredSettingList();
            }
        }

        private void DrawSinglePlugin(IGrouping<BepInPlugin, SettingEntryBase> plugin)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                if (_showDebug)
                    SettingFieldDrawer.DrawCenteredLabel(new GUIContent($"{plugin.Key.Name.TrimStart('!')} {plugin.Key.Version}", "GUID: " + plugin.Key.GUID));
                else
                    SettingFieldDrawer.DrawCenteredLabel($"{plugin.Key.Name.TrimStart('!')} {plugin.Key.Version}");

                foreach (var category in plugin
                    .Select(x => new { plugin = x, category = GetCategory(x) })
                    .GroupBy(x => x.category.text)
                    .OrderBy(x => string.Equals(x.Key, KeyboardShortcutsCategoryName.text, StringComparison.Ordinal))
                    .ThenBy(x => x.Key))
                {
                    if (!string.IsNullOrEmpty(category.Key))
                        SettingFieldDrawer.DrawCenteredLabel(category.First().category);

                    foreach (var setting in category.OrderBy(x => x.plugin.DispName))
                    {
                        DrawSingleSetting(setting.plugin);
                        GUILayout.Space(2);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private static GUIContent GetCategory(SettingEntryBase x)
        {
            // Legacy behavior
            if (x.SettingType == typeof(BepInEx.KeyboardShortcut)) return KeyboardShortcutsCategoryName;

            return new GUIContent(x.Category);
        }

        private void DrawSingleSetting(SettingEntryBase setting)
        {
            GUILayout.BeginHorizontal();
            {
                try
                {
                    DrawSettingName(setting);
                    _fieldDrawer.DrawSettingValue(setting);
                    DrawDefaultButton(setting);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"[ConfigManager] Failed to draw setting {setting.DispName} - {ex}");
                    GUILayout.Label("Failed to draw this field, check log for details.");
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSettingName(SettingEntryBase setting)
        {
            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), setting.Description),
                GUILayout.Width(LeftColumnWidth), GUILayout.MaxWidth(LeftColumnWidth));
        }

        private static void DrawDefaultButton(SettingEntryBase setting)
        {
            bool DrawDefaultButton()
            {
                GUILayout.Space(5);
                return GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
            }

            if (setting.DefaultValue != null)
            {
                if (DrawDefaultButton())
                    setting.Set(setting.DefaultValue);
            }
            else if (setting.Wrapper != null)
            {
                var method = setting.Wrapper.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                if (method != null && DrawDefaultButton())
                    method.Invoke(setting.Wrapper, null);
            }
            else if (setting.SettingType.IsClass)
            {
                if (DrawDefaultButton())
                    setting.Set(null);
            }
        }

        private void Start()
        {
            var background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            background.SetPixel(0, 0, Color.black);
            background.Apply();
            TooltipBg = background;

            var windowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            windowBackground.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1));
            windowBackground.Apply();
            WindowBackground = windowBackground;
        }

        private void Update()
        {
            if (OverrideHotkey) return;

            if (_keybind.Value.IsDown())
            {
                DisplayingWindow = !DisplayingWindow;
            }
        }
    }
}
