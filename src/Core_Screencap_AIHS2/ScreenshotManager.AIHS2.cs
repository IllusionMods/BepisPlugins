using BepInEx;
using BepInEx.Configuration;
using BepisPlugins;
using Pngcs.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private ConfigEntry<int> CustomShadowResolution { get; set; }
        private ConfigEntry<ShadowCascades> ShadowCascadeOverride { get; set; }
        private static ConfigEntry<DisableAOSetting> DisableAO { get; set; }

        private void InitializeGameSpecific()
        {
            var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);

            CustomShadowResolution = Config.Bind(
                "Rendered screenshots", "Shadow resolution override",
                8192,
                new ConfigDescription("By default, shadow map resolution is computed from its importance on screen. Setting this to a value greater than zero will override that behavior. Please note that the shadow map resolution will still be capped by memory and hardware limits.", new AcceptableValueList<int>(0, 4096, 8192, 16384, 32768)));

            ShadowCascadeOverride = Config.Bind(
                "Rendered screenshots", "Shadow cascade override",
                ShadowCascades.Four,
                new ConfigDescription("When capturing screenshots, different shadow cascade values may look better. Override it or keep the current value."));

            DisableAO = Config.Bind(
                "Rendered screenshots", "Disable AO",
                DisableAOSetting.WhenUpsampling,
                new ConfigDescription("When capturing screenshots, upsampling can cause ambient occlusion to start banding and produce weird effects on the end image. Change this setting to disable AO when capturing the screenshot."));
        }

        #endregion

        private void Update()
        {
            if (KeyGui.Value.IsDown())
            {
                uiShow = !uiShow;
                ResolutionXBuffer = ResolutionX.Value.ToString();
                ResolutionYBuffer = ResolutionY.Value.ToString();
            }
            else if (KeyCapture.Value.IsDown())
            {
                CaptureScreenshotNormal();
            }
            else if (KeyCaptureAlpha.Value.IsDown())
            {
                CaptureScreenshotRender();
            }
        }

        #region Screenshot Handler

        private void CaptureAndWrite(bool alpha, string capType)
        {
            Config.Reload();
            var result = Capture(ResolutionX.Value, ResolutionY.Value, DownscalingRate.Value, alpha);
            StartCoroutine(WriteTex(result, alpha, capType));
        }

        /// <summary>
        /// Capture the screen into a texture based on supplied arguments. Remember to RenderTexture.ReleaseTemporary the texture when done with it.
        /// </summary>
        /// <param name="width">Width of the resulting capture, after downscaling</param>
        /// <param name="height">Height of the resulting capture, after downscaling</param>
        /// <param name="downscaling">How much to oversize and then downscale. 1 for none.</param>
        /// <param name="transparent">Should the capture be transparent</param>
        public RenderTexture Capture(int width, int height, int downscaling, bool transparent)
        {
            try { OnPreCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            try
            {
                if (!transparent)
                    return CaptureOpaque(width, height, downscaling);
                else
                    return CaptureTransparent(width, height, downscaling);
            }
            finally
            {
                try { OnPostCapture?.Invoke(); }
                catch (Exception ex) { Logger.LogError(ex); }
            }
        }

        /// <summary>
        /// Captures an opaque screenshot at specified resolution with optional upsampling.
        /// Handles depth of field adjustments for the capture.
        /// </summary>
        private RenderTexture CaptureOpaque(int width, int height, int downscaling)
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

            ScaleTex(ref colour, width, height, downscaling);

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
        private RenderTexture CaptureTransparent(int width, int height, int downscaling)
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

            _matComposite.SetTexture("_Overlay", mask);

            Graphics.Blit(colour, alpha, _matComposite);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(colour);

            ScaleTex(ref alpha, width, height, downscaling);

            return alpha;
        }

        private void CaptureScreenshotNormal()
        {
            PlayCaptureSound();
            var path = GetUniqueFilename("UI");
            ScreenCapture.CaptureScreenshot(path, UIShotUpscale.Value);
            StartCoroutine(WaitForEndOfFrameThen(() => LogScreenshotMessage("Writing normal screenshot to " + path.Substring(Paths.GameRootPath.Length))));
        }

        private void CaptureScreenshotRender()
        {
            PlayCaptureSound();

            var alphaAllowed = SceneManager.GetActiveScene().name == "CharaCustom" || Constants.InsideStudio;
            if (CaptureAlphaMode.Value != AlphaMode.None && alphaAllowed)
                StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(true, "Render")));
            else
                StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(false, "Render")));
        }

        private IEnumerator WaitForEndOfFrameThen(Action a)
        {
            var sc = QualitySettings.shadowCascades;

            if (ShadowCascadeOverride.Value != ShadowCascades.Off)
                QualitySettings.shadowCascades = (int)ShadowCascadeOverride.Value;

            var lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                l.shadowCustomResolution = CustomShadowResolution.Value;

            yield return new WaitForEndOfFrame();
            a();

            QualitySettings.shadowCascades = sc;

            foreach (var l in lights)
                l.shadowCustomResolution = 0;
        }

        #endregion


        #region Image Processing

        private Material _matComposite;
        private Material _matScale;

        private enum ShadowCascades
        {
            Zero = 0,
            Two = 2,
            Four = 4,
            Off
        }

        private enum DisableAOSetting
        {
            Always,
            WhenUpsampling,
            Never
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
        /// Scales a render texture to the target resolution using custom shader.
        /// Used for downscaling high resolution captures to final output size.
        /// </summary>
        private void ScaleTex(ref RenderTexture rt, int width, int height, int downScaling)
        {
            if (downScaling > 1)
            {
                var resized = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                // Pack downscaling parameters into a Vector4:
                // xy: downscaling factors for width and height
                // zw: final target dimensions
                // This format is required by the resize shader
                _matScale.SetVector("_KernelAndSize", new Vector4(downScaling, downScaling, width, height));
                Graphics.Blit(rt, resized, _matScale);
                RenderTexture.ReleaseTemporary(rt);
                rt = resized;    // Give em the ol' switcheroo
            }
        }

        /// <summary>
        /// Writes the captured RenderTexture to a PNG file asynchronously.
        /// Handles both RGBA32 (transparent) and RGBAFloat (opaque) formats.
        /// </summary>
        private IEnumerator WriteTex(RenderTexture rt, bool alpha, string capType)
        {
            //Pull texture off of GPU
            var req = AsyncGPUReadback.Request(rt, 0, 0, rt.width, 0, rt.height, 0, 1, alpha ? TextureFormat.RGBA32 : TextureFormat.RGBAFloat);
            while (!req.done) yield return null;

            RenderTexture.ReleaseTemporary(rt);
            string path = GetUniqueFilename(capType);

            LogScreenshotMessage("Writing rendered screenshot to " + path.Substring(Paths.GameRootPath.Length));

            //Write raw pixel data to a file
            //Uses pngcs Unity fork: https://github.com/andrew-raphael-lukasik/pngcs
            if (alpha)
            {
                using (var buffer = req.GetData<Color32>())
                    yield return PNG.WriteAsync(buffer.ToArray(), req.width, req.height, 8, true, false, path);
            }
            else
            {
                using (var buffer = req.GetData<Color>())
                    yield return PNG.WriteAsync(buffer.ToArray(), req.width, req.height, 8, false, false, path);
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