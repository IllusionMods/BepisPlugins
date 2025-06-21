using System;
using System.ComponentModel;
using System.Linq;
using BepisPlugins;
using Screencap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;
#pragma warning disable 1591

//code by essu
namespace alphaShot
{
    [Obsolete("Avoid using AlphaShot2 directly since it might get changed in the future. Use ScreenshotManager.Capture instead", false)]
    public class AlphaShot2 : MonoBehaviour
    {
        private Material matScale;
        private Material matBlackout;
        private Material matMask;

        private Material matRgAlpha;
        private Material matComposite;

        private bool InStudio = false;

        private void Awake()
        {
            var abd = ResourceUtils.GetEmbeddedResource("blackout.unity3d");
            var ab = AssetBundle.LoadFromMemory(abd);
            matBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            matMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            matScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            ab.Unload(false);

            var rgbd = ResourceUtils.GetEmbeddedResource("rgalpha.unity3d");
            var rgab = AssetBundle.LoadFromMemory(rgbd);
            matRgAlpha = new Material(rgab.LoadAsset<Shader>("rgAlpha2"));
            rgab.Unload(false);

            var compab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            matComposite = new Material(compab.LoadAsset<Shader>("composite"));
            compab.Unload(false);

#if !EC
            InStudio = Constants.InsideStudio;
#endif
        }

        public RenderTexture LanczosTex(Texture input, int ResolutionX, int ResolutionY)
        {
            matScale.SetVector("_KernelAndSize", new Vector4(5, 5, ResolutionX, ResolutionY));
            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(input, rt, matScale);
            if (input is RenderTexture rtInput)
                RenderTexture.ReleaseTemporary(rtInput);
            else
                Destroy(input);
            RenderTexture.active = prev;
            return rt;
        }

        public RenderTexture CaptureTex(int ResolutionX, int ResolutionY, int DownscalingRate, AlphaMode mode)
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
            RenderTexture fullSizeCapture;
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

        private RenderTexture CaptureOpaque(int ResolutionX, int ResolutionY)
        {
            var renderCam = Camera.main;
            var tt = renderCam.targetTexture;
            var rta = RenderTexture.active;

            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            RenderTexture.active = rt;

            var rect = renderCam.rect;

            renderCam.targetTexture = rt;
            renderCam.rect = new Rect(0, 0, 1, 1);
            renderCam.Render();
            renderCam.rect = rect;
            
#if !EC
            // Handle studio picture frames by overlaying them on top of the full size image, before it's downscaled
            if (InStudio &&
                Studio.Studio.IsInstance() &&
                Studio.Studio.Instance.frameCtrl != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame.mainTexture != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame.isActiveAndEnabled)
            {
                var frame = Studio.Studio.Instance.frameCtrl.imageFrame.mainTexture;
                matComposite.SetTexture("_Overlay", frame);
                var prevrt = rt;
                rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                Graphics.Blit(prevrt, rt, matComposite);
                RenderTexture.ReleaseTemporary(prevrt);
            }
#endif
            
            renderCam.targetTexture = tt;

            // Get rid of alpha channel because it contains bad data
            // TODO This is ass, find a way to do this with only RTs
            var ss = new Texture2D(ResolutionX, ResolutionY, TextureFormat.RGB24, false);
            ss.ReadPixels(new Rect(0, 0, ResolutionX, ResolutionY), 0, 0);
            ss.Apply();

            Graphics.Blit(ss, rt);
            GameObject.DestroyImmediate(ss);

            RenderTexture.active = rta;
            return rt;
        }

        #region rgAlpha

