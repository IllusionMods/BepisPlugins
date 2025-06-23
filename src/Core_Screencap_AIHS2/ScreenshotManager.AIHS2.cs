using BepInEx.Configuration;
using BepisPlugins;
using Pngcs.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace Screencap
{
    /// <summary>
    /// Brought to AI-Shoujo by essu - the local smug, benevolent modder.
    /// Tool Window ported from KK by SuitIThub
    /// </summary>
    public partial class ScreenshotManager
    {
        #region Config properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ConfigEntry<int> CustomShadowResolution { get; set; }
        public static ConfigEntry<ShadowCascades> ShadowCascadeOverride { get; set; }
        public static ConfigEntry<DisableAOSetting> DisableAO { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void InitializeGameSpecific()
        {
            CustomShadowResolution = Config.Bind(
                "Render Settings", "Shadow resolution override",
                8192,
                new ConfigDescription("By default, shadow map resolution is computed from its importance on screen. Setting this to a value greater than zero will override that behavior. Please note that the shadow map resolution will still be capped by memory and hardware limits.", new AcceptableValueList<int>(0, 4096, 8192, 16384, 32768)));

            ShadowCascadeOverride = Config.Bind(
                "Render Settings", "Shadow cascade override",
                ShadowCascades.Four,
                new ConfigDescription("When capturing screenshots, different shadow cascade values may look better. Override it or keep the current value."));

            DisableAO = Config.Bind(
                "Render Settings", "Disable AO",
                DisableAOSetting.WhenUpsampling,
                new ConfigDescription("When capturing screenshots, upsampling can cause ambient occlusion to start banding and produce weird effects on the end image. Change this setting to disable AO when capturing the screenshot."));
        }

        /// <summary>
        /// Specifies the number of shadow cascades to use for rendering. 
        /// When capturing screenshots, different shadow cascade values may look better.
        /// </summary>
        public enum ShadowCascades
        {
            /// <summary> Keep the current value. </summary>
            [System.ComponentModel.Description("Keep current value")]
            Off = -1,
            /// <summary> Force zero shadow cascades (turn them off). </summary>
            Zero = 0,
            /// <summary> Force two shadow cascades. </summary>
            Two = 2,
            /// <summary> Force four shadow cascades. </summary>
            Four = 4,
        }

        /// <summary>  
        /// Specifies the behavior for disabling Ambient Occlusion (AO) during screenshot capture.  
        /// </summary>  
        public enum DisableAOSetting
        {
            /// <summary> Always disable Ambient Occlusion regardless of other settings. </summary>  
            Always,
            /// <summary> Disable Ambient Occlusion only when upsampling is enabled to prevent artifacts. </summary>  
            WhenUpsampling,
            /// <summary> Keep the original game settings. </summary>  
            Never
        }

        #endregion

        #region Screenshot Handler

        private IEnumerator TakeRenderScreenshot(bool in3D)
        {
            FirePreCapture();

            var filename = GetUniqueFilename(in3D ? "3D-Render" : "Render", UseJpg.Value);
            LogScreenshotMessage(in3D ? "3D rendered" : "rendered", filename);
            PlayCaptureSound();

            var sc = QualitySettings.shadowCascades;

            if (ShadowCascadeOverride.Value != ShadowCascades.Off)
                QualitySettings.shadowCascades = (int)ShadowCascadeOverride.Value;

            var lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                l.shadowCustomResolution = CustomShadowResolution.Value;

            yield return new WaitForEndOfFrame();

            var alphaAllowed = SceneManager.GetActiveScene().name == "CharaCustom" || Constants.InsideStudio;
            var alpha = CaptureAlphaMode.Value != AlphaMode.None && alphaAllowed ? AlphaModeUtils.Default : AlphaMode.None;

            var output = !in3D ? CaptureRender(transparencyMode: alpha) : Do3DCapture(() => CaptureRender(transparencyMode: alpha));

            QualitySettings.shadowCascades = sc;

            foreach (var l in lights)
                l.shadowCustomResolution = 0;

            FirePostCapture();

            if (output != null)
                yield return WriteTex(output, alpha, filename);
        }

        private RenderTexture DoCaptureRender(int width, int height, int downscaling, AlphaMode transparencyMode)
        {
            return transparencyMode == AlphaMode.None ? CaptureOpaque(width, height, downscaling) : CaptureTransparent(width, height, downscaling);
        }

        /// <summary>
        /// Captures an opaque screenshot at specified resolution with optional upsampling.
        /// Handles depth of field adjustments for the capture.
        /// </summary>
        private static RenderTexture CaptureOpaque(int width, int height, int downscaling)
        {
            var scaledWidth = width * downscaling;
            var scaledHeight = height * downscaling;

            var cam = Camera.main.gameObject;
            var dof = cam.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
            float dofPrevBlurSize = 0;
            if (dof != null)
            {
                dofPrevBlurSize = dof.maxBlurSize;
                // Scale blur size proportionally with resolution to maintain consistent DoF effect
                // Higher resolution needs proportionally larger blur radius
                var ratio = Screen.height / (float)scaledHeight;
                dof.maxBlurSize *= ratio * downscaling;
            }

            var colour = CaptureScreen(scaledWidth, scaledHeight, false);

            if (downscaling > 1)
                colour = ScaleTex(colour, width, height, downscaling);

            if (dof != null)
            {
                dof.maxBlurSize = dofPrevBlurSize;
            }

            return colour;
        }

        /// <summary>
        /// Captures a transparent screenshot by disabling background and compositing alpha.
        /// Temporarily modifies scene settings to achieve transparency.
        /// </summary>
        private static RenderTexture CaptureTransparent(int width, int height, int downscaling)
        {
            var scaledWidth = width * downscaling;
            var scaledHeight = height * downscaling;

            var cam = Camera.main.gameObject;
            var dof = cam.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
            float dofPrevBlurSize = 0;
            if (dof != null)
            {
                dofPrevBlurSize = dof.maxBlurSize;
                var ratio = Screen.height / (float)scaledHeight; //Use larger of width/height?
                dof.maxBlurSize *= ratio * downscaling;
            }

            var colour = CaptureScreen(scaledWidth, scaledHeight, false);

            var ppl = cam.GetComponent<PostProcessLayer>();
            if (ppl != null) ppl.enabled = false;

            //Disable background. Sinful, truly.
            var bg = SceneManager.GetActiveScene().GetRootGameObjects()[0].transform.Find("CustomControl/Map3D/p_ai_mi_createBG00_00");
            GameObject m3D = null;
            if (bg != null) m3D = bg.gameObject;

            if (m3D != null) m3D.SetActive(false);

            if (dof != null)
            {
                dof.maxBlurSize = dofPrevBlurSize;
                if (dof.enabled) dof.enabled = false;
                else dof = null;
            }

            var mask = CaptureScreen(scaledWidth, scaledHeight, true);

            if (ppl != null) ppl.enabled = true;
            if (dof != null) dof.enabled = true;
            if (m3D != null) m3D.SetActive(true);

            var alpha = RenderTexture.GetTemporary(scaledWidth, scaledHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            // Prevent previous frames from bleeding through when rapid captures are taken
            Graphics.Blit(Texture2D.blackTexture, alpha);

            MatComposite.SetTexture("_Overlay", mask);

            Graphics.Blit(colour, alpha, MatComposite);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(colour);

            if (downscaling > 1)
                alpha = ScaleTex(alpha, width, height, downscaling);

            return alpha;
        }

        #endregion

        #region Image Processing

        private static Material _matComposite;
        private static Material _matScale;
        private static Material MatComposite
        {
            get
            {
                if (!_matComposite) LoadBundleComposite();
                return _matComposite;
            }
        }
        private static Material MatScale
        {
            get
            {
                if (!_matScale) LoadBundleComposite();
                return _matScale;
            }
        }

        /// <summary>
        /// Scales a render texture to the target resolution using custom shader.
        /// Used for downscaling high resolution captures to final output size.
        /// </summary>
        private static RenderTexture ScaleTex(Texture input, int width, int height, int downScaling)
        {
            var resized = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            // Pack downscaling parameters into a Vector4:
            // xy: downscaling factors for width and height
            // zw: final target dimensions
            // This format is required by the resize shader
            MatScale.SetVector("_KernelAndSize", new Vector4(downScaling, downScaling, width, height));
            Graphics.Blit(input, resized, MatScale);

            if (input is RenderTexture rtInput)
                RenderTexture.ReleaseTemporary(rtInput);
            else
                Destroy(input);

            return resized;    // Give em the ol' switcheroo
        }

        private static void LoadBundleComposite()
        {
            var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);
        }

        private static IEnumerable<AmbientOcclusion> DisableAmbientOcclusion()
        {
            var aos = new List<AmbientOcclusion>();

            // Disable ambient occlusion based on settings:
            // - Always: Disable regardless of other settings
            // - WhenUpsampling: Only disable when downscaling > 1 to prevent artifacts
            // - Never: Keep AO enabled
            // Returns list of disabled AO components to re-enable later
            if (DisableAO.Value == DisableAOSetting.Always || DisableAO.Value == DisableAOSetting.WhenUpsampling && DownscalingRate.Value > 1)
                foreach (var vol in FindObjectsOfType<PostProcessVolume>())
                {
                    if (vol.profile.TryGetSettings(out AmbientOcclusion ao))
                    {
                        if (!ao.enabled.value) continue;
                        ao.enabled.value = false;
                        aos.Add(ao);
                    }
                }

            return aos;
        }

        /// <summary>
        /// Writes the captured RenderTexture to a PNG file asynchronously.
        /// Handles both RGBA32 (transparent) and RGBAFloat (opaque) formats.
        /// </summary>
        private static IEnumerator WriteTex(RenderTexture rt, AlphaMode alpha, string filename)
        {
            if (UseJpg.Value)
            {
                // TODO Not async
                var t2d = rt.CopyToTexture2D();
                RenderTexture.ReleaseTemporary(rt);
                yield return null;
                var encoded = t2d.EncodeToJPG(JpgQuality.Value);
                GameObject.DestroyImmediate(t2d);
                yield return null;
                File.WriteAllBytes(filename, encoded);
            }
            else
            {
                // Pull texture off of GPU
                // Not available on KK/EC/KKS, possibly achievable with https://github.com/SlightlyMad/AsyncTextureReader instead
                var req = AsyncGPUReadback.Request(rt, 0, 0, rt.width, 0, rt.height, 0, 1, alpha != AlphaMode.None ? TextureFormat.RGBA32 : TextureFormat.RGBAFloat);
                while (!req.done) yield return null;

                RenderTexture.ReleaseTemporary(rt);

                //Write raw pixel data to a file
                //Uses pngcs Unity fork: https://github.com/andrew-raphael-lukasik/pngcs
                if (alpha != AlphaMode.None)
                {
                    using (var buffer = req.GetData<Color32>())
                        yield return PNG.WriteAsync(buffer.ToArray(), req.width, req.height, 8, true, false, filename);
                }
                else
                {
                    using (var buffer = req.GetData<Color>())
                        yield return PNG.WriteAsync(buffer.ToArray(), req.width, req.height, 8, false, false, filename);
                }
            }
        }

        private static RenderTexture CaptureScreen(int width, int height, bool alpha)
        {
            // Temporarily disable ambient occlusion to prevent artifacts
            var aos = DisableAmbientOcclusion();

            // Select appropriate render texture format:
            // - ARGB32 for transparent captures (alpha channel needed)
            // - Default for opaque captures (better color precision)
            var fmt = alpha ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
            var rt = RenderTexture.GetTemporary(width, height, 32, fmt, RenderTextureReadWrite.Default);

            var cam = Camera.main;

            // Store original camera settings to restore later
            var oldCf = cam.clearFlags;
            var oldBg = cam.backgroundColor;
            var oldRt = cam.targetTexture;
            var oldRtc = Camera.current.targetTexture;

            // Configure camera for capture:
            // - For transparent captures: Use solid color clear and transparent background
            // - For opaque captures: Keep original settings
            cam.clearFlags = alpha ? CameraClearFlags.SolidColor : oldCf;
            cam.backgroundColor = alpha ? new Color(0, 0, 0, 0) : oldBg;
            cam.targetTexture = rt;

            cam.Render();

            cam.clearFlags = oldCf;
            cam.backgroundColor = oldBg;
            cam.targetTexture = oldRt;
            Camera.current.targetTexture = oldRtc;

            // Restore postprocessing settings
            if (DisableAO.Value == DisableAOSetting.Always || DisableAO.Value == DisableAOSetting.WhenUpsampling && DownscalingRate.Value > 1)
                foreach (var ao in aos)
                    ao.enabled.value = true;

            return rt;
        }

        #endregion
    }
}