using UnityEngine;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUI
    {
        private class ScrollViewState
        {
            public Rect position;
            public Rect visibleRect;
            public Rect viewRect;
            public Vector2 scrollPosition;
            public bool apply;

            public void ScrollTo(Rect pos) =>
                ScrollTowards(pos, float.PositiveInfinity);

            public bool ScrollTowards(Rect pos, float maxDelta)
            {
                Vector2 needed = ScrollNeeded(pos);
                if (needed.sqrMagnitude >= 0.0001f)
                {
                    if (maxDelta != 0)
                    {
                        if (needed.magnitude > maxDelta)
                            needed = needed.normalized * maxDelta;
                        scrollPosition += needed;
                        apply = true;
                    }
                    return true;
                }
                return false;
            }

            private Vector2 ScrollNeeded(Rect pos)
            {
                Rect rect = visibleRect;
                rect.x += scrollPosition.x;
                rect.y += scrollPosition.y;
                float f = pos.width - visibleRect.width;
                if (f > 0)
                {
                    pos.width -= f;
                    pos.x += f * 0.5f;
                }
                f = pos.height - visibleRect.height;
                if (f > 0)
                {
                    pos.height -= f;
                    pos.y += f * 0.5f;
                }
                Vector2 needed = new Vector2(
                    pos.xMax > rect.xMax ? pos.xMax - rect.xMax : pos.xMin < rect.xMin ? pos.xMin - rect.xMin : 0,
                    pos.yMax > rect.yMax ? pos.yMax - rect.yMax : pos.yMin < rect.yMin ? pos.yMin - rect.yMin : 0);
                viewRect.width = Mathf.Max(viewRect.width, visibleRect.width);
                viewRect.height = Mathf.Max(viewRect.height, visibleRect.height);
                needed.x = Mathf.Clamp(needed.x, viewRect.xMin - scrollPosition.x, viewRect.xMax - visibleRect.width - scrollPosition.x);
                needed.y = Mathf.Clamp(needed.y, viewRect.yMin - scrollPosition.y, viewRect.yMax - visibleRect.height - scrollPosition.y);
                return needed;
            }
        }
    }
}