        private RenderTexture CaptureRgAlpha(int ResolutionX, int ResolutionY)
        {
            Camera main = Camera.main;

            #region Generate alpha mask image

            // Disable special effects that interfere with transparency
            var disableTypes = new Type[]
            {
                typeof(BloomAndFlares),
                typeof(VignetteAndChromaticAberration),
                typeof(AmplifyColorEffect),
            };
            var disabled = main.gameObject.GetComponents<Behaviour>().Where(x => x.enabled && disableTypes.Contains(x.GetType())).ToArray();
            foreach (var comp in disabled) comp.enabled = false;
#if !EC     
            object[] disabledPostProcessing = null;
            // Check if PostProcessingRuntime assembly is present with reflection first
            if (AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).Any(nam => nam == "Unity.Postprocessing.Runtime" || nam == "PostProcessingRuntime"))
            {
                // Wrap everything that references the PostProcessingRuntime assembly in lambdas with captured values so that they get compiled into lazy loaded classes.
                // As long as this is not called then the code is not loaded and doesn't blow up with FileNotFoundException because of the missing assembly.
                new Action(() =>
                {
                    var disablePostProcessTypes = new Type[]
                    {
                        typeof(UnityEngine.Rendering.PostProcessing.Bloom),
                        typeof(UnityEngine.Rendering.PostProcessing.Vignette),
                        typeof(UnityEngine.Rendering.PostProcessing.Grain),
                        typeof(UnityEngine.Rendering.PostProcessing.ColorGrading)
                    };

                    var volume = GameObject.Find("PostProcessVolume");
                    if (volume != null)
                    {
                        var postProcessVolume = volume.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
                        if (postProcessVolume != null)
                        {
                            // Have to cast the array to object[] since it's outside of the lambda and if the assembly was missing it would blow up this method
                            disabledPostProcessing = postProcessVolume.profile.settings.Where(x => x.enabled && disablePostProcessTypes.Contains(x.GetType())).Cast<object>().ToArray();
                            foreach (UnityEngine.Rendering.PostProcessing.PostProcessEffectSettings comp in disabledPostProcessing)
                                comp.enabled.Override(false);
                        }
                    }
                }).Invoke();
            }
#endif

            // Do composite alpha captures. 2 color composite gives better effects than grayscale alpha, especially on edges and partially transparent things.
            var rtR = PerformRgCapture(ResolutionX, ResolutionY, Color.red);
            var rtG = PerformRgCapture(ResolutionX, ResolutionY, Color.green);

            // Re-enable all disabled effects. Do this ASAP in case something in this method crashes to not corrupt game state.
            foreach (var comp in disabled) comp.enabled = true;
#if !EC     // See if the assembly exists and there was something to do in the scene
            if (disabledPostProcessing != null)
            {
                new Action(() =>
                {
                    foreach (UnityEngine.Rendering.PostProcessing.PostProcessEffectSettings comp in disabledPostProcessing)
                        comp.enabled.Override(true);
                }).Invoke();
            }
#endif

            var rtAlpha = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            ClearRT(rtAlpha);

            matRgAlpha.SetTexture("_green", rtG);
            Graphics.Blit(rtR, rtAlpha, matRgAlpha);
            //System.IO.File.WriteAllBytes("e:\\alphaRTex.png", GetT2D(alphaRTex).EncodeToPNG());

            RenderTexture.ReleaseTemporary(rtR);
            RenderTexture.ReleaseTemporary(rtG);

            #endregion

            #region Combine color with alpha

            // Generate the actual color capture. All effect can be enabled during this since we already have the alpha mask ready.
            var texColor = PerformCapture(ResolutionX, ResolutionY, false);

            var rtOutput = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rtOutput;
            GL.Clear(false, true, new Color(0, 0, 0, 0));

            matMask.SetTexture("_Mask", rtAlpha);
            Graphics.Blit(texColor, rtOutput, matMask);

            RenderTexture.ReleaseTemporary(texColor);
            RenderTexture.ReleaseTemporary(rtAlpha);

            RenderTexture.active = prev;

            #endregion

            return rtOutput;
        }

        private static void ClearRT(RenderTexture rt)
        {
            var targetTexture = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            RenderTexture.active = targetTexture;
        }

        public static Texture2D GetT2D(RenderTexture renderTexture)
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
            var temporary = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
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

        private RenderTexture CaptureAlpha(int ResolutionX, int ResolutionY)
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

            var capture1 = PerformCapture(ResolutionX, ResolutionY, true);

            foreach (var comp in disabled) comp.enabled = true;

            var capture2 = PerformCapture(ResolutionX, ResolutionY, false);

            var rt = RenderTexture.GetTemporary(capture1.width, capture1.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            matMask.SetTexture("_Mask", capture1);
            Graphics.Blit(capture2, rt, matMask);
            RenderTexture.active = prev;

            RenderTexture.ReleaseTemporary(capture1);
            RenderTexture.ReleaseTemporary(capture2);

            return rt;
        }

        public RenderTexture PerformCapture(int ResolutionX, int ResolutionY, bool CaptureMask)
        {
            var renderCam = Camera.main;
            var targetTexture = renderCam.targetTexture;
            var rta = RenderTexture.active;
            var rect = renderCam.rect;
            var backgroundColor = renderCam.backgroundColor;
            var clearFlags = renderCam.clearFlags;
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

            RenderTexture.active = rta;
            renderCam.backgroundColor = backgroundColor;
            renderCam.clearFlags = clearFlags;

            return rt_temp;
        }

        #endregion
    }
}
