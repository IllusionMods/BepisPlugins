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

        private ConfigWrapper<int> CaptureWidth { get; set; }
        private ConfigWrapper<int> CaptureHeight { get; set; }
        private ConfigWrapper<int> Downscaling { get; set; }

        private static string GetCaptureFolder()
        {
            var dir = Path.Combine(Paths.GameRootPath, "UserData", "cap");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void Awake()
        {
            CaptureWidth = Config.Wrap(nameof(Screencap), nameof(CaptureWidth), "Capture width (px)", Screen.width);
            CaptureHeight = Config.Wrap(nameof(Screencap), nameof(CaptureHeight), "Capture height (px)", Screen.height);
            Downscaling = Config.Wrap(nameof(Screencap), nameof(Downscaling), "Downscaling, only takes effect when value is > 1", 1);
            
            var ab = AssetBundle.LoadFromMemory(Properties.Resources.composite);
            _matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            _matScale = new Material(ab.LoadAsset<Shader>("resize"));
            ab.Unload(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                StartCoroutine(WaitForEndOfFrameThen(Opaque));
            }
            else if (Input.GetKeyDown(KeyCode.F11))
            {
                StartCoroutine(WaitForEndOfFrameThen(Transparent));
            }
        }

        private static IEnumerator WaitForEndOfFrameThen(Action a)
        {
            yield return new WaitForEndOfFrame();
            a();
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

            var aos = DisableAmbientOcclusion();

            var colour = Capture(scaledWidth, scaledHeight, false);

            foreach (var ao in aos)
                ao.enabled.Override(true);

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

            var aos = DisableAmbientOcclusion();

            var colour = Capture(scaledWidth, scaledHeight, false);

            foreach (var ao in aos)
                ao.enabled.value = true;

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

            var path = Path.Combine(GetCaptureFolder(), $"AI_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png");
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

        private static RenderTexture Capture(int width, int height, bool alpha)
        {
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

            return rt;
        }
    }
}