using BepInEx;
using BepInEx.Configuration;
using BepisPlugins;
using HarmonyLib;
using Pngcs.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace Screencap
{
    /// <summary>
    /// Plugin for taking high quality screenshots with optional transparency.
    /// Brought to AI-Shoujo by essu - the local smug, benevolent modder.
    /// </summary>
    public partial class ScreenshotManager : BaseUnityPlugin
    {
        /// <summary>
        /// GUID of the plugin, use with BepInDependency
        /// </summary>
        public const string GUID = "com.bepis.bepinex.screenshotmanager";
        /// <summary>
        /// Version of the plugin, use with BepInDependency
        /// </summary>
        public const string Version = Metadata.PluginsVersion;

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

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

        private Material _matComposite;
        private Material _matScale;

        private ConfigEntry<int> CaptureWidth { get; set; }
        private ConfigEntry<int> CaptureHeight { get; set; }
        private static ConfigEntry<int> Downscaling { get; set; }
        private ConfigEntry<bool> Alpha { get; set; }
        private ConfigEntry<int> CustomShadowResolution { get; set; }
        private ConfigEntry<ShadowCascades> ShadowCascadeOverride { get; set; }
        private static ConfigEntry<DisableAOSetting> DisableAO { get; set; }
        private ConfigEntry<bool> ScreenshotMessage { get; set; }
        private static ConfigEntry<int> UIShotUpscale { get; set; }

        private ConfigEntry<KeyboardShortcut> KeyCaptureNormal { get; set; }
        private ConfigEntry<KeyboardShortcut> KeyCaptureRender { get; set; }

        private static string GetCaptureFilename()
        {
            var dir = Path.Combine(Paths.GameRootPath, "UserData", "cap");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir,
#if AI
                $"AI_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
#elif HS2
                $"HS2_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
#endif
                );
        }

        private void Awake()
        {
            CaptureWidth = Config.Bind("Rendered screenshots", "Screenshot width", Screen.width, new ConfigDescription("Screenshot width in pixels", new AcceptableValueRange<int>(1, 10000)));
            CaptureHeight = Config.Bind("Rendered screenshots", "Screenshot height", Screen.height, new ConfigDescription("Screenshot height in pixels", new AcceptableValueRange<int>(1, 10000)));
            Downscaling = Config.Bind("Rendered screenshots", "Upsampling ratio", 2, new ConfigDescription("Render the scene in x times larger resolution, then downscale it to the correct size. Improves screenshot quality at cost of more RAM usage and longer capture times.\n\nBE CAREFUL, SETTING THIS TOO HIGH CAN AND WILL CRASH THE GAME BY RUNNING OUT OF RAM.", new AcceptableValueRange<int>(1, 4)));
            Alpha = Config.Bind("Rendered screenshots", nameof(Alpha), true, new ConfigDescription("When capturing the screenshot make the background transparent. Only works if the background is a 2D image, not a 3D object like a map."));
            ScreenshotMessage = Config.Bind("General", "Show messages on screen", true, new ConfigDescription("Whether screenshot messages will be displayed on screen. Messages will still be written to the log."));

            CustomShadowResolution = Config.Bind("Rendered screenshots", "Shadow resolution override", 8192, new ConfigDescription("By default, shadow map resolution is computed from its importance on screen. Setting this to a value greater than zero will override that behavior. Please note that the shadow map resolution will still be capped by memory and hardware limits.", new AcceptableValueList<int>(0, 4096, 8192, 16384, 32768)));

            ShadowCascadeOverride = Config.Bind("Rendered screenshots", "Shadow cascade override", ShadowCascades.Four, new ConfigDescription("When capturing screenshots, different shadow cascade values may look better. Override it or keep the current value."));
            DisableAO = Config.Bind("Rendered screenshots", "Disable AO", DisableAOSetting.WhenUpsampling, new ConfigDescription("When capturing screenshots, upsampling can cause ambient occlusion to start banding and produce weird effects on the end image. Change this setting to disable AO when capturing the screenshot."));

            KeyCaptureNormal = Config.Bind("Hotkeys", "Capture normal screenshot", new KeyboardShortcut(KeyCode.F9), "Capture a screenshot \"as you see it\". Includes interface and such.");
            KeyCaptureRender = Config.Bind("Hotkeys", "Capture rendered screenshot", new KeyboardShortcut(KeyCode.F11), "Capture a rendered screenshot with no interface. Controlled by other settings.");

            UIShotUpscale = Config.Bind(
                "UI Screenshots", "Screenshot resolution multiplier",
                1,
                new ConfigDescription("Multiplies the UI screenshot resolution from the current game resolution by this amount.\nWarning: Some elements will still be rendered at the original resolution (most notably the interface).", new AcceptableValueRange<int>(1, 8), "Advanced"));

            var ab = AssetBundle.LoadFromMemory(Properties.Resources.composite);
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);

            Hooks.Apply();
        }

        /// <summary>
        /// Disable built-in screenshots
        /// </summary>
        private static class Hooks
        {
            public static void Apply()
            {
                var h = Harmony.CreateAndPatchAll(typeof(Hooks));

                var msvoType = Type.GetType("UnityEngine.Rendering.PostProcessing.MultiScaleVO, Unity.Postprocessing.Runtime");
                h.Patch(AccessTools.Method(msvoType, "PushAllocCommands"), transpiler: new HarmonyMethod(typeof(Hooks), nameof(AoBandingFix)));
            }

#if AI
            // Hook here instead of hooking GameScreenShot.Capture to not affect the Photo functionality
            [HarmonyPrefix, HarmonyPatch(typeof(AIProject.Scene.MapScene), "CaptureSS")]
            private static bool CaptureSSOverride() => false;
#elif HS2
            public static bool SoundWasPlayed;

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.Capture), typeof(string))]
            private static bool CaptureOverride()
            {
                SoundWasPlayed = true;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture), typeof(string))]
            private static bool CaptureOverride2()
            {
                SoundWasPlayed = true;
                return false;
            }
