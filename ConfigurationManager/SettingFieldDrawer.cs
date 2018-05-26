using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConfigurationManager
{
    internal class SettingFieldDrawer
    {
        private Dictionary<PropSettingEntry, ComboBox> _comboBoxCache = new Dictionary<PropSettingEntry, ComboBox>();

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

        public void DrawComboboxField(PropSettingEntry setting, System.Collections.IList list)
        {
            var buttonText = new GUIContent(setting.Get().ToString());
            var dispRect = GUILayoutUtility.GetRect(buttonText, GUI.skin.button, GUILayout.ExpandWidth(true));

            if (!_comboBoxCache.TryGetValue(setting, out ComboBox box))
            {
                box = new ComboBox(dispRect, buttonText, list.Cast<object>().Select(x => new GUIContent(x.ToString())).ToArray(), GUI.skin.button);
            }
            else
            {
                box.Rect = dispRect;
                box.ButtonContent = buttonText;
            }

            var id = box.Show();
            if (id >= 0 && id < list.Count)
            {
                setting.Set(list[id]);
            }
        }

        public void DrawRangeField(PropSettingEntry setting, AcceptableValueRangeAttribute range)
        {
            object value = setting.Get();
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

        /// <summary>
        /// Unknown type, read only
        /// </summary>
        public void DrawUnknownField(PropSettingEntry setting)
        {
            GUILayout.TextArea(setting.Get()?.ToString() ?? "NULL");
        }
    }
}