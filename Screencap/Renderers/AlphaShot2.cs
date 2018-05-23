using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

//code by essu
namespace alphaShot
{
    public class AlphaShot2 : MonoBehaviour
    {
        private Material matBlackout = null;
        private Material matMask = null;
        private int col = Shader.PropertyToID("_TargetColour");

        void Awake()
        {
            var abd = Screencap.Properties.Resources.blackout;
            var ab = AssetBundle.LoadFromMemory(abd);
            matBlackout = new Material(ab.LoadAsset<Shader>("assets/blackout.shader"));
            matMask = new Material(ab.LoadAsset<Shader>("assets/alphamask.shader"));
            ab.Unload(false);
        }

        private class MaterialInfo   //pls no bully
        {
            public Material m = null;
            public Dictionary<int, Texture> tex_props = new Dictionary<int, Texture>();
            public Dictionary<int, float> float_props = new Dictionary<int, float>();
            public Dictionary<int, Color> col_props = new Dictionary<int, Color>();
        }

        public byte[] Capture(int ResolutionX, int ResolutionY, int DownscalingRate, bool CaptureAlpha)
        {
            var currentScene = SceneManager.GetActiveScene().name;
            if (CaptureAlpha && (currentScene == "CustomScene" || currentScene == "Studio")) return this.CaptureAlpha(ResolutionX, ResolutionY, DownscalingRate);
            else return CaptureOpaque(ResolutionX, ResolutionY, DownscalingRate);
        }

        private byte[] CaptureOpaque(int ResolutionX, int ResolutionY, int DownscalingRate)
        {
            var renderCam = Camera.main;
            var tt = renderCam.targetTexture;
            if (DownscalingRate > 1)
            {
                ResolutionX *= DownscalingRate;
                ResolutionY *= DownscalingRate;
            }
            var rta = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            renderCam.targetTexture = rt;
            var ss = new Texture2D(ResolutionX, ResolutionY, TextureFormat.RGB24, false);
            var rect = renderCam.rect;
            renderCam.rect = new Rect(0, 0, 1, 1);
            renderCam.Render();
            renderCam.rect = rect;
            RenderTexture.active = rt;
            ss.ReadPixels(new Rect(0, 0, ResolutionX, ResolutionY), 0, 0);
            renderCam.targetTexture = tt;
            RenderTexture.active = rta;
            RenderTexture.ReleaseTemporary(rt);

            byte[] ret = null;
            if (DownscalingRate > 1)
            {
                var texture2D4 = new Texture2D(ResolutionX, ResolutionY, TextureFormat.ARGB32, false);
                var pixels = ScaleUnityTexture.ScaleLanczos(ss.GetPixels32(), ss.width, ResolutionX, ResolutionY);
                texture2D4.SetPixels32(pixels);
                GameObject.Destroy(ss);
                ret = texture2D4.EncodeToPNG();
                GameObject.Destroy(texture2D4);
            }
            else
            {
                ret = ss.EncodeToPNG();
                GameObject.Destroy(ss);
            }
            

            return ret;
        }

        private byte[] CaptureAlpha(int ResolutionX, int ResolutionY, int DownscalingRate)
        {
            var main = Camera.main;

            if (DownscalingRate > 1)
            {
                ResolutionX *= DownscalingRate;
                ResolutionY *= DownscalingRate;
            }

            var baf = main.GetComponent<BloomAndFlares>();
            var baf_e = baf?.enabled;
            if (baf) baf.enabled = false;

            var vig = main.GetComponent<VignetteAndChromaticAberration>();
            var vig_e = vig?.enabled;
            if (vig) vig.enabled = false;

            var texture2D = PerformCapture(ResolutionX, ResolutionY, DownscalingRate, true);
            var texture2D2 = PerformCapture(ResolutionX, ResolutionY, DownscalingRate, false);

            var rt = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0, RenderTextureFormat.ARGB32);
            matMask.SetTexture("_Mask", texture2D);
            Graphics.Blit(texture2D2, rt, matMask);
            GameObject.Destroy(texture2D);
            GameObject.Destroy(texture2D2);
            var texture2D3 = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            texture2D3.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] ret = null;
            if (DownscalingRate > 1)
            {
                var pixels = ScaleUnityTexture.ScaleLanczos(texture2D3.GetPixels32(), texture2D3.width, ResolutionX, ResolutionY);
                GameObject.Destroy(texture2D3);
                var texture2D4 = new Texture2D(ResolutionX, ResolutionY, TextureFormat.ARGB32, false);
                texture2D4.SetPixels32(pixels);
                ret = texture2D4.EncodeToPNG();
                GameObject.Destroy(texture2D4);
            }
            else
            {
                ret = texture2D3.EncodeToPNG();
                GameObject.Destroy(texture2D3);
            }

            if (baf) baf.enabled = baf_e.Value;
            if (vig) vig.enabled = vig_e.Value;

            return ret;
        }

        public Texture2D PerformCapture(int ResolutionX, int ResolutionY, int DownscalingRate, bool captureAlphaOnly)
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
            Shader.SetGlobalColor(ChaShader._LineColorG, new Color(.5f, .5f, .5f, 0f));
            if (captureAlphaOnly)
            {
                renderCam.backgroundColor = Color.white;
                renderCam.renderingPath = RenderingPath.VertexLit;
                matBlackout.SetColor(col, Color.black);
                renderCam.RenderWithShader(matBlackout.shader, null);
            }
            else
            {
                renderCam.backgroundColor = Color.black;
                renderCam.renderingPath = RenderingPath.Forward;
                renderCam.Render();
            }
            Shader.SetGlobalColor(ChaShader._LineColorG, lc);
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
    }
}