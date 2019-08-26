// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx;
using ConfigurationManager.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace ConfigurationManager
{
    internal class SettingFieldDrawer
    {
        private static readonly IEnumerable<KeyCode> KeysToCheck =
            BepInEx.Configuration.KeyboardShortcut.AllKeyCodes.Except(new[] { KeyCode.Mouse0 }).ToArray();

        private readonly Dictionary<SettingEntryBase, ComboBox> _comboBoxCache =
            new Dictionary<SettingEntryBase, ComboBox>();

        private SettingEntryBase CurrentKeyboardShortcutToSet;

        public void ClearCache() => _comboBoxCache.Clear();

        public void DrawBoolField(SettingEntryBase setting)
        {
            var boolVal = (bool)setting.Get();
            var result = GUILayout.Toggle(boolVal, boolVal ? "Enabled" : "Disabled", GUILayout.ExpandWidth(true));
            if (result != boolVal)
                setting.Set(result);
        }

        public void DrawCenteredLabel(string text, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        internal void DrawCenteredLabel(GUIContent content, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            GUILayout.Label(content);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void DrawComboboxField(SettingEntryBase setting, IList list, float windowYmax)
        {
            var buttonText = ObjectToGuiContent(setting.Get());
            var dispRect = GUILayoutUtility.GetRect(buttonText, GUI.skin.button, GUILayout.ExpandWidth(true));

            if (!_comboBoxCache.TryGetValue(setting, out var box))
            {
                box = new ComboBox(dispRect, buttonText, list.Cast<object>().Select(ObjectToGuiContent).ToArray(), GUI.skin.button, windowYmax);
                _comboBoxCache[setting] = box;
            }
            else
            {
                box.Rect = dispRect;
                box.ButtonContent = buttonText;
            }

            box.Show(id =>
            {
                if (id >= 0 && id < list.Count)
                    setting.Set(list[id]);
            });
        }

        private static GUIContent ObjectToGuiContent(object x)
        {
            if (x is Enum)
            {
                var enumType = x.GetType();
                var enumMember = enumType.GetMember(x.ToString()).FirstOrDefault();
                var attr = enumMember?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                if (attr != null)
                    return new GUIContent(attr.Description);
                return new GUIContent(x.ToString().ToProperCase());
            }
            return new GUIContent(x.ToString());
        }

        public bool DrawCurrentDropdown()
        {
            if (ComboBox.CurrentDropdownDrawer != null)
            {
                ComboBox.CurrentDropdownDrawer.Invoke();
                ComboBox.CurrentDropdownDrawer = null;
                return true;
            }
            return false;
        }

        public void DrawRangeField(SettingEntryBase setting)
        {
            var value = setting.Get();
            var converted = (float)Convert.ToDouble(value);
            var leftValue = (float)Convert.ToDouble(setting.AcceptableValueRange.Key);
            var rightValue = (float)Convert.ToDouble(setting.AcceptableValueRange.Value);

            var result = GUILayout.HorizontalSlider(converted, leftValue, rightValue, GUILayout.ExpandWidth(true));
            if (Math.Abs(result - converted) > Mathf.Abs(rightValue - leftValue) / 1000)
            {
                var newValue = Convert.ChangeType(result, setting.SettingType);
                setting.Set(newValue);
            }

            if (setting.ShowRangeAsPercent == true)
            {
                DrawCenteredLabel(
                    Mathf.Round(100 * Mathf.Abs(result - leftValue) / Mathf.Abs(rightValue - leftValue)) + "%",
                    GUILayout.Width(50));
            }
            else
            {
                var strVal = value.ToString();
                var strResult = GUILayout.TextField(strVal, GUILayout.Width(50));
                if (strResult != strVal)
                {
                    var resultVal = (float)Convert.ToDouble(strResult);
                    var clampedResultVal = Mathf.Clamp(resultVal, leftValue, rightValue);
                    setting.Set(Convert.ChangeType(clampedResultVal, setting.SettingType));
                }
            }
        }

        public void DrawUnknownField(SettingEntryBase setting, int rightColumnWidth)
        {
            // Try to use user-supplied converters
            if (setting.ObjToStr != null && setting.StrToObj != null)
            {
                var text = setting.ObjToStr(setting.Get());
                var result = GUILayout.TextField(text, GUILayout.MaxWidth(rightColumnWidth));
                if (result != text)
                    setting.Set(setting.StrToObj(result));
                return;
            }

            // Fall back to slow/less reliable method
            var value = setting.Get()?.ToString() ?? "NULL";
            if (CanCovert(value, setting.SettingType))
            {
                var result = GUILayout.TextField(value, GUILayout.MaxWidth(rightColumnWidth));
                if (result != value)
                    setting.Set(Convert.ChangeType(result, setting.SettingType));
            }
            else
            {
                GUILayout.TextArea(value, GUILayout.MaxWidth(rightColumnWidth));
            }

            GUILayout.FlexibleSpace();
        }

        private readonly Dictionary<Type, bool> _canCovertCache = new Dictionary<Type, bool>();
        private bool CanCovert(string value, Type type)
        {
            if (_canCovertCache.ContainsKey(type))
                return _canCovertCache[type];

            try
            {
                var _ = Convert.ChangeType(value, type);
                _canCovertCache[type] = true;
                return true;
            }
            catch
            {
                _canCovertCache[type] = false;
                return false;
            }
        }

        public void DrawKeyboardShortcut(SettingEntryBase setting)
        {
            var value = setting.Get();
            var isOldType = value is KeyboardShortcut;

            GUILayout.BeginHorizontal();
            {
                if (CurrentKeyboardShortcutToSet == setting)
                {
                    GUILayout.Label("Press any key combination", GUILayout.ExpandWidth(true));
                    GUIUtility.keyboardControl = -1;

                    foreach (var key in KeysToCheck)
                        if (Input.GetKeyUp(key))
                        {
                            if (isOldType) setting.Set(new KeyboardShortcut(key, KeysToCheck.Where(Input.GetKey).ToArray()));
                            else setting.Set(new BepInEx.Configuration.KeyboardShortcut(key, KeysToCheck.Where(Input.GetKey).ToArray()));
                            CurrentKeyboardShortcutToSet = null;
                            break;
                        }

                    if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                        CurrentKeyboardShortcutToSet = null;
                }
                else
                {
                    if (GUILayout.Button(value.ToString(), GUILayout.ExpandWidth(true)))
                        CurrentKeyboardShortcutToSet = setting;

                    if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    {
                        if (isOldType) setting.Set(new KeyboardShortcut());
                        else setting.Set(BepInEx.Configuration.KeyboardShortcut.Empty);
                        CurrentKeyboardShortcutToSet = null;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}