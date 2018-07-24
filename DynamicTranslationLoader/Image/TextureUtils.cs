using System.IO;
using UnityEngine;

namespace DynamicTranslationLoader.Image
{
    internal class TextureUtils
    {
        internal static Texture2D MakeReadable(Texture tex)
        {
            var tmp = TextureToRenderTexture(tex);
            var readable = GetT2D(tmp);
            Object.DestroyImmediate(tmp);
            return readable;
        }

        internal static RenderTexture TextureToRenderTexture(Texture tex)
        {
            var tmp = new RenderTexture(tex.width, tex.height, 0);
            var rt = RenderTexture.active;
            Graphics.Blit(tex, tmp);
            RenderTexture.active = rt;
            return tmp;
        }

        internal static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        internal static void SaveTexR(RenderTexture renderTexture, string path)
        {
            var tex = GetT2D(renderTexture);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        internal static void SaveTex(Texture tex, string name, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = tmp;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(tex, tmp);
            SaveTexR(tmp, name);
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}
