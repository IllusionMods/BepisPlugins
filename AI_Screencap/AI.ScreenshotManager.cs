using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepisPlugins;
using BepInEx;
using BepInEx.Configuration;
using Pngcs.Unity;
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
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInIncompatibility("Screencap")]
    [BepInIncompatibility("EdgeDestroyer")]
    public class ScreenshotManager : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.screenshotmanager";
        public const string PluginName = "Screenshot Manager";
        public const string Version = Metadata.PluginsVersion;

        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

        private Material _matComposite;
        private Material _matScale;

        private ConfigEntry<int> CaptureWidth { get; set; }
        private ConfigEntry<int> CaptureHeight { get; set; }
        private ConfigEntry<int> Downscaling { get; set; }
        private ConfigEntry<bool> Alpha { get; set; }
        private ConfigEntry<int> CustomShadowResolution { get; set; }

        private ConfigEntry<KeyboardShortcut> KeyCaptureNormal { get; set; }
        private ConfigEntry<KeyboardShortcut> KeyCaptureRender { get; set; }

        private static string GetCaptureFilename()
        {
            var dir = Path.Combine(Paths.GameRootPath, "UserData", "cap");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"AI_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png");
        }

        private void Awake()
        {
            CaptureWidth = Config.AddSetting("Rendered screenshots", "Screenshot width", Screen.width, new ConfigDescription("Screenshot width in pixels", new AcceptableValueRange<int>(1, 10000)));
            CaptureHeight = Config.AddSetting("Rendered screenshots", "Screenshot height", Screen.height, new ConfigDescription("Screenshot height in pixels", new AcceptableValueRange<int>(1, 10000)));
            Downscaling = Config.AddSetting("Rendered screenshots", "Upsampling ratio", 2, new ConfigDescription("Render the scene in x times larger resolution, then downscale it to the correct size. Improves screenshot quality at cost of more RAM usage and longer capture times.\n\nBE CAREFUL, SETTING THIS TOO HIGH CAN AND WILL CRASH THE GAME BY RUNNING OUT OF RAM.", new AcceptableValueRange<int>(1, 4)));
            Alpha = Config.AddSetting("Rendered screenshots", nameof(Alpha), true, new ConfigDescription("When capturing the screenshot make the background transparent. Only works if the background is a 2D image, not a 3D object like a map."));

            CustomShadowResolution = Config.AddSetting("Rendered screenshots", "Shadow resolution override", 8192, new ConfigDescription("By default, shadow map resolution is computed from its importance on screen. Setting this to a value greater than zero will override that behavior. Please note that the shadow map resolution will still be capped by memory and hardware limits.", new AcceptableValueList<int>(0, 4096, 8192, 16384, 32768)));

            KeyCaptureNormal = Config.AddSetting("Hotkeys", "Capture normal screenshot", new KeyboardShortcut(KeyCode.F9), "Capture a screenshot \"as you see it\". Includes interface and such.");
            KeyCaptureRender = Config.AddSetting("Hotkeys", "Capture rendered screenshot", new KeyboardShortcut(KeyCode.F11), "Capture a rendered screenshot with no interface. Controlled by other settings.");

            var ab = AssetBundle.LoadFromMemory(Properties.Resources.composite);
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);
        }

        private void Update()
        {
            if (KeyCaptureNormal.Value.IsDown())
            {
                var path = GetCaptureFilename();
                ScreenCapture.CaptureScreenshot(path);
                StartCoroutine(WaitForEndOfFrameThen(() => Logger.LogMessage("Writing normal screenshot to " + path.Substring(Paths.GameRootPath.Length))));
            }
            else if (KeyCaptureRender.Value.IsDown())
            {
                if (Alpha.Value)
                    StartCoroutine(WaitForEndOfFrameThen(Transparent));
                else
                    StartCoroutine(WaitForEndOfFrameThen(Opaque));
            }
        }

        private static IEnumerator WaitForEndOfFrameThen(Action a)
        {
            var sc = QualitySettings.shadowCascades;
            QualitySettings.shadowCascades = 0;

            yield return new WaitForEndOfFrame();
            a();

            QualitySettings.shadowCascades = sc;
        }

        private void Opaque()
        {
            OnPreCapture?.Invoke();

            Config.Reload();
            var width = CaptureWidth.Value;
            var height = CaptureHeight.Value;
            var downScaling = Downscaling.Value;

            var scaledWidth = width * downScaling;
            var scaledHeight = height * downScaling;

            var colour = Capture(scaledWidth, scaledHeight, false);

            ScaleTex(ref colour, width, height, downScaling);

            StartCoroutine(WriteTex(colour, false));

            OnPostCapture?.Invoke();
        }

        private void Transparent()
        {
            OnPreCapture?.Invoke();

            Config.Reload();
            var width = CaptureWidth.Value;
            var height = CaptureHeight.Value;
            var downScaling = Downscaling.Value;

            var scaledWidth = width * downScaling;
            var scaledHeight = height * downScaling;

            var colour = Capture(scaledWidth, scaledHeight, false);

            var ppl = Camera.main.gameObject.GetComponent<PostProcessLayer>();
            ppl.enabled = false;

            var m3D = SceneManager.GetActiveScene().GetRootGameObjects()[0].transform.Find("CustomControl/Map3D/p_ai_mi_createBG00_00").gameObject; //Disable background. Sinful, truly.
            m3D.SetActive(false);

            var mask = Capture(scaledWidth, scaledHeight, true);

            ppl.enabled = true;

            m3D.SetActive(true);

            var alpha = RenderTexture.GetTemporary(scaledWidth, scaledHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            _matComposite.SetTexture("_Overlay", mask);

            Graphics.Blit(colour, alpha, _matComposite);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(colour);

            ScaleTex(ref alpha, width, height, downScaling);

            StartCoroutine(WriteTex(alpha, true));

            OnPostCapture?.Invoke();
        }

        private static IEnumerable<AmbientOcclusion> DisableAmbientOcclusion()
        {
            var aos = new List<AmbientOcclusion>();
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

            Logger.LogMessage("Writing rendered screenshot to " + path.Substring(Paths.GameRootPath.Length));

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

        private RenderTexture Capture(int width, int height, bool alpha)
        {
            // Setup postprocessing effects
            var aos = DisableAmbientOcclusion();

            var lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                l.shadowCustomResolution = CustomShadowResolution.Value;

            // Do the capture
            var fmt = alpha ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
            var rt = RenderTexture.GetTemporary(width, height, 32, fmt, RenderTextureReadWrite.Default);

            var cam = Camera.main;

            var cf = cam.clearFlags;
            var bg = cam.backgroundColor;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = alpha ? new Color(0, 0, 0, 0) : Color.black;
            cam.targetTexture = rt;

            cam.Render();

            cam.clearFlags = cf;
            cam.backgroundColor = bg;
            cam.targetTexture = null;
            Camera.current.targetTexture = null;    //Well shit.

            // Restore postprocessing settings
            foreach (var l in lights)
                l.shadowCustomResolution = 0;

            foreach (var ao in aos)
                ao.enabled.value = true;

            return rt;
        }
    }
}
