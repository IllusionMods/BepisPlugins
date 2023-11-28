using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.GUI;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUI
    {
        private static int s_ScrollControlId;
        private static Stack<ScrollViewState> scrollViewStates = new Stack<ScrollViewState>();

        public static Il2CppSystem.DateTime nextScrollStepTime { get; set; } = Il2CppSystem.DateTime.Now;

        public static int scrollTroughSide { get; set; }

        public static void CleanupRoots()
        {
            s_Skin = null;
            try
            {
                GUIUtility.CleanupRoots();
            }
            catch
            {
            }
            GUILayoutUtility.CleanupRoots();
            GUISkin.CleanupRoots();
            GUIStyle.CleanupRoots();
        }

        public static int DoButtonGrid(Rect position, int selected, Il2CppReferenceArray<GUIContent> contents, Il2CppStringArray controlNames, int itemsPerRow, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, ToolbarButtonSize buttonSize, [Optional] Il2CppStructArray<bool> contentsEnabled)
        {
            GUIUtility.CheckOnGUI();
            int contentsLength = contents.Length;
            if (contentsLength == 0)
                return selected;
            if (itemsPerRow <= 0)
            {
                UnityEngine.Debug.LogWarning("You are trying to create a SelectionGrid with zero or less elements to be displayed in the horizontal direction. Set itemsPerRow to a positive value.");
                return selected;
            }
            if (contentsEnabled != null && contentsEnabled.Length != contentsLength)
                throw new ArgumentException("contentsEnabled");
            int yCount = contentsLength / itemsPerRow;
            if (contentsLength % itemsPerRow != 0)
                yCount++;
            float x = CalcTotalHorizSpacing(itemsPerRow, style, firstStyle, midStyle, lastStyle);
            float y = Mathf.Max(style.margin.top, style.margin.bottom) * (yCount - 1);
            float elemWidth = (position.width - x) / itemsPerRow;
            float elemHeight = (position.height - y) / yCount;
            if (style.fixedWidth != 0)
                elemWidth = style.fixedWidth;
            if (style.fixedHeight != 0)
                elemHeight = style.fixedHeight;
            Rect[] rects = CalcMouseRects(position, contents, itemsPerRow, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, false, buttonSize);
            bool guiEnabled = enabled;
            GUIStyle selectedStyle = null;
            int id = 0;
            for (int i = 0; i < contentsLength; i++)
            {
                bool doDisable = guiEnabled && contentsEnabled != null && !contentsEnabled[i];
                if (doDisable)
                    enabled = guiEnabled = false;
                Rect rect = rects[i];
                GUIContent content = contents[i];
                if (controlNames != null)
                    SetNextControlName(controlNames[i]);
                int controlID = GUIUtility.GetControlID(s_ButtonGridHash, FocusType.Passive, rect);
                if (i == selected)
                    id = controlID;
                switch (Event.current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.hotControl = controlID;
                            Event.current.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                            Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                            changed = true;
                            return i;
                        }
                        break;
                    case EventType.Repaint:
                        {
                            GUIStyle currentStyle = contentsLength != 1 ? i != 0 ? i != contentsLength - 1 ? midStyle : lastStyle : firstStyle : style;
                            bool containsMouse = rect.Contains(Event.current.mousePosition);
                            if (selected != i)
                            {
                                int hotControl = GUIUtility.hotControl;
                                bool isHotControl = hotControl == controlID;
                                currentStyle.Draw(rect, content, containsMouse && (isHotControl || hotControl == 0 && guiEnabled), isHotControl && guiEnabled, false, false);
                            }
                            else
                                selectedStyle = currentStyle;
                            if (containsMouse)
                            {
                                GUIUtility.mouseUsed = true;
                                if (!string.IsNullOrEmpty(content.tooltip))
                                    GUIStyle.SetMouseTooltip(content.tooltip, rect);
                            }
                        }
                        break;
                }
                if (doDisable)
                    enabled = guiEnabled = true;
            }
            if (selectedStyle != null)
            {
                Rect rect = rects[selected];
                GUIContent content = contents[selected];
                bool containsMouse = rect.Contains(Event.current.mousePosition);
                int hotControl = GUIUtility.hotControl;
                bool isHotControl = hotControl == id;
                bool doDisable = guiEnabled && contentsEnabled != null && !contentsEnabled[selected];
                if (doDisable)
                    enabled = guiEnabled = false;
                selectedStyle.Draw(rect, content, containsMouse && (isHotControl || hotControl == 0 && guiEnabled), isHotControl && guiEnabled, true, false);
                if (doDisable)
                    enabled = true;
            }
            return selected;
        }

        public static Il2CppStructArray<Rect> CalcMouseRects(Rect position, Il2CppReferenceArray<GUIContent> contents, int itemsPerRow, float elemWidth, float elemHeight, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, bool addBorders, ToolbarButtonSize buttonSize)
        {
            int contentsLength = contents.Length, xIndex = 0;
            float x = position.xMin, y = position.yMin;
            GUIStyle currentStyle = contentsLength > 1 ? firstStyle : style;
            Rect[] rects = new Rect[contentsLength];
            for (int i = 0; i < contentsLength; i++)
            {
                float width = 0;
                if (buttonSize == ToolbarButtonSize.Fixed)
                    width = elemWidth;
                else if (buttonSize == ToolbarButtonSize.FitToContents)
                    width = currentStyle.CalcSize(contents[i]).x;
                Rect rect = new Rect(x, y, width, elemHeight);
                if (addBorders)
                    rect = currentStyle.margin.Add(rect);
                rects[i] = GUIUtility.AlignRectToDevice(rect);
                GUIStyle nextStyle = i != contentsLength - 2 && i != itemsPerRow - 2 ? midStyle : lastStyle;
                x = rects[i].xMax + Mathf.Max(currentStyle.margin.right, nextStyle.margin.left);
                if (++xIndex >= itemsPerRow)
                {
                    xIndex = 0;
                    y += elemHeight + Mathf.Max(style.margin.top, style.margin.bottom);
                    x = position.xMin;
                    nextStyle = firstStyle;
                }
                currentStyle = nextStyle;
            }
            return rects;
        }

        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id, GUIStyle thumbExtent = null)
        {
            GUIUtility.CheckOnGUI();
            if (id == 0)
                id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);
            SliderHandler sliderHandler = new SliderHandler(position, value, size, start, end, slider, thumb, horiz, id);
            return sliderHandler.Handle();
        }

        public static float Scroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            GUIUtility.CheckOnGUI();
            int controlID = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);
            Rect sliderPosition, leftButtonRect, rightButtonRect;
            if (horiz)
            {
                sliderPosition = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
                leftButtonRect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                rightButtonRect = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                sliderPosition = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
                leftButtonRect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                rightButtonRect = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }
            value = Slider(sliderPosition, value, size, leftValue, rightValue, slider, thumb, horiz, controlID, null);
            if (ScrollerRepeatButton(controlID, leftButtonRect, leftButton))
                value -= (leftValue >= rightValue) ? -10 : 10;
            if (ScrollerRepeatButton(controlID, rightButtonRect, rightButton))
                value += (leftValue >= rightValue) ? -10 : 10;
            EventType eventType = Event.current.type;
            if (eventType == EventType.MouseUp && eventType == EventType.Used)
                s_ScrollControlId = 0;
            return leftValue < rightValue ?
                Mathf.Clamp(value, leftValue, rightValue - size) :
                Mathf.Clamp(value, rightValue, leftValue - size);
        }

        public static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool result = false;
            if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                result = s_ScrollControlId != scrollerID;
                s_ScrollControlId = scrollerID;
                if (result)
                    nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(250);
                else if (result = Il2CppSystem.DateTime.Now >= nextScrollStepTime)
                    nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(30);
                if (Event.current.type == EventType.Repaint)
                    InternalRepaintEditorWindow();
            }
            return result;
        }

        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            GUIUtility.CheckOnGUI();
            int controlID = GUIUtility.GetControlID(s_ScrollviewHash, FocusType.Passive);
            ScrollViewState scrollViewState = (ScrollViewState)GUIStateObjects.GetStateObject(typeof(ScrollViewState), controlID);
            if (scrollViewState.apply)
            {
                scrollPosition = scrollViewState.scrollPosition;
                scrollViewState.apply = false;
            }
            scrollViewState.position = position;
            scrollViewState.scrollPosition = scrollPosition;
            scrollViewState.visibleRect = scrollViewState.viewRect = viewRect;
            scrollViewState.visibleRect.width = position.width;
            scrollViewState.visibleRect.height = position.height;
            scrollViewStates.Push(scrollViewState);
            Rect screenRect = new Rect(position);
            Event currentEvent = Event.current;
            EventType eventType = currentEvent.type;
            if (eventType == EventType.Layout)
            {
                GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
            }
            else if (eventType != EventType.Used)
            {
                bool showVertical = alwaysShowVertical;
                bool showHorizontal = alwaysShowHorizontal;
                if (showHorizontal || viewRect.width > screenRect.width)
                {
                    scrollViewState.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                    screenRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                    showHorizontal = true;
                }
                if (showVertical || viewRect.height > screenRect.height)
                {
                    scrollViewState.visibleRect.width = position.width - verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                    screenRect.width -= verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                    showVertical = true;
                    if (!showHorizontal && viewRect.width > screenRect.width)
                    {
                        scrollViewState.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        screenRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        showHorizontal = true;
                    }
                }
                if (eventType == EventType.Repaint && background != GUIStyle.none)
                    background.Draw(position, position.Contains(currentEvent.mousePosition), false, showHorizontal && showVertical, false);
                if (showHorizontal && horizontalScrollbar != GUIStyle.none)
                    scrollPosition.x = HorizontalScrollbar(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, screenRect.width, horizontalScrollbar.fixedHeight), scrollPosition.x, Mathf.Min(screenRect.width, viewRect.width), 0, viewRect.width, horizontalScrollbar);
                else
                {
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    scrollPosition.x = horizontalScrollbar == GUIStyle.none ? Mathf.Clamp(scrollPosition.x, 0, Mathf.Max(viewRect.width - position.width, 0)) : 0;
                }
                if (showVertical && verticalScrollbar != GUIStyle.none)
                    scrollPosition.y = VerticalScrollbar(new Rect(screenRect.xMax + verticalScrollbar.margin.left, screenRect.y, verticalScrollbar.fixedWidth, screenRect.height), scrollPosition.y, Mathf.Min(screenRect.height, viewRect.height), 0, viewRect.height, verticalScrollbar);
                else
                {
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    scrollPosition.y = verticalScrollbar == GUIStyle.none ? Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(viewRect.height - position.height, 0)) : 0;
                }
            }
            GUIClip.Push(screenRect, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)), Vector2.zero, false);
            return scrollPosition;
        }

        public static void EndScrollView(bool handleScrollWheel)
        {
            GUIUtility.CheckOnGUI();
            ScrollViewState scrollViewState = scrollViewStates.Peek();
            GUIClip.Pop();
            scrollViewStates.Pop();
            if (handleScrollWheel)
            {
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.ScrollWheel && scrollViewState.position.Contains(currentEvent.mousePosition))
                {
                    Vector2 delta = currentEvent.delta;
                    scrollViewState.scrollPosition.x = Mathf.Clamp(scrollViewState.scrollPosition.x + delta.x * 20, 0, scrollViewState.viewRect.width - scrollViewState.visibleRect.width);
                    scrollViewState.scrollPosition.y = Mathf.Clamp(scrollViewState.scrollPosition.y + delta.y * 20, 0, scrollViewState.viewRect.height - scrollViewState.visibleRect.height);
                    if (scrollViewState.scrollPosition.x < 0)
                        scrollViewState.scrollPosition.x = 0;
                    if (scrollViewState.scrollPosition.y < 0)
                        scrollViewState.scrollPosition.y = 0;
                    scrollViewState.apply = true;
                    currentEvent.Use();
                }
            }
        }

        private static ScrollViewState GetTopScrollView() =>
            scrollViewStates.Count != 0 ? scrollViewStates.Peek() : null;

        public static void ScrollTo(Rect position) =>
            GetTopScrollView()?.ScrollTo(position);

        public static bool ScrollTowards(Rect position, float maxDelta) =>
            GetTopScrollView()?.ScrollTowards(position, maxDelta) ?? false;
    }
}
