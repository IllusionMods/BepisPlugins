using Screencap;
using System.Collections.Generic;
using UnityEngine;

//code by essu
namespace alphaShot
{
    public class MaterialInfo   //pls no bully
    {
        public Renderer r = null;
        public Dictionary<int, Texture> tex_props = new Dictionary<int, Texture>();
        public Dictionary<int, float> float_props = new Dictionary<int, float>();
        public Dictionary<int, Color> col_props = new Dictionary<int, Color>();
    }
    
    public static class AlphaShot2
    {
        public static byte[] Capture(int width, int height, int downsampling, int antialiasing)
        {
            var texture2D = PerformCapture(true, width * downsampling, height * downsampling, antialiasing);
            var texture2D2 = PerformCapture(false, width * downsampling, height * downsampling, antialiasing);

            //Save(texture2D.EncodeToPNG());
            //Save(texture2D2.EncodeToPNG());

            var texture2D3 = new Texture2D(texture2D.width, texture2D.height, TextureFormat.ARGB32, false);

            var pixels = texture2D.GetPixels();
            var pixels2 = texture2D2.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var a = 1 - pixels[i].r;
                pixels2[i].a = a;
            }
            texture2D3.SetPixels(pixels2);
            texture2D3.Apply();

            texture2D3.Downsample(width, height, downsampling);
            texture2D3.Apply();

            
            byte[] result = texture2D3.EncodeToPNG();

            GameObject.Destroy(texture2D);
            GameObject.Destroy(texture2D2);
            GameObject.Destroy(texture2D3);

            var cc = Singleton<ChaControl>.Instance;
            cc.SetBodyBaseMaterial();

            return result;
        }

        //TODO: Switch depending on shader type, clean this brute force shit up
        static int[] potentials = new[] {
                    Shader.PropertyToID("_MainTex"), //regular tex
                    Shader.PropertyToID("_NormalMap"), //Pantyhose
                    Shader.PropertyToID("_HairGloss"),  //Shader Forge/main_hair
                    Shader.PropertyToID("_overtex1"),  //eye
                    Shader.PropertyToID("_overtex2"),  //eye
                };

        static int[] potentials2 = new[] {
                    Shader.PropertyToID("_rimV"),  //Shader Forge/main_hair

                    Shader.PropertyToID("_SpecularPower"),  //skin shine
                    Shader.PropertyToID("_SpecularPowerNail"),  //skin shine
                };

        static int[] potentials3 = new[] {
                    Shader.PropertyToID("_Color"),  //eye?
                    Shader.PropertyToID("_shadowcolor"),  //eye
                    Shader.PropertyToID("_overcolor1"),  //eye
                    Shader.PropertyToID("_overcolor2"),  //eye
                    Shader.PropertyToID("_overcolor3"),  //eye
                };

        public static Texture2D PerformCapture(bool captureAlphaOnly, int renderWidth, int renderHeight, int antiAliasing)
        {
            var renderCam = CameraUtils.CopyCamera(Camera.main);
            var targetTexture = renderCam.targetTexture;
            var rect = renderCam.rect;
            var backgroundColor = renderCam.backgroundColor;
            var clearFlags = renderCam.clearFlags;
            var t2d = new Texture2D(renderWidth, renderHeight, TextureFormat.RGB24, false);
            var rt_temp = RenderTexture.GetTemporary(renderWidth, renderHeight, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);

            rt_temp.antiAliasing = antiAliasing;

            var list = new List<MaterialInfo>();
            var lc = Shader.GetGlobalColor(ChaShader._LineColorG);
            renderCam.backgroundColor = Color.black;

            var plainTex = new Texture2D(1, 1);
            plainTex.SetPixel(0, 0, Color.black);
            plainTex.Apply();

            if (captureAlphaOnly)
            {

                Shader.SetGlobalColor(ChaShader._LineColorG, new Color(.5f, .5f, .5f, 0f));

                renderCam.backgroundColor = Color.white;

                foreach (var smr in GameObject.FindObjectsOfType<Renderer>())
                {
                    var p = new Dictionary<int, Texture>();
                    foreach (var potential in potentials)
                        if (smr.material.HasProperty(potential))
                        {
                            var gt = smr.material.GetTexture(potential);
                            p[potential] = gt;
                            smr.material.SetTexture(potential, plainTex);
                        }

                    var p2 = new Dictionary<int, float>();
                    foreach (var potential2 in potentials2)
                        if (smr.material.HasProperty(potential2))
                        {
                            var gt = smr.material.GetFloat(potential2);
                            p2[potential2] = gt;
                            smr.material.SetFloat(potential2, 0f);
                        }

                    var p3 = new Dictionary<int, Color>();
                    foreach (var potential3 in potentials3)
                        if (smr.material.HasProperty(potential3))
                        {
                            var gt = smr.material.GetColor(potential3);
                            p3[potential3] = gt;
                            smr.material.SetColor(potential3, Color.black);
                        }

                    var mi = new MaterialInfo() { r = smr, tex_props = p, float_props = p2, col_props = p3 };
                    list.Add(mi);
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
            t2d.ReadPixels(new Rect(0f, 0f, renderWidth, renderHeight), 0, 0);
            t2d.Apply();
            RenderTexture.active = null;
            renderCam.backgroundColor = backgroundColor;
            renderCam.clearFlags = clearFlags;
            RenderTexture.ReleaseTemporary(rt_temp);
            GameObject.Destroy(plainTex);

            foreach (var smr in list)
            {
                foreach (var k in smr.tex_props.Keys)
                    smr.r.material.SetTexture(k, smr.tex_props[k]);
                foreach (var k in smr.float_props.Keys)
                    smr.r.material.SetFloat(k, smr.float_props[k]);
                foreach (var k in smr.col_props.Keys)
                    smr.r.material.SetColor(k, smr.col_props[k]);
            }

            GameObject.Destroy(renderCam.gameObject);

            return t2d;
        }
    }
}