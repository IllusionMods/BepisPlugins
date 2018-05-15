using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var main = Camera.main;
            var tt = main.targetTexture;
            ResolutionX *= DownscalingRate;
            ResolutionY *= DownscalingRate;
            var rta = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(ResolutionX, ResolutionY, 24);
            main.targetTexture = rt;
            var ss = new Texture2D(ResolutionX, ResolutionY, TextureFormat.RGB24, false);
            main.Render();
            RenderTexture.active = rt;
            ss.ReadPixels(new Rect(0, 0, ResolutionX, ResolutionY), 0, 0);
            main.targetTexture = tt;
            RenderTexture.active = rta;
            RenderTexture.ReleaseTemporary(rt);

            var texture2D4 = new Texture2D(ResolutionX, ResolutionY, TextureFormat.ARGB32, false);
            var pixels = ScaleUnityTexture.ScaleLanczos(ss.GetPixels32(), ss.width, ResolutionX, ResolutionY);
            texture2D4.SetPixels32(pixels);
            GameObject.Destroy(ss);
            byte[] ret = texture2D4.EncodeToPNG();
            GameObject.Destroy(texture2D4);

            return ret;
        }

        private byte[] CaptureAlpha(int ResolutionX, int ResolutionY, int DownscalingRate)
        {
            var main = Camera.main;

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

            var texture2D4 = new Texture2D(ResolutionX, ResolutionY, TextureFormat.ARGB32, false);
            var pixels = ScaleUnityTexture.ScaleLanczos(texture2D3.GetPixels32(), texture2D3.width, ResolutionX, ResolutionY);
            texture2D4.SetPixels32(pixels);
            GameObject.Destroy(texture2D3);
            byte[] ret = texture2D4.EncodeToPNG();

            GameObject.Destroy(texture2D4);

            if (baf) baf.enabled = baf_e.Value;
            if (vig) vig.enabled = vig_e.Value;

            StartCoroutine(RestoreSkinTexture());

            return ret;
        }

        IEnumerator RestoreSkinTexture()
        {
            yield return new WaitForEndOfFrame();
            foreach (var cc in GameObject.FindObjectsOfType<ChaControl>())
            {
                cc.SetBodyBaseMaterial();
                cc.SetFaceBaseMaterial();
            }
        }

        //TODO: Switch depending on shader type, clean this brute force shit up
        KeyValuePair<int, float>[] potentials = new[] {
                    new KeyValuePair<int, float>(Shader.PropertyToID("_MainTex"), 0f), //regular tex
                    new KeyValuePair<int, float>(Shader.PropertyToID("_NormalMap"), 0f), //Pantyhose
                    new KeyValuePair<int, float>(Shader.PropertyToID("_NormalMapDetail"), 0f),
                    new KeyValuePair<int, float>(Shader.PropertyToID("_HairGloss"), 0f), //Shader Forge/main_hair
                    new KeyValuePair<int, float>(Shader.PropertyToID("_overtex1"), 0f), //eye
                    new KeyValuePair<int, float>(Shader.PropertyToID("_overtex2"), 0f), //eye
                    new KeyValuePair<int, float>(Shader.PropertyToID("_overtex3"), 0f), //liquid
                    new KeyValuePair<int, float>(Shader.PropertyToID("_DetailMask"), 0f), //hair
                    new KeyValuePair<int, float>(Shader.PropertyToID("_GlassRamp"), 0f), //items/glasses
                    new KeyValuePair<int, float>(Shader.PropertyToID("_Texture2"), 0f), //liquid
                    new KeyValuePair<int, float>(Shader.PropertyToID("_Texture3"), 0f), //liquid
                    new KeyValuePair<int, float>(Shader.PropertyToID("_ColorMask"), 1f), //items/glasses
                };

        int[] potentials2 = new[] {
                    Shader.PropertyToID("_rimV"),  //Shader Forge/main_hair

                    Shader.PropertyToID("_SpecularPower"),  //skin shine
                    Shader.PropertyToID("_SpecularPowerNail"),  //skin shine

                    Shader.PropertyToID("_liquidmask"), //liquid
                };

        int[] potentials3 = new[] {
                    Shader.PropertyToID("_Color"),  //eye?
                    Shader.PropertyToID("_shadowcolor"),  //eye
                    Shader.PropertyToID("_overcolor1"),  //eye
                    Shader.PropertyToID("_overcolor2"),  //eye
                    Shader.PropertyToID("_overcolor3"),  //eye
                    Shader.PropertyToID("_SpecularColor"), //skin shine

                    Shader.PropertyToID("_ShadowColor"),  //hair
                    Shader.PropertyToID("_Color2"),  //hair
                    Shader.PropertyToID("_Color3"),  //hair
                    Shader.PropertyToID("_Color4"),  //items/glasses
                };

        public Texture2D PerformCapture(int ResolutionX, int ResolutionY, int DownscalingRate, bool captureAlphaOnly)
        {
            var renderCam = Camera.main;
            var targetTexture = renderCam.targetTexture;
            var rta = RenderTexture.active;
            var rect = renderCam.rect;
            var backgroundColor = renderCam.backgroundColor;
            var clearFlags = renderCam.clearFlags;
            var rw = ResolutionX * DownscalingRate;
            var rh = ResolutionY * DownscalingRate;
            var t2d = new Texture2D(rw, rh, TextureFormat.RGB24, false);
            var rt_temp = RenderTexture.GetTemporary(rw, rh, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);

            var list = new List<MaterialInfo>();
            var lc = Shader.GetGlobalColor(ChaShader._LineColorG);
            renderCam.backgroundColor = Color.black;

            if (captureAlphaOnly)
            {
                Shader.SetGlobalColor(ChaShader._LineColorG, new Color(.5f, .5f, .5f, 0f));

                renderCam.backgroundColor = Color.white;

                foreach (var smr in GameObject.FindObjectsOfType<Renderer>().Where(x => x.enabled && x.material != null))
                {
                    foreach (var m in smr.materials)
                    {
                        var p = new Dictionary<int, Texture>();
                        foreach (var potential in potentials)
                            if (m.HasProperty(potential.Key))
                            {
                                var gt = m.GetTexture(potential.Key);
                                p[potential.Key] = gt;
                                m.SetTexture(potential.Key, Blackout(gt, potential.Value));
                            }

                        var p2 = new Dictionary<int, float>();
                        foreach (var potential2 in potentials2)
                            if (m.HasProperty(potential2))
                            {
                                var gt = m.GetFloat(potential2);
                                p2[potential2] = gt;
                                m.SetFloat(potential2, 0f);
                            }

                        var p3 = new Dictionary<int, Color>();
                        foreach (var potential3 in potentials3)
                            if (m.HasProperty(potential3))
                            {
                                var gt = m.GetColor(potential3);
                                p3[potential3] = gt;
                                m.SetColor(potential3, Color.black);
                            }

                        var mi = new MaterialInfo() { m = m, tex_props = p, float_props = p2, col_props = p3 };

                        list.Add(mi);
                    }
                }
            }

            Shader.SetGlobalColor(ChaShader._LineColorG, lc);
            renderCam.clearFlags = CameraClearFlags.Color;
            renderCam.targetTexture = rt_temp;
            renderCam.rect = new Rect(0, 0, 1, 1);
            renderCam.Render();
            renderCam.targetTexture = targetTexture;
            renderCam.rect = rect;

            RenderTexture.active = rt_temp;
            t2d.ReadPixels(new Rect(0f, 0f, rw, rh), 0, 0);
            t2d.Apply();
            RenderTexture.active = rta;
            renderCam.backgroundColor = backgroundColor;
            renderCam.clearFlags = clearFlags;
            RenderTexture.ReleaseTemporary(rt_temp);

            foreach (var smr in list)
            {
                foreach (var k in smr.tex_props.Keys)
                {
                    var t = smr.m.GetTexture(k);
                    GameObject.Destroy(t);
                    smr.m.SetTexture(k, smr.tex_props[k]);
                }
                foreach (var k in smr.float_props.Keys)
                    smr.m.SetFloat(k, smr.float_props[k]);
                foreach (var k in smr.col_props.Keys)
                    smr.m.SetColor(k, smr.col_props[k]);
            }


            return t2d;
        }

        Texture Blackout(Texture source, float bw = 0f)
        {
            if (source == null) return null;
            matBlackout.SetColor(col, new Color(bw, bw, bw));
            var rt = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, rt, matBlackout);
            return rt;
        }
    }
}