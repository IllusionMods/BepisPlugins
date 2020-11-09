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

        private bool InStudio = false;

        private void Awake()
        {
            var abd = Screencap.Properties.Resources.blackout;
            var ab = AssetBundle.LoadFromMemory(abd);
            matBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            matMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            matScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            ab.Unload(false);

            InStudio = SceneManager.GetActiveScene().name == "Studio";
        }

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

        public byte[] Capture(int ResolutionX, int ResolutionY, int DownscalingRate, bool Transparent)
        {
            var fullSizeCapture = CaptureTex(ResolutionX, ResolutionY, DownscalingRate, Transparent);
            var ret = fullSizeCapture.EncodeToPNG();
            Destroy(fullSizeCapture);
            return ret;
        }

        public Texture2D CaptureTex(int ResolutionX, int ResolutionY, int DownscalingRate, bool Transparent)
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
            Texture2D fullSizeCapture;
            int newWidth = ResolutionX * DownscalingRate;
            int newHeight = ResolutionY * DownscalingRate;
            float orgBlurSize = 0.0f;

            // Fix depth of field
            DepthOfField dof = (DepthOfField)Camera.main.gameObject.GetComponent(typeof(DepthOfField));
            if (dof != null)
            {
                orgBlurSize = dof.maxBlurSize;
                dof.maxBlurSize = newWidth * orgBlurSize / Screen.width;
            }

            if (Transparent && (InStudio
                || SceneManager.GetActiveScene().name == "CustomScene"
                || SceneManager.GetActiveScene().name == "HEditScene"
                || SceneManager.GetActiveScene().name == "HPlayScene"))
                fullSizeCapture = CaptureAlpha(newWidth, newHeight);
            else
                fullSizeCapture = CaptureOpaque(newWidth, newHeight);

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

        private Texture2D CaptureAlpha(int ResolutionX, int ResolutionY)
        {
            var main = Camera.main;

            var baf = main.GetComponent<BloomAndFlares>();
            bool baf_e = false;
            if (baf)
            {
                baf_e = baf.enabled;
                baf.enabled = false;
            }

            var vig = main.GetComponent<VignetteAndChromaticAberration>();
            bool vig_e = false;
            if (vig)
            {
                vig_e = vig.enabled;
                vig.enabled = false;
            }

            var ace = main.GetComponent<AmplifyColorEffect>();
            bool ace_e = false;
            if (ace)
            {
                ace_e = ace.enabled;
                ace.enabled = false;
            }

            var texture2D = PerformCapture(ResolutionX, ResolutionY, true);
            if (baf) baf.enabled = baf_e;
            if (vig) vig.enabled = vig_e;
            if (ace) ace.enabled = ace_e;

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
    }
}
