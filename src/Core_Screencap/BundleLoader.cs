using BepisPlugins;
using UnityEngine;

namespace Screencap
{
    internal static class BundleLoader
    {
        static BundleLoader()
        {
            var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("screencap.unity3d"));
            MatOpaque = new Material(ab.LoadAsset<Shader>("opaque_screencap.shader"));
            Mat3d = new Material(ab.LoadAsset<Shader>("3d_screencap.shader"));
            
            MatAccumulate = new Material(ab.LoadAsset<Shader>("accumulate.shader"));
            MatNormalize = new Material(ab.LoadAsset<Shader>("normalize.shader"));
            
            MatEquirectangularConverter = new Material(ab.LoadAsset<Shader>("EquirectangularConverter.shader"));

            MatComposite = new Material(ab.LoadAsset<Shader>("composite"));
            MatScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            
            MatRgAlpha = new Material(ab.LoadAsset<Shader>("rgAlpha2"));
            MatBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            MatMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            
            ab.Unload(false);
        }

        public static Material MatOpaque { get; }
        public static Material Mat3d { get; }
        public static Material MatAccumulate { get; }
        public static Material MatNormalize { get; }
        public static Material MatEquirectangularConverter { get; }
        public static Material MatScale { get; }
        public static Material MatBlackout { get; }
        public static Material MatMask { get; }
        public static Material MatComposite { get; }
        public static Material MatRgAlpha { get; }

        public static RenderTexture StitchImages(RenderTexture capture, RenderTexture capture2, float overlapOffset)
        {
            var xAdjust = (int)(capture.width * overlapOffset);
            var result = RenderTexture.GetTemporary((capture.width - xAdjust) * 2, capture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);

            Mat3d.SetTexture("_TextureTwo", capture2);
            Mat3d.SetFloat("_OverlapOffset", overlapOffset);
            Graphics.Blit(capture, result, Mat3d);

            return result;
        }
    }
}
