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
using Logger = BepInEx.Logger;

namespace ConfigurationManager
{
    [BepInPlugin(GUID, "Configuration Manager", Version)]
    [Browsable(false)]
    public class ConfigurationManager : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.configurationmanager";
        public const string Version = Metadata.PluginsVersion;

        private static readonly GUIContent KeyboardShortcutsCategoryName = new GUIContent("Keyboard shortcuts",
            "The first key is the main key, while the rest are modifiers." +
            "\nThe shortcut will only fire when you press \n" +
            "the main key while all modifiers are already pressed.");

        private const string SearchBoxName = "searchBox";
        private const int WindowId = -68;
        private bool _focusSearchBox;

        public event EventHandler<ValueChangedEventArgs<bool>> DisplayingWindowChanged;
        public Action KeyPressedOverride;

        private Dictionary<Type, Action<PropSettingEntry>> _settingDrawHandlers;

        private bool _displayingWindow;

        private readonly SettingFieldDrawer _fieldDrawer = new SettingFieldDrawer();
        private string _modsWithoutSettings;

        private List<PropSettingEntry> _allSettings;
        private List<IGrouping<BepInPlugin, PropSettingEntry>> _filteredSetings;

        private Rect _settingWindowRect, _screenRect;
        private Vector2 _settingWindowScrollPos;

        public static Texture2D TooltipBg;
        public static Texture2D WindowBackground;

        private readonly ConfigWrapper<bool> _showAdvanced;
        private readonly ConfigWrapper<bool> _showKeybinds;
        private readonly ConfigWrapper<bool> _showSettings;
        private bool _showDebug;
        private string _searchString = string.Empty;

        public ConfigurationManager()
        {
            _showAdvanced = new ConfigWrapper<bool>("showAdvanced", this, false);
            _showKeybinds = new ConfigWrapper<bool>("showKeybinds", this, true);
            _showSettings = new ConfigWrapper<bool>("showSettings", this, true);
        }

