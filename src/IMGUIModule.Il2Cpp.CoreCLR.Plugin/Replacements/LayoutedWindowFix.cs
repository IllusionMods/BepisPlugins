using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace IMGUIModule.Il2Cpp.CoreCLR.Replacements
{
    using UnityEngine;
    internal static class LayoutedWindowFix
    {
        public static void ApplyIfNeeded(List<IDetour> detours)
        {
            if(typeof(GUILayout).GetNestedType(nameof(GUILayout.LayoutedWindow), AccessTools.all) != null) return;

            Console.WriteLine("AAAAAAAAAAAAAAA");

            // No matter what method of patching is used this will crash because LayoutedWindow is missing and so the patching backend crashes on instructions using it before we can do anything about it
            // The only way around this is to cecil edit the interop assembly to stub out GUILayout.DoWindow beforehand, which is done in the Patcher project
            detours.Add(new Detour(from: AccessTools.Method(typeof(GUILayout), nameof(GUILayout.DoWindow)),
                                   to: AccessTools.Method(typeof(LayoutedWindowFix), nameof(LayoutedWindowFix.DoWindow))));
        }

        private static Rect DoWindow(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();
            LayoutedWindow layoutedWindow = new LayoutedWindow(func, screenRect, content, options, style);
            return GUI.Window(id, screenRect, (GUI.WindowFunction)layoutedWindow.DoWindow, content, style);
        }
        private sealed class LayoutedWindow
        {
            internal LayoutedWindow(GUI.WindowFunction f, Rect screenRect, GUIContent content, GUILayoutOption[] options, GUIStyle style)
            {
                this.m_Func = f;
                this.m_ScreenRect = screenRect;
                this.m_Options = options;
                this.m_Style = style;
            }

            public void DoWindow(int windowID)
            {
                GUILayoutGroup topLevel = GUILayoutUtility.current.topLevel;
                EventType type = Event.current.type;
                EventType eventType = type;
                if (eventType != EventType.Layout)
                {
                    topLevel.ResetCursor();
                }
                else
                {
                    topLevel.resetCoords = true;
                    topLevel.rect = this.m_ScreenRect;
                    bool flag = this.m_Options != null;
                    if (flag)
                    {
                        topLevel.ApplyOptions(this.m_Options);
                    }
                    topLevel.isWindow = true;
                    topLevel.windowID = windowID;
                    topLevel.style = this.m_Style;
                }
                this.m_Func.Invoke(windowID);
            }

            private readonly GUI.WindowFunction m_Func;

            private readonly GUILayoutOption[] m_Options;

            private readonly Rect m_ScreenRect;

            private readonly GUIStyle m_Style;
        }
    }
}
