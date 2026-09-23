using UnityEngine;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUILayout
    {
        private sealed class GUIGridSizer : GUILayoutEntry
        {
            private readonly int m_Count;
            private readonly int m_XCount;
            private readonly float m_MinButtonWidth = -1;
            private readonly float m_MaxButtonWidth = -1;
            private readonly float m_MinButtonHeight = -1;
            private readonly float m_MaxButtonHeight = -1;

            private GUIGridSizer(GUIContent[] contents, int xCount, GUIStyle buttonStyle, GUILayoutOption[] options) : base(0, 0, 0, 0, GUIStyle.none)
            {
                m_Count = contents.Length;
                m_XCount = xCount;
                ApplyStyleSettings(buttonStyle);
                ApplyOptions(options);
                int rows = this.rows;
                if (xCount != 0 && contents.Length != 0)
                {
                    float x = Mathf.Max(buttonStyle.margin.left, buttonStyle.margin.right) * (m_XCount - 1);
                    float y = Mathf.Max(buttonStyle.margin.top, buttonStyle.margin.bottom) * (rows - 1);
                    if (buttonStyle.fixedWidth != 0)
                        m_MinButtonWidth = m_MaxButtonWidth = buttonStyle.fixedWidth;
                    if (buttonStyle.fixedHeight != 0)
                        m_MinButtonHeight = m_MaxButtonHeight = buttonStyle.fixedHeight;
                    if (m_MinButtonWidth == -1)
                    {
                        if (minWidth != 0)
                            m_MinButtonWidth = (minWidth - x) / m_XCount;
                        if (maxWidth != 0)
                            m_MaxButtonWidth = (maxWidth - x) / m_XCount;
                    }
                    if (m_MinButtonHeight == -1)
                    {
                        if (minHeight != 0)
                            m_MinButtonHeight = (minHeight - y) / rows;
                        if (maxHeight != 0)
                            m_MaxButtonHeight = (maxHeight - y) / rows;
                    }
                    if (m_MinButtonHeight == -1 || m_MaxButtonHeight == -1 || m_MinButtonWidth == -1 || m_MaxButtonWidth == -1)
                    {
                        float xMax = 0, yMax = 0;
                        foreach (GUIContent content in contents)
                        {
                            Vector2 vector = buttonStyle.CalcSize(content);
                            xMax = Mathf.Max(xMax, vector.x);
                            yMax = Mathf.Max(yMax, vector.y);
                        }
                        if (m_MinButtonWidth == -1)
                            m_MinButtonWidth = m_MaxButtonWidth != -1 ? Mathf.Min(xMax, m_MaxButtonWidth) : xMax;
                        if (m_MaxButtonWidth == -1)
                            m_MaxButtonWidth = m_MinButtonWidth != -1 ? Mathf.Max(xMax, m_MinButtonWidth) : xMax;
                        if (m_MinButtonHeight == -1)
                            m_MinButtonHeight = m_MaxButtonHeight != -1 ? Mathf.Min(yMax, m_MaxButtonHeight) : yMax;
                        if (m_MaxButtonHeight == -1)
                            m_MaxButtonHeight = m_MinButtonHeight != -1 ? Mathf.Max(maxHeight, m_MinButtonHeight) : maxHeight;
                    }
                    minWidth = m_MinButtonWidth * m_XCount + x;
                    maxWidth = m_MaxButtonWidth * m_XCount + x;
                    minHeight = m_MinButtonHeight * rows + y;
                    maxHeight = m_MaxButtonHeight * rows + y;
                }
            }

            public static Rect GetRect(GUIContent[] contents, int xCount, GUIStyle style, GUILayoutOption[] options)
            {
                EventType type = Event.current.type;
                if (type != EventType.Layout)
                    return type != EventType.Used ? UnityEngine.GUILayoutUtility.current.topLevel.GetNext().rect : kDummyRect;
                UnityEngine.GUILayoutUtility.current.topLevel.Add(new GUIGridSizer(contents, xCount, style, options));
                return Rect.zero;
            }

            private int rows => m_Count / m_XCount + (m_Count % m_XCount != 0 ? 1 : 0);
        }
    }
}
