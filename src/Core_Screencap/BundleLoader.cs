using BepisPlugins;
using UnityEngine;

namespace Screencap
{
    internal static class BundleLoader
    {
        private static Material _matOpaque;
        private static Material _mat3d;

        public static Material MatOpaque
        {
            get
            {
                if (!_matOpaque) LoadBundleScreencap();
                return _matOpaque;
            }
        }

        public static Material Mat3d
        {
            get
            {
                if (!_mat3d) LoadBundleScreencap();
                return _mat3d;
            }
        }

        private static void LoadBundleScreencap()
        {
            var opaqAb = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("screencap.unity3d"));
            _matOpaque = new Material(opaqAb.LoadAsset<Shader>("opaque_screencap.shader"));
            _mat3d = new Material(opaqAb.LoadAsset<Shader>("3d_screencap.shader"));
            opaqAb.Unload(false);
        }

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
