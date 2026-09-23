using System;
using UnityEngine;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal class GUILayoutGroup : UnityEngine.GUILayoutGroup
    {
        public GUILayoutGroup() : base() { }
        public GUILayoutGroup(IntPtr pointer) : base(pointer) { }

        public new Rect PeekNext()
        {
            int cursor = m_Cursor;
            int count = entries.Count;
            if (cursor < count)
                return entries[cursor].rect;
            throw new ArgumentException(string.Concat(
                "Getting control ",
                cursor,
                "'s position in a group with only ",
                count,
                " controls when doing ",
                Event.current.rawType,
                "\nAborting"));
        }

        public new Rect GetLast()
        {
            int cursor = m_Cursor;
            if (cursor != 0)
            {
                int count = entries.Count;
                if (cursor <= count)
                    return entries[cursor - 1].rect;
                UnityEngine.Debug.LogError(string.Concat(
                                               "Getting control ",
                                               cursor,
                                               "'s position in a group with only ",
                                               count,
                                               " controls when doing ",
                                               Event.current.type));
            }
            else
                UnityEngine.Debug.LogError("You cannot call GetLast immediately after beginning a group.");
            return kDummyRect;
        }
    }
}