        public bool DisplayingWindow
        {
            get => _displayingWindow;
            set
            {
                if (_displayingWindow == value) return;
                _displayingWindow = value;

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
            IEnumerable<PropSettingEntry> results = _allSettings;

            if (!string.IsNullOrEmpty(_searchString))
            {
                results = results.Where(x => ContainsSearchString(x, _searchString));
            }
            else
            {
                if (!_showAdvanced.Value)
                    results = results.Where(x => x.IsAdvanced != true);
                if (!_showKeybinds.Value)
                    results = results.Where(x => x.SettingType != typeof(KeyboardShortcut));
                if (!_showSettings.Value)
                    results = results.Where(x => x.IsAdvanced == true || x.SettingType == typeof(KeyboardShortcut));
            }

            _filteredSetings = results.GroupBy(x => x.PluginInfo).OrderBy(x => x.Key.Name).ToList();
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

        private void CalculateWindowRect()
        {
            var size = new Vector2(Mathf.Min(Screen.width - 100, 650), Screen.height - 100);
            var offset = new Vector2((Screen.width - size.x) / 2, (Screen.height - size.y) / 2);
            _settingWindowRect = new Rect(offset, size);

            _screenRect = new Rect(0, 0, Screen.width, Screen.height);
        }

        protected void OnGUI()
        {
            if (DisplayingWindow)
            {
                if (GUI.Button(_screenRect, string.Empty, GUI.skin.box) &&
                    !_settingWindowRect.Contains(Input.mousePosition))
                    DisplayingWindow = false;

                GUI.Box(_settingWindowRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = WindowBackground } });

                GUILayout.Window(WindowId, _settingWindowRect, SettingsWindow, "Plugin / mod settings");
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
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (!_fieldDrawer.DrawCurrentDropdown())
                DrawTooltip(_settingWindowRect);
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

        private void DrawSinglePlugin(IGrouping<BepInPlugin, PropSettingEntry> plugin)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                if (_showDebug)
                    _fieldDrawer.DrawCenteredLabel(new GUIContent($"{plugin.Key.Name.TrimStart('!')} {plugin.Key.Version}", "GUID: " + plugin.Key.GUID));
                else
                    _fieldDrawer.DrawCenteredLabel($"{plugin.Key.Name.TrimStart('!')} {plugin.Key.Version}");

                foreach (var category in plugin.Select(x => new
                {
                    plugin = x,
                    category = x.SettingType == typeof(KeyboardShortcut)
                            ? KeyboardShortcutsCategoryName
                            : new GUIContent(x.Category)
                })
                    .GroupBy(a => a.category.text)
                    .OrderBy(x=> string.Equals(x.Key, KeyboardShortcutsCategoryName.text, StringComparison.Ordinal))
                    .ThenBy(x => x.Key))
                {
                    if (!string.IsNullOrEmpty(category.Key))
                        _fieldDrawer.DrawCenteredLabel(category.First().category);

                    foreach (var setting in category.OrderBy(x => x.plugin.DispName))
                    {
                        DrawSingleSetting(setting.plugin);
                        GUILayout.Space(2);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawSingleSetting(PropSettingEntry setting)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), setting.Description),
                    GUILayout.Width(_settingWindowRect.width / 2.5f));

                if (setting.AcceptableValues is CustomSettingDrawAttribute customDrawer)
                    customDrawer.Run(setting.PluginInstance);
                else if (setting.AcceptableValues is AcceptableValueRangeAttribute range)
                    _fieldDrawer.DrawRangeField(setting, range);
                else if (setting.AcceptableValues is AcceptableValueListAttribute list)
                {
                    try
                    {
                        var acceptableValues = list.GetAcceptableValues(setting.PluginInstance);
                        if (acceptableValues == null || acceptableValues.Length == 0)
                            throw new ArgumentException("AcceptableValueListAttribute returned a null or empty list of acceptable values. You need to supply at least 1 option.");

                        if (!setting.SettingType.IsInstanceOfType(acceptableValues.FirstOrDefault(x => x != null)))
                            throw new ArgumentException("AcceptableValueListAttribute returned a list with items of type other than the settng type itself.");

                        _fieldDrawer.DrawComboboxField(setting, acceptableValues, _settingWindowRect.yMax);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "[ConfigManager] Failed to get acceptable values - " + ex);
                        GUILayout.Label("Failed to get dropdown values");
                    }
                }
                else if (setting.SettingType.IsEnum)
                    _fieldDrawer.DrawComboboxField(setting, Enum.GetValues(setting.SettingType), _settingWindowRect.yMax);
                else
                    DrawFieldBasedOnValueType(setting);

                if (setting.DefaultValue != null)
                {
                    if (DrawDefaultButton())
                        setting.Set(setting.DefaultValue);
                }
                else if (setting.Wrapper != null)
                {
                    var method = setting.Wrapper.GetType()
                        .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                    if (method != null && DrawDefaultButton())
                        method.Invoke(setting.Wrapper, null);
                }
            }
            GUILayout.EndHorizontal();
        }
        private void DrawFieldBasedOnValueType(PropSettingEntry setting)
        {
            if (_settingDrawHandlers.TryGetValue(setting.SettingType, out var drawMethod))
                drawMethod(setting);
            else
                _fieldDrawer.DrawUnknownField(setting);
        }

        private static bool DrawDefaultButton()
        {
            GUILayout.Space(5);
            return GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
        }

        private void Start()
        {
            _settingDrawHandlers = new Dictionary<Type, Action<PropSettingEntry>>
            {
                {typeof(bool), _fieldDrawer.DrawBoolField},
                {typeof(KeyboardShortcut), _fieldDrawer.DrawKeyboardShortcut}
            };

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
            if (Input.GetKeyUp(KeyCode.F1))
            {
                if (KeyPressedOverride != null)
                    KeyPressedOverride();
                else
                    DisplayingWindow = !DisplayingWindow;
            }
        }
    }
}
