using System;
using System.Linq;
using UnityEngine;

namespace Screencap
{
    internal static class Extensions
    {
        public static string GetDisplayName(this AlphaMode mode)
        {
            switch (mode)
            {
                case AlphaMode.None:
                    return "No";
#if HS2 || AI
                case AlphaMode.composite:
                    return "Composite";
#else
                case AlphaMode.blackout:
                    return "Cutout";
                case AlphaMode.rgAlpha:
                    return "Gradual";
#endif
                default:
                    return null;
            };
        }

        public static readonly AlphaMode AlphaModeMaxValue = (AlphaMode)Enum.GetValues(typeof(AlphaMode)).Cast<int>().Max();

        public static void ClearRenderTexture(this RenderTexture rt)
        {
            var origTex = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            RenderTexture.active = origTex;
        }

        public static Texture2D CopyToTexture2D(this RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = currentActiveRT;
            return tex;
        }
    }
}
