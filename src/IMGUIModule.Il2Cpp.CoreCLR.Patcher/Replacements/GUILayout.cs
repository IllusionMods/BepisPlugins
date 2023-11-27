using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using static UnityEngine.GUILayout;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUILayout
    {
        public static void FlexibleSpace()
        {
            GUIUtility.CheckOnGUI();
            GUILayoutOption layoutOption = GUILayoutUtility.current.topLevel.isVertical ?
                ExpandHeight(true) :
                ExpandWidth(true);
            layoutOption.value = 10000;
            GUILayoutUtility.GetRect(0, 0, GUILayoutUtility.spaceStyle, layoutOption);
            if (Event.current.type == EventType.Layout)
                GUILayoutUtility.current.topLevel.entries[GUILayoutUtility.current.topLevel.entries.Count - 1].consideredForMargin = false;
        }

        public static GUILayoutOption Width(float width) =>
            new GUILayoutOption(GUILayoutOption.Type.fixedWidth, width);

        public static GUILayoutOption Height(float height) =>
            new GUILayoutOption(GUILayoutOption.Type.fixedHeight, height);

        public static GUILayoutOption MinWidth(float minWidth) =>
            new GUILayoutOption(GUILayoutOption.Type.minWidth, minWidth);

        public static GUILayoutOption MaxWidth(float maxWidth) =>
            new GUILayoutOption(GUILayoutOption.Type.maxWidth, maxWidth);

        public static GUILayoutOption MinHeight(float minHeight) =>
            new GUILayoutOption(GUILayoutOption.Type.minHeight, minHeight);

        public static GUILayoutOption MaxHeight(float maxHeight) =>
            new GUILayoutOption(GUILayoutOption.Type.maxHeight, maxHeight);

        public static int SelectionGrid(int selected, Il2CppReferenceArray<GUIContent> contents, int xCount, GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options)
        {
            return UnityEngine.GUI.SelectionGrid(IMGUIModule.Il2Cpp.CoreCLR.GUIGridSizer.GetRect(contents, xCount, style, options), selected, contents, xCount, style);
        }
    }

    internal sealed class GUIGridSizer : GUILayoutEntry
    {
        private GUIGridSizer(GUIContent[] contents, int xCount, GUIStyle buttonStyle, GUILayoutOption[] options)
            : base(0f, 0f, 0f, 0f, GUIStyle.none)
        {
            m_Count = contents.Length;
            m_XCount = xCount;
            ApplyStyleSettings(buttonStyle);
            ApplyOptions(options);
            bool flag = xCount == 0 || contents.Length == 0;
            if (!flag)
            {
                float num = Mathf.Max(buttonStyle.margin.left, buttonStyle.margin.right) * (m_XCount - 1);
                float num2 = Mathf.Max(buttonStyle.margin.top, buttonStyle.margin.bottom) * (rows - 1);
                bool flag2 = buttonStyle.fixedWidth != 0f;
                if (flag2)
                {
                    m_MinButtonWidth = (m_MaxButtonWidth = buttonStyle.fixedWidth);
                }
                bool flag3 = buttonStyle.fixedHeight != 0f;
                if (flag3)
                {
                    m_MinButtonHeight = (m_MaxButtonHeight = buttonStyle.fixedHeight);
                }
                bool flag4 = m_MinButtonWidth == -1f;
                if (flag4)
                {
                    bool flag5 = minWidth != 0f;
                    if (flag5)
                    {
                        m_MinButtonWidth = (minWidth - num) / m_XCount;
                    }
                    bool flag6 = maxWidth != 0f;
                    if (flag6)
                    {
                        m_MaxButtonWidth = (maxWidth - num) / m_XCount;
                    }
                }
                bool flag7 = m_MinButtonHeight == -1f;
                if (flag7)
                {
                    bool flag8 = minHeight != 0f;
                    if (flag8)
                    {
                        m_MinButtonHeight = (minHeight - num2) / rows;
                    }
                    bool flag9 = maxHeight != 0f;
                    if (flag9)
                    {
                        m_MaxButtonHeight = (maxHeight - num2) / rows;
                    }
                }
                bool flag10 = m_MinButtonHeight == -1f || m_MaxButtonHeight == -1f || m_MinButtonWidth == -1f || m_MaxButtonWidth == -1f;
                if (flag10)
                {
                    float num3 = 0f;
                    float num4 = 0f;
                    foreach (GUIContent guicontent in contents)
                    {
                        Vector2 vector = buttonStyle.CalcSize(guicontent);
                        num4 = Mathf.Max(num4, vector.x);
                        num3 = Mathf.Max(num3, vector.y);
                    }
                    bool flag11 = m_MinButtonWidth == -1f;
                    if (flag11)
                    {
                        bool flag12 = m_MaxButtonWidth != -1f;
                        if (flag12)
                        {
                            m_MinButtonWidth = Mathf.Min(num4, m_MaxButtonWidth);
                        }
                        else
                        {
                            m_MinButtonWidth = num4;
                        }
                    }
                    bool flag13 = m_MaxButtonWidth == -1f;
                    if (flag13)
                    {
                        bool flag14 = m_MinButtonWidth != -1f;
                        if (flag14)
                        {
                            m_MaxButtonWidth = Mathf.Max(num4, m_MinButtonWidth);
                        }
                        else
                        {
                            m_MaxButtonWidth = num4;
                        }
                    }
                    bool flag15 = m_MinButtonHeight == -1f;
                    if (flag15)
                    {
                        bool flag16 = m_MaxButtonHeight != -1f;
                        if (flag16)
                        {
                            m_MinButtonHeight = Mathf.Min(num3, m_MaxButtonHeight);
                        }
                        else
                        {
                            m_MinButtonHeight = num3;
                        }
                    }
                    bool flag17 = m_MaxButtonHeight == -1f;
                    if (flag17)
                    {
                        bool flag18 = m_MinButtonHeight != -1f;
                        if (flag18)
                        {
                            maxHeight = Mathf.Max(maxHeight, m_MinButtonHeight);
                        }
                        m_MaxButtonHeight = maxHeight;
                    }
                }
                minWidth = m_MinButtonWidth * m_XCount + num;
                maxWidth = m_MaxButtonWidth * m_XCount + num;
                minHeight = m_MinButtonHeight * rows + num2;
                maxHeight = m_MaxButtonHeight * rows + num2;
            }
        }

        public static Rect GetRect(GUIContent[] contents, int xCount, GUIStyle style, GUILayoutOption[] options)
        {
            Rect rect = new Rect(0f, 0f, 0f, 0f);
            EventType type = Event.current.type;
            if (type != EventType.Layout)
            {
                if (type == EventType.Used)
                {
                    return kDummyRect;
                }
                rect = GUILayoutUtility.current.topLevel.GetNext().rect;
            }
            else
            {
                GUILayoutUtility.current.topLevel.Add(new GUIGridSizer(contents, xCount, style, options));
            }
            return rect;
        }

        private int rows
        {
            get
            {
                int num = m_Count / m_XCount;
                bool flag = m_Count % m_XCount != 0;
                if (flag)
                {
                    num++;
                }
                return num;
            }
        }

        private readonly int m_Count;

        private readonly float m_MaxButtonHeight = -1f;

        private readonly float m_MaxButtonWidth = -1f;

        private readonly float m_MinButtonHeight = -1f;

        private readonly float m_MinButtonWidth = -1f;

        private readonly int m_XCount;
    }
}
