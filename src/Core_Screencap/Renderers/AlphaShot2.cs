using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

//code by essu
namespace alphaShot
{
    public class AlphaShot2 : MonoBehaviour
    {
        private Material matScale;
        private Material matBlackout;
        private Material matMask;

        private Material matRgAlpha;

        private bool InStudio = false;

        private void Awake()
        {
            var abd = Screencap.Properties.Resources.blackout;
            var ab = AssetBundle.LoadFromMemory(abd);
            matBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            matMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            matScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            ab.Unload(false);

            var rgbd = Screencap.Properties.Resources.rgalpha;
            var rgab = AssetBundle.LoadFromMemory(rgbd);
            matRgAlpha = new Material(rgab.LoadAsset<Shader>("rgAlpha2"));
            rgab.Unload(false);


            InStudio = SceneManager.GetActiveScene().name == "Studio";
        }

        [Obsolete]
        public byte[] Lanczos(Texture input, int ResolutionX, int ResolutionY)
        {
            var t2d = LanczosTex(input, ResolutionX, ResolutionY);
            var ret = t2d.EncodeToPNG();
            Destroy(t2d);
            return ret;
        }

        public Texture2D LanczosTex(Texture input, int ResolutionX, int ResolutionY)
        {
            matScale.SetVector("_KernelAndSize", new Vector4(5, 5, ResolutionX, ResolutionY));
            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(input, rt, matScale);
            DestroyImmediate(input);
            var t2d = new Texture2D(ResolutionX, ResolutionY, TextureFormat.ARGB32, false);
            t2d.ReadPixels(new Rect(0, 0, ResolutionX, ResolutionY), 0, 0, false);
            t2d.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return t2d;
        }

        [Obsolete]
        public byte[] Capture(int ResolutionX, int ResolutionY, int DownscalingRate, bool Transparent)
        {
            var fullSizeCapture = CaptureTex(ResolutionX, ResolutionY, DownscalingRate, Transparent ? AlphaMode.blackout : AlphaMode.None);
            var ret = fullSizeCapture.EncodeToPNG();
            Destroy(fullSizeCapture);
            return ret;
        }

        [Obsolete]
        public Texture2D CaptureTex(int ResolutionX, int ResolutionY, int DownscalingRate, bool Transparent)
        {
            return CaptureTex(ResolutionX, ResolutionY, DownscalingRate, Transparent ? AlphaMode.blackout : AlphaMode.None);
        }

        public Texture2D CaptureTex(int ResolutionX, int ResolutionY, int DownscalingRate, AlphaMode mode)
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
            Texture2D fullSizeCapture;
            int newWidth = ResolutionX * DownscalingRate;
            int newHeight = ResolutionY * DownscalingRate;

            // Fix depth of field
            float orgBlurSize = 0.0f;
            var dof = Camera.main.gameObject.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
            if (dof != null)
            {
                orgBlurSize = dof.maxBlurSize;
                dof.maxBlurSize = newWidth * orgBlurSize / Screen.width;
            }

            if (mode != AlphaMode.None)
            {
                bool CanTakeAlpha()
                {
                    if (InStudio) return true;
                    var sceneName = SceneManager.GetActiveScene().name;
                    return sceneName == "CustomScene" || sceneName == "HEditScene" || sceneName == "HPlayScene";
                }

                if (!CanTakeAlpha())
                    mode = AlphaMode.None;
            }

            switch (mode)
            {
                case AlphaMode.None:
                    fullSizeCapture = CaptureOpaque(newWidth, newHeight);
                    break;
                case AlphaMode.blackout:
                    fullSizeCapture = CaptureAlpha(newWidth, newHeight);
                    break;
                case AlphaMode.rgAlpha:
                    fullSizeCapture = CaptureRgAlpha(newWidth, newHeight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            // Recover depth of field
            if (dof != null)
            {
                dof.maxBlurSize = orgBlurSize;
            }

            if (DownscalingRate > 1)
                return LanczosTex(fullSizeCapture, ResolutionX, ResolutionY);

            return fullSizeCapture;
        }

        private Texture2D CaptureOpaque(int ResolutionX, int ResolutionY)
        {
            var renderCam = Camera.main;
            var tt = renderCam.targetTexture;
            var rta = RenderTexture.active;

            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            var ss = new Texture2D(ResolutionX, ResolutionY, TextureFormat.RGB24, false);
            var rect = renderCam.rect;

            renderCam.targetTexture = rt;
            renderCam.rect = new Rect(0, 0, 1, 1);
            renderCam.Render();
            renderCam.rect = rect;
            RenderTexture.active = rt;
            ss.ReadPixels(new Rect(0, 0, ResolutionX, ResolutionY), 0, 0);
            ss.Apply();
            renderCam.targetTexture = tt;
            RenderTexture.active = rta;
            RenderTexture.ReleaseTemporary(rt);

            return ss;
        }

        #region rgAlpha

        private Texture2D CaptureRgAlpha(int ResolutionX, int ResolutionY)
        {
            Camera main = Camera.main;
            var disableTypes = new Type[]
            {
                typeof(BloomAndFlares),
                typeof(VignetteAndChromaticAberration),
                typeof(AmplifyColorEffect),
            };
            var disabled = main.gameObject.GetComponents<Behaviour>().Where(x => x.enabled && disableTypes.Contains(x.GetType())).ToArray();
            foreach (var comp in disabled) comp.enabled = false;

            var r = PerformRgCapture(ResolutionX, ResolutionY, Color.red);
            var g = PerformRgCapture(ResolutionX, ResolutionY, Color.green);

            foreach (var comp in disabled) comp.enabled = true;

            var output = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32);
            ClearRT(output);

            matRgAlpha.SetTexture("_red", r);
            matRgAlpha.SetTexture("_green", g);

            Graphics.Blit(r, output, matRgAlpha);

            RenderTexture.ReleaseTemporary(r);
            RenderTexture.ReleaseTemporary(g);
            var result = GetT2D(output);
            // File.WriteAllBytes("e:\\result.png",result.EncodeToPNG());
            RenderTexture.ReleaseTemporary(output);
            return result;
        }

