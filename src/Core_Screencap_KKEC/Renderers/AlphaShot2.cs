using System;
using System.Linq;
using BepisPlugins;
using Screencap;
using UnityEngine;
using UnityEngine.SceneManagement;

//code by essu
namespace alphaShot
{
    internal class AlphaShot2 : MonoBehaviour
    {
        public static int GridSize = 10;
        public static float OffsetScale = 1f;
        public static float FovJitter = 0f;

        #region Materials

        private static Material _matScale;
        private static Material _matBlackout;
        private static Material _matMask;
        private static Material MatScale
        {
            get
            {
                if (!_matScale) LoadBundleBlackout();
                return _matScale;
            }
        }
        private static Material MatBlackout
        {
            get
            {
                if (!_matBlackout) LoadBundleBlackout();
                return _matBlackout;
            }
        }
        private static Material MatMask
        {
            get
            {
                if (!_matMask) LoadBundleBlackout();
                return _matMask;
            }
        }
        private static void LoadBundleBlackout()
        {
            var abd = ResourceUtils.GetEmbeddedResource("blackout.unity3d");
            var ab = AssetBundle.LoadFromMemory(abd);
            _matBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            _matMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            ab.Unload(false);
        }
#if !EC
        private static Material _matComposite;
        private static Material MatComposite
        {
            get
            {
                if (!_matComposite) LoadBundleComposite();
                return _matComposite;
            }
        }
        private static void LoadBundleComposite()
        {
            var compab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            _matComposite = new Material(compab.LoadAsset<Shader>("composite"));
            compab.Unload(false);
        }
#endif
        private static Material _matRgAlpha;

        private static Material MatRgAlpha
        {
            get
            {
                if (!_matRgAlpha) LoadBundleRgalpha();
                return _matRgAlpha;
            }
        }

        private static void LoadBundleRgalpha()
        {
            var rgbd = ResourceUtils.GetEmbeddedResource("rgalpha.unity3d");
            var rgab = AssetBundle.LoadFromMemory(rgbd);
            _matRgAlpha = new Material(rgab.LoadAsset<Shader>("rgAlpha2"));
            rgab.Unload(false);
        }

        #endregion

        public static RenderTexture LanczosTex(Texture input, int resolutionX, int resolutionY)
        {
            MatScale.SetVector("_KernelAndSize", new Vector4(5, 5, resolutionX, resolutionY));
            var rt = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(input, rt, MatScale);
            if (input is RenderTexture rtInput)
                RenderTexture.ReleaseTemporary(rtInput);
            else
                Destroy(input);
            RenderTexture.active = prev;
            return rt;
        }

        public RenderTexture CaptureTex(int resolutionX, int resolutionY, int downscalingRate, AlphaMode mode)
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
            int newWidth = resolutionX * downscalingRate;
            int newHeight = resolutionY * downscalingRate;

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
#if !EC
                    if (Constants.InsideStudio) return true;
#endif
                    var sceneName = SceneManager.GetActiveScene().name;
                    return sceneName == "CustomScene" || sceneName == "HEditScene" || sceneName == "HPlayScene";
                }

