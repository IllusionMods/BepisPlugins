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
    }
}