        //TODO: is this necessary?
        private static void ClearRT(RenderTexture rt)
        {
            var main = Camera.main;
            var targetTexture = RenderTexture.active;//main.targetTexture;
            //main.targetTexture = rt;
            RenderTexture.active = rt;

            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            //main.targetTexture = targetTexture;
            RenderTexture.active = targetTexture;

        }

        private static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        public RenderTexture PerformRgCapture(int ResolutionX, int ResolutionY, Color bg)
        {
            Camera main = Camera.main;
            RenderTexture active = RenderTexture.active;
            var targetTexture = main.targetTexture;
            var rect = main.rect;
            var backgroundColor = main.backgroundColor;
            var clearFlags = main.clearFlags;
            var temporary = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32);
            ClearRT(temporary);

            main.clearFlags = CameraClearFlags.Color;
            main.backgroundColor = bg;
            main.rect = new Rect(0f, 0f, 1f, 1f);
            main.targetTexture = temporary;

            main.Render();

            main.targetTexture = targetTexture;
            main.rect = rect;
            RenderTexture.active = active;
            main.backgroundColor = backgroundColor;
            main.clearFlags = clearFlags;
            return temporary;
        }

        #endregion

        #region blackout

        private Texture2D CaptureAlpha(int ResolutionX, int ResolutionY)
        {
            var main = Camera.main;

            var disableTypes = new Type[]
            {
                typeof(BloomAndFlares),
                typeof(VignetteAndChromaticAberration),
                typeof(AmplifyColorEffect),
            };
            var disabled = main.gameObject.GetComponents<Behaviour>().Where(x => x.enabled && disableTypes.Contains(x.GetType())).ToArray();
            foreach (var comp in disabled) comp.enabled = false;

            var texture2D = PerformCapture(ResolutionX, ResolutionY, true);

            foreach (var comp in disabled) comp.enabled = true;

            var texture2D2 = PerformCapture(ResolutionX, ResolutionY, false);

            var rt = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            matMask.SetTexture("_Mask", texture2D);
            Graphics.Blit(texture2D2, rt, matMask);
            Destroy(texture2D);
            Destroy(texture2D2);
            var texture2D3 = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            texture2D3.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            texture2D3.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return texture2D3;
        }

        public Texture2D PerformCapture(int ResolutionX, int ResolutionY, bool CaptureMask)
        {
            var renderCam = Camera.main;
            var targetTexture = renderCam.targetTexture;
            var rta = RenderTexture.active;
            var rect = renderCam.rect;
            var backgroundColor = renderCam.backgroundColor;
            var clearFlags = renderCam.clearFlags;
            var t2d = new Texture2D(ResolutionX, ResolutionY, TextureFormat.RGB24, false);
            var rt_temp = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);

            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.targetTexture = rt_temp;
            renderCam.rect = new Rect(0, 0, 1, 1);

            var lc = Shader.GetGlobalColor(ChaShader._LineColorG);
            if (CaptureMask)
            {
                Shader.SetGlobalColor(ChaShader._LineColorG, new Color(.5f, .5f, .5f, 0f));
                GL.Clear(true, true, Color.white);
                renderCam.backgroundColor = Color.white;
                renderCam.renderingPath = RenderingPath.VertexLit;
                renderCam.RenderWithShader(matBlackout.shader, null);
                Shader.SetGlobalColor(ChaShader._LineColorG, lc);
            }
            else
            {
                renderCam.backgroundColor = Color.black;
                renderCam.renderingPath = RenderingPath.Forward;
                renderCam.Render();
            }
            renderCam.targetTexture = targetTexture;
            renderCam.rect = rect;

            RenderTexture.active = rt_temp;
            t2d.ReadPixels(new Rect(0f, 0f, ResolutionX, ResolutionY), 0, 0);
            t2d.Apply();
            RenderTexture.active = rta;
            renderCam.backgroundColor = backgroundColor;
            renderCam.clearFlags = clearFlags;
            RenderTexture.ReleaseTemporary(rt_temp);

            return t2d;
        }

        #endregion

        public enum AlphaMode
        {
            [Description("No transparency")]
            None = 0,
            [Description("Cutout transparency (hard edges)")]
            blackout,
            [Description("Gradual transparency (has issues with some effects)")]
            rgAlpha
        }
    }
}
