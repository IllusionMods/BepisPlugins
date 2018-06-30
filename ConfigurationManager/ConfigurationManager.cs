// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using ConfigurationManager.Utilities;
using Manager;
using BepInEx.Logging;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace ConfigurationManager
{
    [BepInPlugin("com.bepis.bepinex.configurationmanager", "Configuration Manager", "1.0")]
    [Browsable(false)]
    public class ConfigurationManager : BaseUnityPlugin
    {
        private static readonly GUIContent KeyboardShortcutsCategoryName = new GUIContent("Keyboard shortcuts",
                "The first key is the main key, while the rest are modifiers.\nThe shortcut will only fire when you press \nthe main key while all modifiers are already pressed.")
            ;

        private static readonly ICollection<string> updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        private readonly Type baseSettingType = typeof(ConfigWrapper<>);
        private Dictionary<Type, Action<PropSettingEntry>> _settingDrawHandlers;

        private bool displayingButton, displayingWindow;

        private readonly SettingFieldDrawer fieldDrawer = new SettingFieldDrawer();
        private string modsWithoutSettings;

        private List<PropSettingEntry> settings;

        private Rect settingWindowRect, buttonRect, screenRect;
        private Vector2 settingWindowScrollPos;
        private readonly ConfigWrapper<bool> showAdvanced = new ConfigWrapper<bool>("showAdvanced", false);

        private readonly ConfigWrapper<bool> showKeybinds = new ConfigWrapper<bool>("showKeybinds", true);
        private readonly ConfigWrapper<bool> showSettings = new ConfigWrapper<bool>("showSettings", true);

        public bool DisplayingButton
        {
            get => displayingButton;
            set
            {
                if (displayingButton == value) return;

                displayingButton = value;

                if (displayingButton)
                {
                    CalculateWindowRect();

                    BuildSettingList();

                    if (displayingWindow)
                        Utils.SetGameCanvasInputsEnabled(false);
                }
                else
                {
                    Utils.SetGameCanvasInputsEnabled(true);
                }
            }
        }

        public bool DisplayingWindow
        {
            get => displayingWindow;
            set
            {
                if (displayingWindow == value) return;
                displayingWindow = value;

                Utils.SetGameCanvasInputsEnabled(!displayingWindow);
            }
        }

        private static bool IsConfigOpened()
        {
            return Scene.Instance.AddSceneName == "Config";
        }

        private void BuildSettingList()
        {
            var results = Enumerable.Empty<PropSettingEntry>();
            var skippedList = new List<string>();

            foreach (var plugin in Utils.FindPlugins())
            {
                var type = plugin.GetType();

                var pluginInfo = MetadataHelper.GetMetadata(type);
                if (pluginInfo == null)
                {
                    Logger.Log(LogLevel.Error, $"Plugin {type.FullName} is missing the BepInPlugin attribute!");
                    skippedList.Add(pluginInfo.Name);
                    continue;
                }

                if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .Any(x => !x.Browsable))
                {
                    skippedList.Add(pluginInfo.Name);
                    continue;
                }

                var detected = new List<PropSettingEntry>();

                // Config wrappers ------

                var settingProps = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FilterBrowsable(true, true)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                detected.AddRange(settingProps.Select(x => PropSettingEntry.FromConfigWrapper(plugin, x, pluginInfo)));

                var settingPropsStatic = type
                    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FilterBrowsable(true, true)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                detected.AddRange(
                    settingPropsStatic.Select(x => PropSettingEntry.FromConfigWrapper(null, x, pluginInfo)));

                // Normal properties ------

                var normalProps = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .FilterBrowsable(true, true)
                    .Concat(type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .FilterBrowsable(true, false))
                    .Distinct()
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                detected.AddRange(normalProps.Select(x => PropSettingEntry.FromNormalProperty(plugin, x, pluginInfo)));

                var normalPropsStatic = type
                    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .FilterBrowsable(true, true)
                    .Concat(type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .FilterBrowsable(true, false))
                    .Distinct()
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                detected.AddRange(
                    normalPropsStatic.Select(x => PropSettingEntry.FromNormalProperty(null, x, pluginInfo)));

                // Allow to enable/disable plugin if it uses any update methods ------
                if (!type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .All(x => !updateMethodNames.Contains(x.Name)))
                {
                    var enabledSetting =
                        PropSettingEntry.FromNormalProperty(plugin, type.GetProperty("enabled"), pluginInfo);
                    enabledSetting.DispName = "!Allow plugin to run on every frame";
                    enabledSetting.Description =
                        "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                    enabledSetting.IsAdvanced = true;
                    detected.Add(enabledSetting);
                }

                detected.RemoveAll(x => x.Browsable == false);

                if (detected.Any())
                    results = results.Concat(detected);
                else
                    skippedList.Add(pluginInfo.Name);
            }

            if (!showAdvanced.Value)
                results = results.Where(x => x.IsAdvanced != true);
            if (!showKeybinds.Value)
                results = results.Where(x => x.SettingType != typeof(KeyboardShortcut));
            if (!showSettings.Value)
                results = results.Where(x => x.IsAdvanced == true || x.SettingType == typeof(KeyboardShortcut));

            settings = results.ToList();

            modsWithoutSettings =
                string.Join(", ", skippedList.Select(x => x.TrimStart('!')).OrderBy(x => x).ToArray());
        }

        private void CalculateWindowRect()
        {
            var size = new Vector2(Mathf.Min(Screen.width - 100, 600), Screen.height - 100);
            var offset = new Vector2((Screen.width - size.x) / 2, (Screen.height - size.y) / 2);
            settingWindowRect = new Rect(offset, size);

            var buttonOffsetH = Screen.width * 0.12f;
            var buttonWidth = 215f;
            buttonRect = new Rect(Screen.width - buttonOffsetH - buttonWidth, Screen.height * 0.033f, buttonWidth,
                Screen.height * 0.04f);

            screenRect = new Rect(0, 0, Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            if (!DisplayingButton) return;

            if (GUI.Button(buttonRect,
                new GUIContent("Plugin / mod settings",
                    "Change settings of the installed \nBepInEx plugins, if they have any.")))
                DisplayingWindow = !DisplayingWindow;

            if (DisplayingWindow)
            {
                if (GUI.Button(screenRect, string.Empty, GUI.skin.box) &&
                    !settingWindowRect.Contains(Input.mousePosition))
                    DisplayingWindow = false;

                GUILayout.Window(-68, settingWindowRect, SettingsWindow, "Plugin / mod settings");
            }
            else
            {
                DrawTooltip();
            }
        }

        private static void DrawTooltip()
        {
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var currentEvent = Event.current;

                GUI.Label(new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y + 25, 400, 500),
                    GUI.tooltip);
            }
        }

        private void SettingsWindow(int id)
        {
            settingWindowScrollPos = GUILayout.BeginScrollView(settingWindowScrollPos);
            GUILayout.BeginVertical();
            {
                DrawWindowHeader();

                foreach (var plugin in settings.GroupBy(x => x.PluginInfo).OrderBy(x => x.Key.Name))
                    DrawSinglePlugin(plugin);

                GUILayout.Space(10);
                GUILayout.Label("Plugins with no options available: " + modsWithoutSettings);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            DrawTooltip();
        }

        private void DrawWindowHeader()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                var newVal = GUILayout.Toggle(showSettings.Value, "Show settings");
                if (showSettings.Value != newVal)
                {
                    showSettings.Value = newVal;
                    BuildSettingList();
                }

                newVal = GUILayout.Toggle(showKeybinds.Value, "Show keyboard shortcuts");
                if (showKeybinds.Value != newVal)
                {
                    showKeybinds.Value = newVal;
                    BuildSettingList();
                }

                newVal = GUILayout.Toggle(showAdvanced.Value, "Show advanced settings");
                if (showAdvanced.Value != newVal)
                {
                    showAdvanced.Value = newVal;
                    BuildSettingList();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSinglePlugin(IGrouping<BepInPlugin, PropSettingEntry> plugin)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                fieldDrawer.DrawCenteredLabel($"{plugin.Key.Name.TrimStart('!')} {plugin.Key.Version}");

                foreach (var category in plugin.Select(x => new
                    {
                        plugin = x,
                        category = x.SettingType == typeof(KeyboardShortcut)
                            ? KeyboardShortcutsCategoryName
                            : new GUIContent(x.Category)
                    })
                    .GroupBy(a => a.category.text).OrderBy(x => x.Key))
                {
                    if (!string.IsNullOrEmpty(category.Key))
                        fieldDrawer.DrawCenteredLabel(category.First().category);

                    foreach (var setting in category.OrderBy(x => x.plugin.DispName))
                    {
                        DrawSingleSetting(setting.plugin);
                        GUILayout.Space(2);
                    }

                    /*if (!string.IsNullOrEmpty(category.Key))
                    {
                        //GUILayout.EndVertical();
                        GUILayout.Space(2);
                    }*/
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawSingleSetting(PropSettingEntry setting)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), setting.Description),
                    GUILayout.Width(settingWindowRect.width / 2.5f));

                if (setting.AcceptableValues is AcceptableValueRangeAttribute range)
                    fieldDrawer.DrawRangeField(setting, range);
                else if (setting.AcceptableValues is AcceptableValueListAttribute list)
                    fieldDrawer.DrawComboboxField(setting, list.AcceptableValues);
                else if (setting.SettingType.IsEnum)
                    fieldDrawer.DrawComboboxField(setting, Enum.GetValues(setting.SettingType));
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
                fieldDrawer.DrawUnknownField(setting);
        }

        private static bool DrawDefaultButton()
        {
            GUILayout.Space(5);
            return GUILayout.Button("Default", GUILayout.ExpandWidth(false));
        }

        private void Start()
        {
            _settingDrawHandlers = new Dictionary<Type, Action<PropSettingEntry>>
            {
                {typeof(bool), fieldDrawer.DrawBoolField},
                {typeof(KeyboardShortcut), fieldDrawer.DrawKeyboardShortcut}
            };
        }

        private void Update()
        {
            DisplayingButton = IsConfigOpened();
        }
    }
}