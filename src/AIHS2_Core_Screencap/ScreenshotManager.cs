using ADV.Commands.Base;
using BepInEx;
using BepInEx.Configuration;
using BepisPlugins;
using HarmonyLib;
using Pngcs.Unity;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using static GameCursor;
using static Illusion.Utils;
using static UnityEngine.GUI;
using static UnityStandardAssets.ImageEffects.BloomOptimized;

namespace Screencap
{
    /// <summary>
    /// Plugin for taking high quality screenshots with optional transparency.
    /// Brought to AI-Shoujo by essu - the local smug, benevolent modder.
    /// Tool Window ported from KK by SuitIThub
    /// Provides features like:
    /// - Custom resolution screenshots
    /// - Transparency support
    /// - Upsampling for higher quality
    /// - Guide lines for composition
    /// - Saved resolution presets
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

        #region Config

        /// <summary>
        /// Maximum allowed screenshot resolution, depends on extreme resolution setting
        /// </summary>
        private int ScreenshotSizeMax => ResolutionAllowExtreme?.Value == true ? 15360 : 4096;
        
        /// <summary>
        /// Minimum allowed screenshot resolution
        /// </summary>
        private const int ScreenshotSizeMin = 2;

        /// <summary>
        /// Directory where screenshots are saved
        /// </summary>
        private readonly string screenshotDir = Path.Combine(Paths.GameRootPath, @"UserData\cap\");

        /// <summary>
        /// List of saved resolution presets
        /// </summary>
        private List<Vector2Int> savedResolutions = new List<Vector2Int>();

        private Queue<IEnumerator> screenshotQueue = new Queue<IEnumerator>();

        private bool isScreenShotProcessing = false;

        private ConfigEntry<int> CaptureWidth { get; set; }
        private ConfigEntry<int> CaptureHeight { get; set; }
        private ConfigEntry<bool> ResolutionAllowExtreme { get; set; }
        private static ConfigEntry<int> Downscaling { get; set; }
        private ConfigEntry<bool> Alpha { get; set; }
        private ConfigEntry<int> CustomShadowResolution { get; set; }
        private ConfigEntry<ShadowCascades> ShadowCascadeOverride { get; set; }
        private static ConfigEntry<DisableAOSetting> DisableAO { get; set; }
        private ConfigEntry<bool> ScreenshotMessage { get; set; }
        private static ConfigEntry<int> UIShotUpscale { get; set; }

        private ConfigEntry<KeyboardShortcut> KeyCaptureNormal { get; set; }
        private ConfigEntry<KeyboardShortcut> KeyCaptureRender { get; set; }
        private ConfigEntry<KeyboardShortcut> KeyGui { get; set; }

        private ConfigEntry<CameraGuideLinesMode> GuideLinesModes { get; set; }

        private ConfigEntry<int> GuideLineThickness { get; set; }

        private ConfigEntry<string> SavedResolutionsConfig { get; set; }

        /// <summary>
        /// Initializes plugin settings and configuration options.
        /// Sets up all configurable parameters like resolution limits, hotkeys,
        /// and screenshot behavior options.
        /// </summary>
        private void InitializeSettings()
        {
            ResolutionAllowExtreme = Config.Bind(
                "Rendered screenshots", "Allow extreme resolutions",
                false,
                new ConfigDescription("Raise maximum rendered screenshot resolution cap to 16k. Trying to take a screenshot too high above 4k WILL CRASH YOUR GAME. ALWAYS SAVE BEFORE ATTEMPTING A SCREENSHOT AND MONITOR RAM USAGE AT ALL TIMES. Changes take effect after restarting the game."));
            
            CaptureWidth = Config.Bind(
                "Rendered screenshots", "Screenshot width", 
                Screen.width, 
                new ConfigDescription("Screenshot width in pixels", new AcceptableValueRange<int>(ScreenshotSizeMin, ScreenshotSizeMax)));

            CaptureHeight = Config.Bind(
                "Rendered screenshots", "Screenshot height", 
                Screen.height, 
                new ConfigDescription("Screenshot height in pixels", new AcceptableValueRange<int>(ScreenshotSizeMin, ScreenshotSizeMax)));

            Downscaling = Config.Bind(
                "Rendered screenshots", "Upsampling ratio", 
                2, 
                new ConfigDescription("Render the scene in x times larger resolution, then downscale it to the correct size. Improves screenshot quality at cost of more RAM usage and longer capture times.\n\nBE CAREFUL, SETTING THIS TOO HIGH CAN AND WILL CRASH THE GAME BY RUNNING OUT OF RAM.", new AcceptableValueRange<int>(1, 4)));

            Alpha = Config.Bind(
                "Rendered screenshots", nameof(Alpha), 
                true, 
                new ConfigDescription("When capturing the screenshot make the background transparent. Only works if the background is a 2D image, not a 3D object like a map."));

            ScreenshotMessage = Config.Bind(
                "General", "Show messages on screen", 
                true, 
                new ConfigDescription("Whether screenshot messages will be displayed on screen. Messages will still be written to the log."));

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

            KeyCaptureNormal = Config.Bind(
                "Hotkeys", "Capture normal screenshot", 
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription("Capture a screenshot \"as you see it\". Includes interface and such."));

            KeyCaptureRender = Config.Bind(
                "Hotkeys", "Capture rendered screenshot", 
                new KeyboardShortcut(KeyCode.F11),
                new ConfigDescription("Capture a rendered screenshot with no interface. Controlled by other settings."));

            KeyGui = Config.Bind(
                "Hotkeys", "Open settings window",
                new KeyboardShortcut(KeyCode.F11, KeyCode.LeftShift),
                new ConfigDescription("Open a quick access window with the most common settings."));

            GuideLinesModes = Config.Bind(
                "General", "Camera guide lines",
                CameraGuideLinesMode.Framing | CameraGuideLinesMode.GridThirds,
                new ConfigDescription("Draws guide lines on the screen to help with framing rendered screenshots. The guide lines are not captured in the rendered screenshot.\nTo show the guide lines, open the quick access settings window.", null, "Advanced"));

            GuideLineThickness = Config.Bind(
                "General", "Guide lines thickness",
                1,
                new ConfigDescription("Thickness of the guide lines in pixels.", new AcceptableValueRange<int>(1, 5), "Advanced"));

            UIShotUpscale = Config.Bind(
                "UI Screenshots", "Screenshot resolution multiplier",
                1,
                new ConfigDescription("Multiplies the UI screenshot resolution from the current game resolution by this amount.\nWarning: Some elements will still be rendered at the original resolution (most notably the interface).", new AcceptableValueRange<int>(1, 8), "Advanced"));

            SavedResolutionsConfig = Config.Bind(
                "Rendered screenshots", "Saved Resolutions",
                string.Empty,
                new ConfigDescription("List of saved resolutions in JSON format.", null, "Advanced"));

            LoadSavedResolutions();
        }

        /// <summary>
        /// Loads previously saved screenshot resolution presets from config.
        /// Parses the saved string format "(width,height)" into Vector2Int values.
        /// </summary>
        private void LoadSavedResolutions()
        {
            if (!string.IsNullOrEmpty(SavedResolutionsConfig.Value))
            {
                savedResolutions = new List<Vector2Int>();

                // Regex pattern to match (x,y) format
                Regex regex = new Regex(@"\((\-?\d+),(\-?\d+)\)");

                foreach (Match match in regex.Matches(SavedResolutionsConfig.Value))
                {
                    int x = int.Parse(match.Groups[1].Value);
                    int y = int.Parse(match.Groups[2].Value);
                    savedResolutions.Add(new Vector2Int(x, y));
                }
            }
        }

        private void SaveSavedResolutions()
        {
            SavedResolutionsConfig.Value = "[" + string.Join(", ", savedResolutions.Select(v => $"({v.x},{v.y})")) + "]";
        }

        private void SaveCurrentResolution()
        {
            var resolution = new Vector2Int(CaptureWidth.Value, CaptureHeight.Value);
            if (!savedResolutions.Contains(resolution))
            {
                savedResolutions.Add(resolution);
                SaveSavedResolutions();
            }
        }

        private void DeleteResolution(Vector2Int resolution)
        {
            savedResolutions.Remove(resolution);
            SaveSavedResolutions();
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitializeSettings();

            CaptureWidth.SettingChanged += (sender, args) => CaptureWidthBuffer = CaptureWidth.Value.ToString();
            CaptureHeight.SettingChanged += (sender, args) => CaptureHeightBuffer = CaptureHeight.Value.ToString();

            var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);

            Hooks.Apply();
        }

