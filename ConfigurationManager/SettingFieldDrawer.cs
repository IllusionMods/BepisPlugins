// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using ConfigurationManager.Utilities;
using UnityEngine;

namespace ConfigurationManager
{
    internal class SettingFieldDrawer
    {
        private static readonly IEnumerable<KeyCode> KeysToCheck =
            KeyboardShortcut.AllKeyCodes.Except(new[] { KeyCode.Mouse0 }).ToArray();

        private readonly Dictionary<PropSettingEntry, ComboBox> _comboBoxCache =
            new Dictionary<PropSettingEntry, ComboBox>();

        private PropSettingEntry CurrentKeyboardShortcutToSet;

        public void ClearCache()
        {
            _comboBoxCache.Clear();
        }

        public void DrawBoolField(PropSettingEntry setting)
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

        public void DrawComboboxField(PropSettingEntry setting, IList list, float windowYmax)
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

        public void DrawRangeField(PropSettingEntry setting, AcceptableValueRangeAttribute range)
        {
            var value = setting.Get();
            var converted = (float)Convert.ToDouble(value);
            var leftValue = (float)Convert.ToDouble(range.MinValue);
            var rightValue = (float)Convert.ToDouble(range.MaxValue);

            var result = GUILayout.HorizontalSlider(converted, leftValue, rightValue, GUILayout.ExpandWidth(true));
            if (Math.Abs(result - converted) > Mathf.Abs(rightValue - leftValue) / 1000)
            {
                var newValue = Convert.ChangeType(result, setting.SettingType);
                setting.Set(newValue);
            }

            if (range.ShowAsPercentage)
                DrawCenteredLabel(
                    Mathf.Round(100 * Mathf.Abs(result - leftValue) / Mathf.Abs(rightValue - leftValue)) + "%",
                    GUILayout.Width(50));
            else
            {
                var strVal = value.ToString();
                var strResult = GUILayout.TextField(strVal, GUILayout.Width(50));
                if (strResult != strVal)
                    setting.Set(Convert.ChangeType(strResult, setting.SettingType));
            }
        }

        /// <summary>
        ///     Unknown type, read only
        /// </summary>
        public void DrawUnknownField(PropSettingEntry setting)
        {
            // Try to use user-supplied converters
            if (setting.ObjToStr != null && setting.StrToObj != null)
            {
                var text = setting.ObjToStr(setting.Get());
                var result = GUILayout.TextField(text);
                if (result != text)
                    setting.Set(setting.StrToObj(result));
                return;
            }

            // Fall back to slow/less reliable method
            var value = setting.Get()?.ToString() ?? "NULL";
            if (CanCovert(value, setting.SettingType))
            {
                var result = GUILayout.TextField(value);
                if (result != value)
                    setting.Set(Convert.ChangeType(result, setting.SettingType));
            }
            else
            {
                GUILayout.TextArea(value);
            }
        }

        private readonly Dictionary<Type, bool> _canCovertCache = new Dictionary<Type, bool>();
        private Boolean CanCovert(string value, Type type)
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

        public void DrawKeyboardShortcut(PropSettingEntry setting)
        {
            var shortcut = (KeyboardShortcut)setting.Get();

            GUILayout.BeginHorizontal();
            {
                if (CurrentKeyboardShortcutToSet == setting)
                {
                    GUILayout.TextArea("Press any key combination", GUILayout.ExpandWidth(true));

                    foreach (var key in KeysToCheck)
                        if (Input.GetKeyUp(key))
                        {
                            shortcut.MainKey = key;
                            shortcut.Modifiers = KeysToCheck.Where(Input.GetKey).OrderBy(x => x.ToString()).ToArray();

                            CurrentKeyboardShortcutToSet = null;
                            break;
                        }

                    if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                        CurrentKeyboardShortcutToSet = null;
                }
                else
                {
                    if (GUILayout.Button(shortcut.ToString(), GUILayout.ExpandWidth(true)))
                        CurrentKeyboardShortcutToSet = setting;

                    if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    {
                        setting.Set(KeyboardShortcut.Empty);
                        CurrentKeyboardShortcutToSet = null;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}