                if (!CanTakeAlpha())
                    mode = AlphaMode.None;
            }

            // TODO This sucks, needs to be turned into proper settings
            int gridCount = Math.Max(1, GridSize);
            float offsetScale = Math.Max(1f, OffsetScale);
            float fovJitter = Mathf.Clamp01(FovJitter); //TODO rather than messing with fov track the camera center point and make sure to always point at it, this gives bokeh for free with increased sample separation (basically larger sensor size)
            int sampleCount = gridCount * gridCount;
            var main = Camera.main;
            var orgFov = main.fieldOfView;
            var sums = new int[resolutionX * resolutionY * 4];
            var readback = new Texture2D(resolutionX, resolutionY, TextureFormat.ARGB32, false);

            for (int gy = 0; gy < gridCount; gy++)
            {
                for (int gx = 0; gx < gridCount; gx++)
                {
                    main.ResetProjectionMatrix();
                    if (!main.orthographic)
                    {
                        main.fieldOfView = orgFov + UnityEngine.Random.Range(-1f, 1f) * fovJitter;
                        main.ResetProjectionMatrix();
                    }

                    var matrix = main.projectionMatrix;
                    float offsetX = ((gx + 0.5f) / gridCount - 0.5f) * downscalingRate * offsetScale;
                    float offsetY = ((gy + 0.5f) / gridCount - 0.5f) * downscalingRate * offsetScale;
                    if (main.orthographic)
                    {
                        matrix[0, 3] += 2f * offsetX / newWidth;
                        matrix[1, 3] += 2f * offsetY / newHeight;
                    }
                    else
                    {
                        matrix[0, 2] += 2f * offsetX / newWidth;
                        matrix[1, 2] += 2f * offsetY / newHeight;
                    }
                    main.projectionMatrix = matrix;

                    var sample = CaptureSample(newWidth, newHeight, mode);
                    if (downscalingRate > 1)
                        sample = LanczosTex(sample, resolutionX, resolutionY);

                    AccumulateSample(sample, readback, sums);
                }
            }

            main.fieldOfView = orgFov;
            main.ResetProjectionMatrix();

            // Recover depth of field
            if (dof != null)
            {
                dof.maxBlurSize = orgBlurSize;
            }

            // TODO this is awful, needs to be done with a shader later
            var pixels = new Color32[resolutionX * resolutionY];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(
                    (byte)((sums[i * 4] + sampleCount / 2) / sampleCount),
                    (byte)((sums[i * 4 + 1] + sampleCount / 2) / sampleCount),
                    (byte)((sums[i * 4 + 2] + sampleCount / 2) / sampleCount),
                    (byte)((sums[i * 4 + 3] + sampleCount / 2) / sampleCount));
            }
            readback.SetPixels32(pixels);
            readback.Apply();

            var result = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            Graphics.Blit(readback, result);
            Destroy(readback);
            return result;
        }

        private RenderTexture CaptureSample(int newWidth, int newHeight, AlphaMode mode)
        {
            switch (mode)
            {
                case AlphaMode.None:
                    return CaptureOpaque(newWidth, newHeight);
                case AlphaMode.blackout:
                    return CaptureAlpha(newWidth, newHeight);
                case AlphaMode.rgAlpha:
                    return CaptureRgAlpha(newWidth, newHeight);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static void AccumulateSample(RenderTexture sample, Texture2D readback, int[] sums)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = sample;
            readback.ReadPixels(new Rect(0, 0, sample.width, sample.height), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(sample);

            var pixels = readback.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                sums[i * 4] += pixels[i].r;
                sums[i * 4 + 1] += pixels[i].g;
                sums[i * 4 + 2] += pixels[i].b;
                sums[i * 4 + 3] += pixels[i].a;
            }
        }

        private RenderTexture CaptureOpaque(int resolutionX, int resolutionY)
        {
            var renderCam = Camera.main;
            var tt = renderCam.targetTexture;
            var rta = RenderTexture.active;

            var rt = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            RenderTexture.active = rt;

            var rect = renderCam.rect;

            renderCam.targetTexture = rt;
            renderCam.rect = new Rect(0, 0, 1, 1);
            renderCam.Render();
            renderCam.rect = rect;

#if !EC
            // Handle studio picture frames by overlaying them on top of the full size image, before it's downscaled
            if (Constants.InsideStudio &&
                Studio.Studio.IsInstance() &&
                Studio.Studio.Instance.frameCtrl != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame.mainTexture != null &&
                Studio.Studio.Instance.frameCtrl.imageFrame.isActiveAndEnabled)
            {
                var frame = Studio.Studio.Instance.frameCtrl.imageFrame.mainTexture;
                MatComposite.SetTexture("_Overlay", frame);
                var prevrt = rt;
                rt = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                Graphics.Blit(prevrt, rt, MatComposite);
                RenderTexture.ReleaseTemporary(prevrt);
            }
#endif

            renderCam.targetTexture = tt;

            // Set alpha to solid opaque (Camera.Render gives messed up alpha channel)
            var rtout = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            Graphics.Blit(rt, rtout, BundleLoader.MatOpaque);
            RenderTexture.ReleaseTemporary(rt);

            RenderTexture.active = rta;
            return rtout;
        }

        #region rgAlpha

        private RenderTexture CaptureRgAlpha(int resolutionX, int resolutionY)
        {
            Camera main = Camera.main;

            #region Generate alpha mask image

            // Disable special effects that interfere with transparency
            var disableTypes = new[]
            {
                typeof(UnityStandardAssets.ImageEffects.BloomAndFlares),
                typeof(UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration),
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
                    var disablePostProcessTypes = new[]
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
            var rtR = PerformRgCapture(resolutionX, resolutionY, Color.red);
            var rtG = PerformRgCapture(resolutionX, resolutionY, Color.green);

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

            var rtAlpha = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            rtAlpha.ClearRenderTexture();

            MatRgAlpha.SetTexture("_green", rtG);
            Graphics.Blit(rtR, rtAlpha, MatRgAlpha);
            //System.IO.File.WriteAllBytes("e:\\alphaRTex.png", GetT2D(alphaRTex).EncodeToPNG());

            RenderTexture.ReleaseTemporary(rtR);
            RenderTexture.ReleaseTemporary(rtG);

            #endregion

            #region Combine color with alpha

            // Generate the actual color capture. All effect can be enabled during this since we already have the alpha mask ready.
            var texColor = PerformCapture(resolutionX, resolutionY, false);

            var rtOutput = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rtOutput;
            GL.Clear(false, true, new Color(0, 0, 0, 0));

            MatMask.SetTexture("_Mask", rtAlpha);
            Graphics.Blit(texColor, rtOutput, MatMask);

            RenderTexture.ReleaseTemporary(texColor);
            RenderTexture.ReleaseTemporary(rtAlpha);

            RenderTexture.active = prev;

            #endregion

            return rtOutput;
        }

        public RenderTexture PerformRgCapture(int resolutionX, int resolutionY, Color bg)
        {
            Camera main = Camera.main;
            RenderTexture active = RenderTexture.active;
            var targetTexture = main.targetTexture;
            var rect = main.rect;
            var backgroundColor = main.backgroundColor;
            var clearFlags = main.clearFlags;
            var temporary = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            temporary.ClearRenderTexture();

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

        private RenderTexture CaptureAlpha(int resolutionX, int resolutionY)
        {
            var main = Camera.main;

            var disableTypes = new[]
            {
                typeof(UnityStandardAssets.ImageEffects.BloomAndFlares),
                typeof(UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration),
                typeof(AmplifyColorEffect),
            };
            var disabled = main.gameObject.GetComponents<Behaviour>().Where(x => x.enabled && disableTypes.Contains(x.GetType())).ToArray();
            foreach (var comp in disabled) comp.enabled = false;

            var capture1 = PerformCapture(resolutionX, resolutionY, true);

            foreach (var comp in disabled) comp.enabled = true;

            var capture2 = PerformCapture(resolutionX, resolutionY, false);

            var rt = RenderTexture.GetTemporary(capture1.width, capture1.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            MatMask.SetTexture("_Mask", capture1);
            Graphics.Blit(capture2, rt, MatMask);
            RenderTexture.active = prev;

            RenderTexture.ReleaseTemporary(capture1);
            RenderTexture.ReleaseTemporary(capture2);

            return rt;
        }

        public RenderTexture PerformCapture(int resolutionX, int resolutionY, bool captureMask)
        {
            var renderCam = Camera.main;
            var targetTexture = renderCam.targetTexture;
            var rta = RenderTexture.active;
            var rect = renderCam.rect;
            var backgroundColor = renderCam.backgroundColor;
            var clearFlags = renderCam.clearFlags;
            var rtTemp = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);

            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.targetTexture = rtTemp;
            renderCam.rect = new Rect(0, 0, 1, 1);

            var lc = Shader.GetGlobalColor(ChaShader._LineColorG);
            if (captureMask)
            {
                Shader.SetGlobalColor(ChaShader._LineColorG, new Color(.5f, .5f, .5f, 0f));
                GL.Clear(true, true, Color.white);
                renderCam.backgroundColor = Color.white;
                renderCam.renderingPath = RenderingPath.VertexLit;
                renderCam.RenderWithShader(MatBlackout.shader, null);
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

            return rtTemp;
        }

        #endregion
    }
}