        private void Update()
        {
            if (KeyGui.Value.IsDown())
            {
                uiShow = !uiShow;
                CaptureWidthBuffer = CaptureWidth.Value.ToString();
                CaptureHeightBuffer = CaptureHeight.Value.ToString();
            }
            else if (KeyCaptureNormal.Value.IsDown())
            {
                // async process for time consuming process
                screenshotQueue.Enqueue(CaptureScreenshotNormalCoroutine());
                if (!isScreenShotProcessing)
                    StartCoroutine(ProcessQueue());
            }
            else if (KeyCaptureRender.Value.IsDown())
            {
                // async process for time consuming process
                screenshotQueue.Enqueue(CaptureScreenshotRenderCoroutine());
                if (!isScreenShotProcessing)
                    StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            isProcessing = true;

            while (screenshotQueue.Count > 0)
            {
                var coroutine = screenshotQueue.Dequeue();
                yield return StartCoroutine(coroutine);
            }

            isProcessing = false;
        }
        #endregion

        /// <summary>
        /// Disable built-in screenshots
        /// </summary>
        private static class Hooks
        {
            public static void Apply()
            {
                var h = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

                var msvoType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.MultiScaleVO, Unity.Postprocessing.Runtime");
                h.Patch(AccessTools.Method(msvoType, "PushAllocCommands"), transpiler: new HarmonyMethod(typeof(Hooks), nameof(AoBandingFix)));
            }

#if AI
            // Hook here instead of hooking GameScreenShot.Capture to not affect the Photo functionality
            [HarmonyPrefix, HarmonyPatch(typeof(AIProject.Scene.MapScene), nameof(AIProject.Scene.MapScene.CaptureSS))]
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

        #region Sound Handler

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

        #endregion

        #region Screenshot Handler

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

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

            // initialize alpha background color
            Graphics.Blit(Texture2D.blackTexture, alpha);

            _matComposite.SetTexture("_Overlay", mask);

            Graphics.Blit(colour, alpha, _matComposite);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(colour);

            ScaleTex(ref alpha, width, height, downscaling);

            return alpha;
        }

        private IEnumerator CaptureScreenshotNormalCoroutine()
        {
            PlayCaptureSound();

            var path = GetCaptureFilename();

            ScreenCapture.CaptureScreenshot(path, UIShotUpscale.Value);

            yield return new WaitForEndOfFrame();

            LogScreenshotMessage("Writing normal screenshot to " + path.Substring(Paths.GameRootPath.Length));
        }

        private IEnumerator CaptureScreenshotRenderCoroutine()
        {
            PlayCaptureSound();

            var alphaAllowed = SceneManager.GetActiveScene().name == "CharaCustom" || Constants.InsideStudio;
            bool useAlpha = Alpha.Value && alphaAllowed;

            yield return WaitForEndOfFrameThen(() => CaptureAndWrite(useAlpha));
        }

        private void CaptureScreenshotNormal()
        {
            PlayCaptureSound();
            var path = GetCaptureFilename();
            ScreenCapture.CaptureScreenshot(path, UIShotUpscale.Value);
            StartCoroutine(WaitForEndOfFrameThen(() => LogScreenshotMessage("Writing normal screenshot to " + path.Substring(Paths.GameRootPath.Length))));
        }

        private void CaptureScreenshotRender()
        {
            PlayCaptureSound();

            var alphaAllowed = SceneManager.GetActiveScene().name == "CharaCustom" || Constants.InsideStudio;
            if (Alpha.Value && alphaAllowed)
                StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(true)));
            else
                StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(false)));
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

        #region File Handler
        
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
            if (DisableAO.Value == DisableAOSetting.Always || DisableAO.Value == DisableAOSetting.WhenUpsampling && Downscaling.Value > 1)
                foreach (var ao in aos)
                    ao.enabled.value = true;

            return rt;
        }
        
        #endregion

        #region GUI

        private readonly int uiWindowHash = GUID.GetHashCode();
        private Rect uiRect = new Rect(20, Screen.height / 2 - 150, 160, 223);
        private bool uiShow = false;
        private string CaptureWidthBuffer = "", CaptureHeightBuffer = "";

        /// <summary>
        /// Draws the screenshot settings GUI window.
        /// Includes controls for:
        /// - Resolution settings and presets
        /// - Upsampling options
        /// - Transparency toggle
        /// - Guide line settings
        /// - Screenshot capture buttons
        /// </summary>
        protected void OnGUI()
        {
            if (uiShow)
            {
                DrawGuideLines();

                IMGUIUtils.DrawSolidBox(uiRect);
                uiRect = GUILayout.Window(uiWindowHash, uiRect, WindowFunction, "Screenshot settings");
                IMGUIUtils.EatInputInRect(uiRect);
            }
        }

        /// <summary>
        /// Draws composition guide lines on screen based on current settings.
        /// Supports rule of thirds, golden ratio, and framing guides.
        /// Adjusts for different aspect ratios between screen and target resolution.
        /// </summary>
        private void DrawGuideLines()
        {
            // Calculate aspect ratios for proper guide positioning
            var desiredAspect = CaptureWidth.Value / (float)CaptureHeight.Value;
            var screenAspect = Screen.width / (float)Screen.height;

            // Handle cases where screen is wider than target
            if (screenAspect > desiredAspect)
            {
                var actualWidth = Mathf.RoundToInt(Screen.height * desiredAspect);
                var barWidth = Mathf.RoundToInt((Screen.width - actualWidth) / 2f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, barWidth, Screen.height));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - barWidth, 0, barWidth, Screen.height));
                }

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Border) != 0)
                {
                    // Draw border around the capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(barWidth, 0, actualWidth, GuideLineThickness.Value));
                    IMGUIUtils.DrawTransparentBox(new Rect(barWidth, Screen.height - GuideLineThickness.Value, actualWidth, GuideLineThickness.Value));
                    IMGUIUtils.DrawTransparentBox(new Rect(barWidth, 0, GuideLineThickness.Value, Screen.height));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - barWidth - GuideLineThickness.Value, 0, GuideLineThickness.Value, Screen.height));
                }

                // Draw composition guides
                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0)
                    DrawGuides(barWidth, 0, actualWidth, Screen.height, 0.3333333f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0)
                    DrawGuides(barWidth, 0, actualWidth, Screen.height, 0.236f);
            }
            else
            {
                var actualHeight = Mathf.RoundToInt(Screen.width / desiredAspect);
                var barHeight = Mathf.RoundToInt((Screen.height - actualHeight) / 2f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0)
                {
                    // Draw darkened areas for parts outside capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, 0, Screen.width, barHeight));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, Screen.height - barHeight, Screen.width, barHeight));
                }

                if ((GuideLinesModes.Value & CameraGuideLinesMode.Border) != 0)
                {
                    // Draw border around the capture area
                    IMGUIUtils.DrawTransparentBox(new Rect(0, barHeight, Screen.width, GuideLineThickness.Value));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, Screen.height - barHeight - GuideLineThickness.Value, Screen.width, GuideLineThickness.Value));
                    IMGUIUtils.DrawTransparentBox(new Rect(0, barHeight, GuideLineThickness.Value, actualHeight));
                    IMGUIUtils.DrawTransparentBox(new Rect(Screen.width - GuideLineThickness.Value, barHeight, GuideLineThickness.Value, actualHeight));
                }

                // Draw composition guides
                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0)
                    DrawGuides(0, barHeight, Screen.width, actualHeight, 0.3333333f);

                if ((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0)
                    DrawGuides(0, barHeight, Screen.width, actualHeight, 0.236f);
            }
        }

        /// <summary>
        /// Draws guide lines for composition based on specified ratios.
        /// Used for both rule of thirds (0.3333) and golden ratio (0.236) guides.
        /// </summary>
        /// <param name="offsetX">X offset from screen edge</param>
        /// <param name="offsetY">Y offset from screen edge</param>
        /// <param name="viewportWidth">Width of the visible area</param>
        /// <param name="viewportHeight">Height of the visible area</param>
        /// <param name="centerRatio">Ratio for guide placement (0.3333 for thirds, 0.236 for golden ratio)</param>
        private void DrawGuides(int offsetX, int offsetY, int viewportWidth, int viewportHeight, float centerRatio)
        {
            // Calculate ratios for guide line placement:
            // For rule of thirds: centerRatio = 0.3333, resulting in 1/3 divisions
            // For golden ratio: centerRatio = 0.236, resulting in golden section divisions
            // sideRatio determines the position of the first line
            // secondRatio determines the position of the second line
            var sideRatio = (1 - centerRatio) / 2;
            var secondRatio = sideRatio + centerRatio;

            // Calculate actual pixel positions for vertical guide lines
            var firstx = offsetX + viewportWidth * sideRatio;
            var secondx = offsetX + viewportWidth * secondRatio;
            IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(firstx), offsetY, GuideLineThickness.Value, viewportHeight));
            IMGUIUtils.DrawTransparentBox(new Rect(Mathf.RoundToInt(secondx), offsetY, GuideLineThickness.Value, viewportHeight));

            // Calculate actual pixel positions for horizontal guide lines
            var firsty = offsetY + viewportHeight * sideRatio;
            var secondy = offsetY + viewportHeight * secondRatio;
            IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(firsty), viewportWidth, GuideLineThickness.Value));
            IMGUIUtils.DrawTransparentBox(new Rect(offsetX, Mathf.RoundToInt(secondy), viewportWidth, GuideLineThickness.Value));
        }

        /// <summary>
        /// Logs a screenshot-related message to the game log.
        /// Uses message or info level based on user preferences.
        /// </summary>
        private void LogScreenshotMessage(string text)
        {
            if (ScreenshotMessage.Value)
                Logger.LogMessage(text);
            else
                Logger.LogInfo(text);
        }

        /// <summary>
        /// Draws the screenshot settings GUI window.
        /// Includes controls for:
        /// - Resolution settings and presets
        /// - Upsampling options
        /// - Transparency toggle
        /// - Guide line settings
        /// - Screenshot capture buttons
        /// </summary>
        private void WindowFunction(int windowID)
        {
            var titleStyle = new GUIStyle
            { 
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            };

            // Resolution settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Output resolution (W/H)", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    GUI.SetNextControlName("X");
                    CaptureWidthBuffer = GUILayout.TextField(CaptureWidthBuffer);

                    GUI.SetNextControlName("Y");
                    CaptureHeightBuffer = GUILayout.TextField(CaptureHeightBuffer);

                    var focused = GUI.GetNameOfFocusedControl();
                    // Update resolution values when:
                    // - Neither width nor height field is focused (user clicked away)
                    // - User pressed Enter/Return key
                    // Also clamps values to valid range and handles parsing errors
                    if (focused != "X" && focused != "Y" || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        if (!int.TryParse(CaptureWidthBuffer, out int x))
                            x = CaptureWidth.Value;
                        if (!int.TryParse(CaptureHeightBuffer, out int y))
                            y = CaptureHeight.Value;
                        CaptureWidthBuffer = (CaptureWidth.Value = Mathf.Clamp(x, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                        CaptureHeightBuffer = (CaptureHeight.Value = Mathf.Clamp(y, ScreenshotSizeMin, ScreenshotSizeMax)).ToString();
                    }
                }
                GUILayout.EndHorizontal();

                // Common aspect ratio buttons
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("1:1"))
                    {
                        var max = Mathf.Max(CaptureWidth.Value, CaptureHeight.Value);
                        CaptureWidth.Value = max;
                        CaptureHeight.Value = max;
                    }
                    if (GUILayout.Button("4:3"))
                    {
                        var max = Mathf.Max(CaptureWidth.Value, CaptureHeight.Value);
                        CaptureWidth.Value = max;
                        CaptureHeight.Value = Mathf.RoundToInt(max * (3f / 4f));
                    }
                    if (GUILayout.Button("16:9"))
                    {
                        var max = Mathf.Max(CaptureWidth.Value, CaptureHeight.Value);
                        CaptureWidth.Value = max;
                        CaptureHeight.Value = Mathf.RoundToInt(max * (9f / 16f));
                    }
                    if (GUILayout.Button("6:10"))
                    {
                        var max = Mathf.Max(CaptureWidth.Value, CaptureHeight.Value);
                        CaptureWidth.Value = Mathf.RoundToInt(max * (6f / 10f));
                        CaptureHeight.Value = max;
                    }
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Set to screen size"))
                {
                    CaptureWidth.Value = Screen.width;
                    CaptureHeight.Value = Screen.height;
                }

                if (GUILayout.Button("Rotate 90 degrees"))
                {
                    var currentX = CaptureWidth.Value;
                    CaptureWidth.Value = CaptureHeight.Value;
                    CaptureHeight.Value = currentX;
                }

                if (GUILayout.Button("Save current resolution"))
                {
                    SaveCurrentResolution();
                }
            }
            GUILayout.EndVertical();

            // Saved resolutions section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Saved Resolutions", titleStyle);
                foreach (var resolution in savedResolutions.ToList())
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button($"{resolution.x}x{resolution.y}"))
                        {
                            CaptureWidth.Value = resolution.x;
                            CaptureHeight.Value = resolution.y;
                        }
                        if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                        {
                            DeleteResolution(resolution);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            // Upsampling settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Screen upsampling rate", titleStyle);

                GUILayout.BeginHorizontal();
                {
                    int downscale = (int)System.Math.Round(GUILayout.HorizontalSlider(Downscaling.Value, 1, 4));

                    GUILayout.Label($"{downscale}x", new GUIStyle
                    {
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    }, GUILayout.ExpandWidth(false));
                    Downscaling.Value = downscale;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Transparency settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Transparent background", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle(!Alpha.Value, "No");
                    if (GUI.changed && val) Alpha.Value = false;

                    GUI.changed = false;
                    val = GUILayout.Toggle(Alpha.Value, "Yes");
                    if (GUI.changed && val) Alpha.Value = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Guide line settings section
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Guide lines", titleStyle);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Thickness", GUILayout.ExpandWidth(false));
                    GUILayout.Space(2);
                    GuideLineThickness.Value = (int)System.Math.Round(GUILayout.HorizontalSlider(GuideLineThickness.Value, 1, 5));
                    GUILayout.Label($"{GuideLineThickness.Value}px", new GUIStyle
                    {
                        normal = new GUIStyleState
                        {
                            textColor = Color.white
                        }
                    }, GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUI.changed = false;
                    var val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.Framing) != 0, "Frame");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.Framing : GuideLinesModes.Value & ~CameraGuideLinesMode.Framing;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.Border) != 0, "Border");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.Border : GuideLinesModes.Value & ~CameraGuideLinesMode.Border;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridThirds) != 0, "3rds");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridThirds : GuideLinesModes.Value & ~CameraGuideLinesMode.GridThirds;

                    GUI.changed = false;
                    val = GUILayout.Toggle((GuideLinesModes.Value & CameraGuideLinesMode.GridPhi) != 0, "Phi");
                    if (GUI.changed) GuideLinesModes.Value = val ? GuideLinesModes.Value | CameraGuideLinesMode.GridPhi : GuideLinesModes.Value & ~CameraGuideLinesMode.GridPhi;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Action buttons
            if (GUILayout.Button("Open screenshot dir"))
                Process.Start(screenshotDir);

            GUILayout.Space(10);
            if (GUILayout.Button($"Capture Normal ({KeyCaptureNormal.Value})"))
                CaptureScreenshotNormal();
            if (GUILayout.Button($"Capture Render ({KeyCaptureRender.Value})"))
                CaptureScreenshotRender();

            GUILayout.Space(2);
            GUILayout.Label("More in Plugin Settings");

            GUI.DragWindow();
        }

        /// <summary>
        /// Available modes for camera guide lines.
        /// Can be combined using flags.
        /// </summary>
        [Flags]
        private enum CameraGuideLinesMode
        {
            [Description("No guide lines")]
            None = 0,
            [Description("Cropped area")]
            Framing = 1 << 0,
            [Description("Rule of thirds")]
            GridThirds = 1 << 1,
            [Description("Golden ratio")]
            GridPhi = 1 << 2,
            [Description("Grid border")]
            Border = 1 << 3
        }

        #endregion
    }
}