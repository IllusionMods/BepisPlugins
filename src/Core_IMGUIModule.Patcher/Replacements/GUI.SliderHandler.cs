using UnityEngine;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUI
    {
        private struct SliderHandler
        {
            private readonly Rect position;
            private readonly float currentValue;
            private readonly float size;
            private readonly float start;
            private readonly float end;
            private readonly GUIStyle slider;
            private readonly GUIStyle thumb;
            private readonly bool horiz;
            private readonly int id;

            public SliderHandler(Rect position, float currentValue, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
            {
                this.position = position;
                this.currentValue = currentValue;
                this.size = size;
                this.start = start;
                this.end = end;
                this.slider = slider;
                this.thumb = thumb;
                this.horiz = horiz;
                this.id = id;
            }

            public float Handle()
            {
                if (slider != null && thumb != null)
                {
                    switch (CurrentEventType())
                    {
                        case EventType.MouseDown:
                            return OnMouseDown();
                        case EventType.MouseUp:
                            return OnMouseUp();
                        case EventType.MouseDrag:
                            return OnMouseDrag();
                        case EventType.Repaint:
                            return OnRepaint();
                    }
                }
                return currentValue;
            }

            private float OnMouseDown()
            {
                Event currentEvent = Event.current;
                Vector2 mousePosition = currentEvent.mousePosition;
                if (!position.Contains(mousePosition) || IsEmptySlider())
                    return currentValue;
                scrollTroughSide = 0;
                GUIUtility.hotControl = id;
                currentEvent.Use();
                if (ThumbSelectionRect().Contains(mousePosition))
                {
                    StartDraggingWithValue(ClampedCurrentValue());
                    return currentValue;
                }
                UnityEngine.GUI.changed = true;
                if (SupportsPageMovements())
                {
                    SliderState().isDragging = false;
                    nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(250);
                    scrollTroughSide = CurrentScrollTroughSide();
                    return PageMovementValue();
                }
                float value = ValueForCurrentMousePosition();
                StartDraggingWithValue(value);
                return Clamp(value);
            }

            private float OnMouseDrag()
            {
                SliderState sliderState;
                if (GUIUtility.hotControl != id || !(sliderState = SliderState()).isDragging)
                    return currentValue;
                UnityEngine.GUI.changed = true;
                Event.current.Use();
                float value = MousePosition() - sliderState.dragStartPos;
                value = sliderState.dragStartValue + value / ValuesPerPixel();
                return Clamp(value);
            }

            private float OnMouseUp()
            {
                if (GUIUtility.hotControl == id)
                {
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
                return currentValue;
            }

            private float OnRepaint()
            {
                slider.Draw(position, GUIContent.none, id);
                if (!IsEmptySlider() && currentValue >= MinValue() && currentValue <= MaxValue())
                    thumb.Draw(ThumbRect(), GUIContent.none, id);
                Vector2 mousePosition = Event.current.mousePosition;
                if (GUIUtility.hotControl != id || !position.Contains(mousePosition) || IsEmptySlider())
                    return currentValue;
                if (ThumbRect().Contains(mousePosition))
                {
                    if (scrollTroughSide != 0)
                        GUIUtility.hotControl = 0;
                    return currentValue;
                }
                UnityEngine.GUI.InternalRepaintEditorWindow();
                if (Il2CppSystem.DateTime.Now < nextScrollStepTime || CurrentScrollTroughSide() != scrollTroughSide)
                    return currentValue;
                nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(30);
                if (SupportsPageMovements())
                {
                    SliderState().isDragging = false;
                    UnityEngine.GUI.changed = true;
                    return PageMovementValue();
                }
                return ClampedCurrentValue();
            }

            private EventType CurrentEventType() => Event.current.GetTypeForControl(id);

            private int CurrentScrollTroughSide()
            {
                Vector2 mousePosition = Event.current.mousePosition;
                Rect thumbRect = ThumbRect();
                return (!horiz ? mousePosition.y <= thumbRect.y : mousePosition.x < thumbRect.x) ? -1 : 1;
            }

            private bool IsEmptySlider() => start == end;

            private bool SupportsPageMovements() => size != 0 && UnityEngine.GUI.usePageScrollbars;

            private float PageMovementValue() =>
                Clamp(currentValue + size * (start <= end && MousePosition() > PageUpMovementBound() ? 0.9f : -0.9f));

            private float PageUpMovementBound()
            {
                Rect thumbRect = ThumbRect();
                return horiz ? thumbRect.xMax - position.x : thumbRect.yMax - position.y;
            }

            private float ValueForCurrentMousePosition()
            {
                Rect thumbRect = ThumbRect();
                return (MousePosition() - (horiz ? thumbRect.width : thumbRect.height) * 0.5f) / ValuesPerPixel() + start - size * 0.5f;
            }

            private float Clamp(float value) => Mathf.Clamp(value, MinValue(), MaxValue());

            private Rect ThumbSelectionRect() => ThumbRect();

            private void StartDraggingWithValue(float dragStartValue)
            {
                SliderState sliderState = SliderState();
                sliderState.dragStartPos = MousePosition();
                sliderState.dragStartValue = dragStartValue;
                sliderState.isDragging = true;
            }

            private SliderState SliderState() =>
                (SliderState)GUIStateObjects.GetStateObject(typeof(SliderState), id);

            private Rect ThumbRect() => !horiz ? VerticalThumbRect() : HorizontalThumbRect();

            private Rect VerticalThumbRect()
            {
                RectOffset padding = slider.padding;
                float valuesPerPixel = ValuesPerPixel();
                float y = ClampedCurrentValue() - start;
                float h = size * valuesPerPixel;
                if (start >= end)
                {
                    y += size;
                    h = -h;
                }
                return new Rect(position.x + padding.left, y * valuesPerPixel + position.y + padding.top, position.width - padding.horizontal, h + ThumbSize());
            }

            private Rect HorizontalThumbRect()
            {
                RectOffset padding = slider.padding;
                float valuesPerPixel = ValuesPerPixel();
                float x = ClampedCurrentValue() - start;
                float y = position.y;
                float w = size * valuesPerPixel;
                float h = position.height;
                if (start < end)
                {
                    y += padding.top;
                    h -= padding.vertical;
                }
                else
                {
                    x += size;
                    w = -w;
                }
                return new Rect(x * valuesPerPixel + position.x + padding.left, y, w + ThumbSize(), h);
            }

            private float ClampedCurrentValue() => Clamp(currentValue);

            private float MousePosition()
            {
                Vector2 mousePosition = Event.current.mousePosition;
                return horiz ? mousePosition.x - position.x : mousePosition.y - position.y;
            }

            private float ValuesPerPixel()
            {
                RectOffset padding = slider.padding;
                return ((horiz ? position.width - padding.horizontal : position.height - padding.vertical) - ThumbSize()) / (end - start);
            }

            private float ThumbSize()
            {
                RectOffset padding = slider.padding;
                return horiz ?
                    thumb.fixedWidth == 0 ? padding.horizontal : thumb.fixedWidth :
                    thumb.fixedHeight == 0 ? padding.vertical : thumb.fixedHeight;
            }

            private float MaxValue() => Mathf.Max(start, end) - size;
            private float MinValue() => Mathf.Min(start, end);
        }
    }
}
