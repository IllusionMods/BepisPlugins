using System;
using UnityEngine;

namespace MessageCenter
{
    public static class ShadowAndOutline
    {
        public static void DrawOutline(Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size)
        {
            float halfSize = size * 0.5F;
            GUIStyle backupStyle = new GUIStyle(style);
            Color backupColor = GUI.color;

            style.normal.textColor = outColor;
            GUI.color = outColor;

            rect.x -= halfSize;
            GUI.Label(rect, text, style);

            rect.x += size;
            GUI.Label(rect, text, style);

            rect.x -= halfSize;
            rect.y -= halfSize;
            GUI.Label(rect, text, style);

            rect.y += size;
            GUI.Label(rect, text, style);

            rect.y -= halfSize;
            style.normal.textColor = inColor;
            GUI.color = backupColor;
            GUI.Label(rect, text, style);

            style = backupStyle;
        }

        public static void DrawShadow(Rect rect, GUIContent content, GUIStyle style, Color txtColor, Color shadowColor, Vector2 direction)
        {
            GUIStyle backupStyle = style;

            style.normal.textColor = shadowColor;
            rect.x += direction.x;
            rect.y += direction.y;
            GUI.Label(rect, content, style);

            style.normal.textColor = txtColor;
            rect.x -= direction.x;
            rect.y -= direction.y;
            GUI.Label(rect, content, style);

            style = backupStyle;
        }
        public static void DrawLayoutShadow(GUIContent content, GUIStyle style, Color txtColor, Color shadowColor, Vector2 direction, params GUILayoutOption[] options)
        {
            DrawShadow(GUILayoutUtility.GetRect(content, style, options), content, style, txtColor, shadowColor, direction);
        }

        public static bool DrawButtonWithShadow(Rect r, GUIContent content, GUIStyle style, float shadowAlpha, Vector2 direction)
        {
            GUIStyle letters = new GUIStyle(style);
            letters.normal.background = null;
            letters.hover.background = null;
            letters.active.background = null;

            bool result = GUI.Button(r, content, style);

            Color color = r.Contains(Event.current.mousePosition) ? letters.hover.textColor : letters.normal.textColor;

            DrawShadow(r, content, letters, color, new Color(0f, 0f, 0f, shadowAlpha), direction);

            return result;
        }

        public static bool DrawLayoutButtonWithShadow(GUIContent content, GUIStyle style, float shadowAlpha, Vector2 direction, params GUILayoutOption[] options)
        {
            return DrawButtonWithShadow(GUILayoutUtility.GetRect(content, style, options), content, style, shadowAlpha, direction);
        }
    }
}
