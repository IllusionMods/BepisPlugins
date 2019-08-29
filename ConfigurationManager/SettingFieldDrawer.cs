// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx;
using ConfigurationManager.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ConfigurationManager
{
    internal class SettingFieldDrawer
    {
        private static readonly IEnumerable<KeyCode> _keysToCheck = BepInEx.Configuration.KeyboardShortcut.AllKeyCodes.Except(new[] { KeyCode.Mouse0 }).ToArray();

        public Dictionary<Type, Action<SettingEntryBase>> SettingDrawHandlers { get; }

        private readonly Dictionary<SettingEntryBase, ComboBox> _comboBoxCache = new Dictionary<SettingEntryBase, ComboBox>();
        private readonly Dictionary<SettingEntryBase, ColorCacheEntry> _colorCache = new Dictionary<SettingEntryBase, ColorCacheEntry>();

        private readonly ConfigurationManager _instance;

        private SettingEntryBase _currentKeyboardShortcutToSet;

        public SettingFieldDrawer(ConfigurationManager instance)
        {
            _instance = instance;
            SettingDrawHandlers = new Dictionary<Type, Action<SettingEntryBase>>
            {
                {typeof(bool), DrawBoolField},
                {typeof(BepInEx.KeyboardShortcut), DrawKeyboardShortcut},
                {typeof(BepInEx.Configuration.KeyboardShortcut), DrawKeyboardShortcut},
                {typeof(Color), DrawColor },
                {typeof(Vector2), DrawVector2 },
                {typeof(Vector3), DrawVector3 },
                {typeof(Vector4), DrawVector4 },
                {typeof(Quaternion), DrawQuaternion },
            };
        }

        public void DrawSettingValue(SettingEntryBase setting)
        {
            if (setting.CustomDrawer != null)
                setting.CustomDrawer.Run(setting.PluginInstance);
            else if (setting.ShowRangeAsPercent != null)
                DrawRangeField(setting);
            else if (setting.AcceptableValues != null)
                DrawListField(setting);
            else if (setting.SettingType.IsEnum)
                DrawComboboxField(setting, Enum.GetValues(setting.SettingType), _instance.SettingWindowRect.yMax);
            else
                DrawFieldBasedOnValueType(setting);
        }

        public void ClearCache()
        {
            _comboBoxCache.Clear();

            foreach (var tex in _colorCache)
                UnityEngine.Object.Destroy(tex.Value.Tex);
            _colorCache.Clear();
        }

        public static void DrawCenteredLabel(string text, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static void DrawCenteredLabel(GUIContent content, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            GUILayout.Label(content);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static bool DrawCurrentDropdown()
        {
            if (ComboBox.CurrentDropdownDrawer != null)
            {
                ComboBox.CurrentDropdownDrawer.Invoke();
                ComboBox.CurrentDropdownDrawer = null;
                return true;
            }
            return false;
        }

        private void DrawListField(SettingEntryBase setting)
        {
            var acceptableValues = setting.AcceptableValues;
            if (acceptableValues.Length == 0)
                throw new ArgumentException("AcceptableValueListAttribute returned an empty list of acceptable values. You need to supply at least 1 option.");

            if (!setting.SettingType.IsInstanceOfType(acceptableValues.FirstOrDefault(x => x != null)))
                throw new ArgumentException("AcceptableValueListAttribute returned a list with items of type other than the settng type itself.");

            DrawComboboxField(setting, acceptableValues, _instance.SettingWindowRect.yMax);
        }

        private void DrawFieldBasedOnValueType(SettingEntryBase setting)
        {
            if (SettingDrawHandlers.TryGetValue(setting.SettingType, out var drawMethod))
                drawMethod(setting);
            else
                DrawUnknownField(setting, _instance.RightColumnWidth);
        }

        private static void DrawBoolField(SettingEntryBase setting)
        {
            var boolVal = (bool)setting.Get();
            var result = GUILayout.Toggle(boolVal, boolVal ? "Enabled" : "Disabled", GUILayout.ExpandWidth(true));
            if (result != boolVal)
                setting.Set(result);
        }

        private void DrawComboboxField(SettingEntryBase setting, IList list, float windowYmax)
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

        private static void DrawRangeField(SettingEntryBase setting)
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

        private void DrawUnknownField(SettingEntryBase setting, int rightColumnWidth)
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

        private void DrawKeyboardShortcut(SettingEntryBase setting)
        {
            var value = setting.Get();
            var isOldType = value is KeyboardShortcut;

            if (_currentKeyboardShortcutToSet == setting)
            {
                GUILayout.Label("Press any key combination", GUILayout.ExpandWidth(true));
                GUIUtility.keyboardControl = -1;

                foreach (var key in _keysToCheck)
                    if (Input.GetKeyUp(key))
                    {
                        if (isOldType) setting.Set(new KeyboardShortcut(key, _keysToCheck.Where(Input.GetKey).ToArray()));
                        else setting.Set(new BepInEx.Configuration.KeyboardShortcut(key, _keysToCheck.Where(Input.GetKey).ToArray()));
                        _currentKeyboardShortcutToSet = null;
                        break;
                    }

                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                    _currentKeyboardShortcutToSet = null;
            }
            else
            {
                if (GUILayout.Button(value.ToString(), GUILayout.ExpandWidth(true)))
                    _currentKeyboardShortcutToSet = setting;

                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                {
                    if (isOldType) setting.Set(new KeyboardShortcut());
                    else setting.Set(BepInEx.Configuration.KeyboardShortcut.Empty);
                    _currentKeyboardShortcutToSet = null;
                }
            }
        }

        private static void DrawVector2(SettingEntryBase obj)
        {
            var setting = (Vector2)obj.Get();
            var copy = setting;
            setting.x = DrawSingleVectorSlider(setting.x, "X");
            setting.y = DrawSingleVectorSlider(setting.y, "Y");
            if (setting != copy) obj.Set(setting);
        }

        private static void DrawVector3(SettingEntryBase obj)
        {
            var setting = (Vector3)obj.Get();
            var copy = setting;
            setting.x = DrawSingleVectorSlider(setting.x, "X");
            setting.y = DrawSingleVectorSlider(setting.y, "Y");
            setting.z = DrawSingleVectorSlider(setting.z, "Z");
            if (setting != copy) obj.Set(setting);
        }

        private static void DrawVector4(SettingEntryBase obj)
        {
            var setting = (Vector4)obj.Get();
            var copy = setting;
            setting.x = DrawSingleVectorSlider(setting.x, "X");
            setting.y = DrawSingleVectorSlider(setting.y, "Y");
            setting.z = DrawSingleVectorSlider(setting.z, "Z");
            setting.w = DrawSingleVectorSlider(setting.w, "W");
            if (setting != copy) obj.Set(setting);
        }

        private static void DrawQuaternion(SettingEntryBase obj)
        {
            var setting = (Quaternion)obj.Get();
            var copy = setting;
            setting.x = DrawSingleVectorSlider(setting.x, "X");
            setting.y = DrawSingleVectorSlider(setting.y, "Y");
            setting.z = DrawSingleVectorSlider(setting.z, "Z");
            setting.w = DrawSingleVectorSlider(setting.w, "W");
            if (setting != copy) obj.Set(setting);
        }

        private static float DrawSingleVectorSlider(float setting, string label)
        {
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            float.TryParse(GUILayout.TextField(setting.ToString("F", CultureInfo.InvariantCulture), GUILayout.ExpandWidth(true)), NumberStyles.Any, CultureInfo.InvariantCulture, out var x);
            return x;
        }

        private void DrawColor(SettingEntryBase obj)
        {
            var setting = (Color)obj.Get();

            if (!_colorCache.TryGetValue(obj, out var cacheEntry))
            {
                cacheEntry = new ColorCacheEntry { Tex = new Texture2D(40, 10, TextureFormat.ARGB32, false), Last = setting };
                cacheEntry.Tex.FillTexture(setting);
                _colorCache[obj] = cacheEntry;
            }

            GUILayout.Label("R", GUILayout.ExpandWidth(false));
            setting.r = GUILayout.HorizontalSlider(setting.r, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.Label("G", GUILayout.ExpandWidth(false));
            setting.g = GUILayout.HorizontalSlider(setting.g, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.Label("B", GUILayout.ExpandWidth(false));
            setting.b = GUILayout.HorizontalSlider(setting.b, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.Label("A", GUILayout.ExpandWidth(false));
            setting.a = GUILayout.HorizontalSlider(setting.a, 0f, 1f, GUILayout.ExpandWidth(true));

            GUILayout.Space(4);

            if (setting != cacheEntry.Last)
            {
                obj.Set(setting);
                cacheEntry.Tex.FillTexture(setting);
                cacheEntry.Last = setting;
            }

            GUILayout.Label(cacheEntry.Tex, GUILayout.ExpandWidth(false));
        }

        private sealed class ColorCacheEntry
        {
            public Color Last;
            public Texture2D Tex;
        }
    }
}