#endif

            // Separate screenshot class for the studio
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), nameof(Studio.GameScreenShot.Capture), typeof(string))]
            private static bool StudioCaptureOverride()
            {
                return false;
            }

            // Fix AO banding in downscaled screenshots
            private static IEnumerable<CodeInstruction> AoBandingFix(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var i in instructions)
                {
                    if (i.opcode == OpCodes.Ldc_I4_S)
                    {
                        if ((int)RenderTextureFormat.RHalf == Convert.ToInt32(i.operand))
                            i.operand = (sbyte)RenderTextureFormat.RFloat;
                        else if ((int)RenderTextureFormat.RGHalf == Convert.ToInt32(i.operand))
                            i.operand = (sbyte)RenderTextureFormat.RGFloat;
                    }
                    yield return i;
                }
            }
        }

        private void Update()
        {
            if (KeyCaptureNormal.Value.IsDown())
            {
                PlayCaptureSound();

                var path = GetCaptureFilename();
                ScreenCapture.CaptureScreenshot(path, UIShotUpscale.Value);
                StartCoroutine(WaitForEndOfFrameThen(() => LogScreenshotMessage("Writing normal screenshot to " + path.Substring(Paths.GameRootPath.Length))));
            }
            else if (KeyCaptureRender.Value.IsDown())
            {
                PlayCaptureSound();

                var alphaAllowed = SceneManager.GetActiveScene().name == "CharaCustom" || Application.productName == "StudioNEOV2";
                if (Alpha.Value && alphaAllowed)
                    StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(true)));
                else
                    StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(false)));
            }
        }

        private static void PlayCaptureSound()
        {
#if AI
            Singleton<Manager.Resources>.Instance.SoundPack.Play(AIProject.SoundPack.SystemSE.Photo);
#elif HS2
            if (Hooks.SoundWasPlayed)
                Hooks.SoundWasPlayed = false;
            else
                Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.photo);
#endif
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

        private void CaptureAndWrite(bool alpha)
        {
            Config.Reload();
            var result = Capture(CaptureWidth.Value, CaptureHeight.Value, Downscaling.Value, alpha);
            StartCoroutine(WriteTex(result, alpha));
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
                var ratio = Screen.height / (float)scaledHeight; //Use larger of width/height?
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

            _matComposite.SetTexture("_Overlay", mask);

            Graphics.Blit(colour, alpha, _matComposite);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(colour);

            ScaleTex(ref alpha, width, height, downscaling);

            return alpha;
        }

        private static IEnumerable<AmbientOcclusion> DisableAmbientOcclusion()
        {
            var aos = new List<AmbientOcclusion>();

            if (DisableAO.Value == DisableAOSetting.Always || DisableAO.Value == DisableAOSetting.WhenUpsampling && Downscaling.Value > 1)
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

        private void ScaleTex(ref RenderTexture rt, int width, int height, int downScaling)
        {
            if (downScaling > 1)
            {
                var resized = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                _matScale.SetVector("_KernelAndSize", new Vector4(downScaling, downScaling, width, height));
                Graphics.Blit(rt, resized, _matScale);
                RenderTexture.ReleaseTemporary(rt);
                rt = resized;    // Give em the ol' switcheroo
            }
        }

        private IEnumerator WriteTex(RenderTexture rt, bool alpha)
        {
            //Pull texture off of GPU
            var req = AsyncGPUReadback.Request(rt, 0, 0, rt.width, 0, rt.height, 0, 1, alpha ? TextureFormat.RGBA32 : TextureFormat.RGBAFloat);
            while (!req.done) yield return null;

            RenderTexture.ReleaseTemporary(rt);
            string path = GetCaptureFilename();

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
            // Setup postprocessing effects to work with the capture
            var aos = DisableAmbientOcclusion();

            // Do the capture
            var fmt = alpha ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
            var rt = RenderTexture.GetTemporary(width, height, 32, fmt, RenderTextureReadWrite.Default);

            var cam = Camera.main;

            var oldCf = cam.clearFlags;
            var oldBg = cam.backgroundColor;
            var oldRt = cam.targetTexture;
            var oldRtc = Camera.current.targetTexture;

            cam.clearFlags = alpha ? CameraClearFlags.SolidColor : oldCf;
            cam.backgroundColor = alpha ? new Color(0, 0, 0, 0) : oldBg;
            cam.targetTexture = rt;

            cam.Render();

            cam.clearFlags = oldCf;
            cam.backgroundColor = oldBg;
            cam.targetTexture = oldRt;
            Camera.current.targetTexture = oldRtc;

            // Restore postprocessing settings
            if (DisableAO.Value == DisableAOSetting.Always || DisableAO.Value == DisableAOSetting.WhenUpsampling && Downscaling.Value > 1)
                foreach (var ao in aos)
                    ao.enabled.value = true;

            return rt;
        }

        private void LogScreenshotMessage(string text)
        {
            if (ScreenshotMessage.Value)
                Logger.LogMessage(text);
            else
                Logger.LogInfo(text);
        }
    }
}