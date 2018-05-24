using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ConfigurationManager
{
    [BepInPlugin(GUID: "com.bepis.bepinex.configurationmanager", Name: "Configuration Manager", Version: "1.0")]
    public class ConfigurationManager : BaseUnityPlugin
    {
        private readonly Type baseSettingType = typeof(ConfigWrapper<>);

        private bool displayingButton, displayingWindow;

        private List<SettingEntry> settings;

        private Rect settingWindowRect, buttonRect, screenRect;

        private Vector2 settingWindowScrollPos;

        public bool DisplayingButton
        {
            get => displayingButton; set
            {
                if (displayingButton == value) return;

                displayingButton = value;

                if (displayingButton)
                {
                    CalculateWindowRect();

                    settings = BuildSettingList();
                }
                else
                {
                    Utilities.SetGameCanvasInputsEnabled(true);
                }
            }
        }

        public bool DisplayingWindow
        {
            get => displayingWindow; set
            {
                if (displayingWindow == value) return;
                displayingWindow = value;

                Utilities.SetGameCanvasInputsEnabled(!displayingWindow);
            }
        }

        private static void DrawCenteredLabel(string text, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static bool IsConfigOpened()
        {
            return Manager.Scene.Instance.AddSceneName == "Config";
        }

        private List<SettingEntry> BuildSettingList()
        {
            var list = new List<SettingEntry>();

            foreach (var plugin in Utilities.FindPlugins())
            {
                var type = plugin.GetType();

                var pluginInfo = TypeLoader.GetMetadata(type);
                if (pluginInfo == null)
                {
                    BepInLogger.Log($"Error: Plugin {type.FullName} is missing the BepInPlugin attribute!");
                    continue;
                }

                var settingProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                list.AddRange(settingProps.Select((x) => SettingEntry.FromConfigWrapper(plugin, x, pluginInfo)).Where(x => x.Browsable));

                var settingPropsStatic = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                list.AddRange(settingPropsStatic.Select((x) => SettingEntry.FromConfigWrapper(null, x, pluginInfo)).Where(x => x.Browsable));

                //TODO scan normal properties too
            }

            return list;
        }

        private void CalculateWindowRect()
        {
            var size = new Vector2(Mathf.Min(Screen.width - 100, 600), Screen.height - 100);
            var offset = new Vector2((Screen.width - size.x) / 2, (Screen.height - size.y) / 2);
            settingWindowRect = new Rect(offset, size);

            var buttonOffsetH = Screen.width * 0.12f;
            var buttonWidth = 215f;
            buttonRect = new Rect(Screen.width - buttonOffsetH - buttonWidth, Screen.height * 0.033f, buttonWidth, Screen.height * 0.04f);

            screenRect = new Rect(0, 0, Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            if (!DisplayingButton) return;

            if (GUI.Button(buttonRect, "Plugin / mod settings"))
            {
                DisplayingWindow = !DisplayingWindow;
            }

            if (DisplayingWindow)
            {
                if (GUI.Button(screenRect, string.Empty, GUI.skin.box) && !settingWindowRect.Contains(Input.mousePosition))
                    DisplayingWindow = false;

                GUILayout.Window(-68, settingWindowRect, SettingsWindow, "Plugin / mod settings");
            }
        }

        private void SettingsWindow(int id)
        {
            settingWindowScrollPos = GUILayout.BeginScrollView(settingWindowScrollPos, GUI.skin.box);
            GUILayout.BeginVertical();
            {
                foreach (var plugin in settings.GroupBy(x => x.PluginInfo).OrderBy(x => x.Key))
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    {
                        DrawCenteredLabel($"{plugin.Key.Name} {plugin.Key.Version.ToString()}");

                        foreach (var category in plugin.GroupBy(x => x.Category).OrderBy(x => x.Key))
                        {
                            if (!string.IsNullOrEmpty(category.Key))
                            {
                                GUILayout.BeginVertical(GUI.skin.box);
                                DrawCenteredLabel(category.Key);
                            }

                            foreach (var setting in category.OrderBy(x => x.DispName))
                            {
                                DrawSingleSetting(setting);
                            }

                            if (!string.IsNullOrEmpty(category.Key))
                                GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawSingleSetting(SettingEntry setting)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(setting.DispName, GUILayout.Width(settingWindowRect.width / 2.5f));

                var value = setting.Get();

                if (setting.SettingType == typeof(bool))
                {
                    var boolVal = (bool)value;
                    var result = GUILayout.Toggle(boolVal, boolVal ? "Enabled" : "Disabled", GUILayout.ExpandWidth(true));
                    if (result != boolVal)
                        setting.Set(result);
                }
                else if (setting.AcceptableValues is AcceptableValueRangeAttribute range)
                {
                    var converted = (float)Convert.ToDouble(value);
                    var leftValue = (float)Convert.ToDouble(range.MinValue);
                    var rightValue = (float)Convert.ToDouble(range.MaxValue);

                    var result = GUILayout.HorizontalSlider(converted, leftValue, rightValue, GUILayout.ExpandWidth(true));
                    if (Math.Abs(result - converted) > Mathf.Abs(rightValue - leftValue) / 1000)
                    {
                        var newValue = Convert.ChangeType(result, value.GetType());
                        setting.Set(newValue);
                    }
                    // todo handle decimals and integers
                    DrawCenteredLabel(Mathf.Round(100 * Mathf.Abs(result - leftValue) / Mathf.Abs(rightValue - leftValue)) + "%", GUILayout.Width(50));
                }
                else if (setting.AcceptableValues is AcceptableValueRangeAttribute list)
                {
                    //TODO implement
                }
                /*else if ()
                {
                    //TODO if type can be converted to and from string, show it in a textfield
                }*/
                else
                {
                    // Unknown type, read only
                    GUILayout.TextArea(setting.Get()?.ToString() ?? "NULL");
                }

                if (setting.DefaultValue != null)
                {
                    if (GUILayout.Button("Default", GUILayout.ExpandWidth(false)))
                        setting.Set(setting.DefaultValue);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void Start()
        {
        }

        private void Update()
        {
            DisplayingButton = IsConfigOpened();

            if (DisplayingButton && DisplayingWindow && Input.GetKey(KeyCode.Escape))
                DisplayingWindow = false;
        }
    }
}