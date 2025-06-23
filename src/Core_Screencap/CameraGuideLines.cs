using Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace Screencap
{
    internal class CameraGuideLines
    {
        private static Material _drawingMaterial;
        private static Material DrawingMaterial
        {
            get
            {
                if (!_drawingMaterial)
                {
                    // Unity has a built-in shader that is useful for drawing
                    // simple colored things.
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    _drawingMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    // Turn on alpha blending
                    _drawingMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    _drawingMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    _drawingMaterial.SetInt("_Cull", (int)CullMode.Off);
                    _drawingMaterial.SetInt("_ZWrite", 0);
                    _drawingMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                }

                return _drawingMaterial;
            }
        }

        /// <summary>
        /// Draws composition guide lines on screen based on current settings.
        /// Supports rule of thirds, golden ratio, and framing guides.
        /// Adjusts for different aspect ratios between screen and target resolution.
        /// Must run in OnGUI.
        /// </summary>
        /// <param name="types">Which guide lines to draw, can combine multiple flags.</param>
        /// <param name="thickness">The thickness of the guide lines in pixels.</param>
        /// <param name="captureResolutionX">Resolution of the capture window (not the screen)</param>
        /// <param name="captureResolutionY">Resolution of the capture window (not the screen)</param>
        public static void DrawGuideLines(CameraGuideLinesType types, int thickness, int captureResolutionX, int captureResolutionY)
        {
            // Calculate aspect ratios for proper guide positioning
            var desiredAspect = captureResolutionX / (float)captureResolutionY;
            var screenAspect = Screen.width / (float)Screen.height;

            int viewportWidth;
            int viewportHeight;
            int offsetX;
            int offsetY;

            // Handle cases where screen is wider than target
            if (screenAspect > desiredAspect)
            {
                viewportWidth = Mathf.RoundToInt(Screen.height * desiredAspect);
                viewportHeight = Screen.height;
                offsetX = Mathf.RoundToInt((Screen.width - viewportWidth) / 2f);
                offsetY = 0;

                if ((types & CameraGuideLinesType.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, offsetX, Screen.height));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - offsetX, 0, offsetX, Screen.height));
                }
            }
            else
            {
                viewportWidth = Screen.width;
                viewportHeight = Mathf.RoundToInt(Screen.width / desiredAspect);
                offsetX = 0;
                offsetY = Mathf.RoundToInt((Screen.height - viewportHeight) / 2f);

                if ((types & CameraGuideLinesType.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, Screen.width, offsetY));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, Screen.height - offsetY, Screen.width, offsetY));
                }
            }

            // Draw composition guides
            if ((types & CameraGuideLinesType.Border) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 1, thickness);

            if ((types & CameraGuideLinesType.GridThirds) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0.3333333f, thickness);

            if ((types & CameraGuideLinesType.GridPhi) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0.236f, thickness);

            if ((types & CameraGuideLinesType.CrossOut) != 0)
                DrawCrossingGuides(offsetX, offsetY, viewportWidth, viewportHeight, thickness);

            if ((types & CameraGuideLinesType.SideV) != 0)
                DrawSidevGuides(offsetX, offsetY, viewportWidth, viewportHeight, thickness);

            if ((types & CameraGuideLinesType.CenterLines) != 0)
                DrawGridGuides(offsetX, offsetY, viewportWidth, viewportHeight, 0, thickness);
        }

        /// <summary>
        /// Draws guide lines for composition based on specified ratios.
        /// Used for both rule of thirds (0.3333) and golden ratio (0.236) guides.
        /// </summary>
        /// <param name="offsetX">X offset from screen edge</param>
        /// <param name="offsetY">Y offset from screen edge</param>
        /// <param name="viewportWidth">Width of the visible area</param>
        /// <param name="viewportHeight">Height of the visible area</param>
        /// <param name="centerRatio">Ratio for guide placement (0.3333 for thirds, 0.236 for golden ratio)</param>
        /// <param name="thickness">Thickness of the drawn lines</param>
        private static void DrawGridGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, float centerRatio, int thickness)
        {
            // Calculate ratios for guide line placement:
            // For rule of thirds: centerRatio = 0.3333, resulting in 1/3 divisions
            // For golden ratio: centerRatio = 0.236, resulting in golden section divisions
            // sideRatio determines the position of the first line
            // secondRatio determines the position of the second line
            var sideRatio = (1 - centerRatio) / 2;
            var secondRatio = sideRatio + centerRatio;
            var halfThick = (int)(thickness / 2);

            // Calculate actual pixel positions for vertical guide lines
            var firstx = offsetX + Mathf.Max(viewportWidth * sideRatio - halfThick, 0);
            IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(firstx), offsetY, thickness, viewportHeight));
            if (centerRatio != 0)
            {
                var secondx = offsetX + Mathf.Min(viewportWidth * secondRatio - halfThick, viewportWidth - thickness);
                IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(secondx), offsetY, thickness, viewportHeight));
            }

            // Calculate actual pixel positions for horizontal guide lines
            var firsty = offsetY + Mathf.Max(viewportHeight * sideRatio - halfThick, 0);
            IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(firsty), viewportWidth, thickness));
            if (centerRatio != 0)
            {
                var secondy = offsetY + Mathf.Min(viewportHeight * secondRatio - halfThick, viewportHeight - thickness);
                IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(secondy), viewportWidth, thickness));
            }
        }

        private static void DrawCrossingGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, int thickness)
        {
            // Draw diagonal lines using GL
            GL.PushMatrix();
            DrawingMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(IMGUIUtils.TransparentBoxColor);

            // Diagonal from top-left to bottom-right
            var angle = Mathf.Atan2(viewportHeight, viewportWidth);
            var perpDist = thickness / 2f;
            var dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            var dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(offsetX + viewportWidth + dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(offsetX + viewportWidth - dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(offsetX - dx, offsetY - dy, 0);
            GL.Vertex3(offsetX + dx, offsetY + dy, 0);

            // Diagonal from top-right to bottom-left
            GL.Vertex3(offsetX + viewportWidth + dx, offsetY - dy, 0);
            GL.Vertex3(offsetX + viewportWidth - dx, offsetY + dy, 0);
            GL.Vertex3(offsetX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(offsetX + dx, offsetY + viewportHeight - dy, 0);

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawSidevGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, int thickness)
        {
            // Draw V-shaped guides from sides
            GL.PushMatrix();
            DrawingMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(IMGUIUtils.TransparentBoxColor);

            // Lines from left side to right center
            var rightCenterX = offsetX + viewportWidth;
            var centerY = offsetY + viewportHeight / 2f;

            // Top left to right center
            var angle = Mathf.Atan2(centerY - offsetY, rightCenterX - offsetX);
            var perpDist = thickness / 2f;
            var dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            var dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(offsetX + dx, offsetY + dy, 0);
            GL.Vertex3(offsetX - dx, offsetY - dy, 0);
            GL.Vertex3(rightCenterX - dx, centerY - dy, 0);
            GL.Vertex3(rightCenterX + dx, centerY + dy, 0);

            // Bottom left to right center
            angle = Mathf.Atan2(offsetY + viewportHeight - centerY, rightCenterX - offsetX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(offsetX + dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(offsetX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(rightCenterX - dx, centerY + dy, 0);
            GL.Vertex3(rightCenterX + dx, centerY - dy, 0);

            // Lines from right side to left center
            var leftCenterX = offsetX;

            // Top right to left center
            angle = Mathf.Atan2(centerY - offsetY, offsetX - rightCenterX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(rightCenterX + dx, offsetY + dy, 0);
            GL.Vertex3(rightCenterX - dx, offsetY - dy, 0);
            GL.Vertex3(leftCenterX - dx, centerY - dy, 0);
            GL.Vertex3(leftCenterX + dx, centerY + dy, 0);

            // Bottom right to left center
            angle = Mathf.Atan2(offsetY + viewportHeight - centerY, offsetX - rightCenterX);
            dx = perpDist * Mathf.Cos(angle + Mathf.PI / 2);
            dy = perpDist * Mathf.Sin(angle + Mathf.PI / 2);

            GL.Vertex3(rightCenterX + dx, offsetY + viewportHeight - dy, 0);
            GL.Vertex3(rightCenterX - dx, offsetY + viewportHeight + dy, 0);
            GL.Vertex3(leftCenterX - dx, centerY + dy, 0);
            GL.Vertex3(leftCenterX + dx, centerY - dy, 0);

            GL.End();
            GL.PopMatrix();
        }
    }
}
