using System;
using BepisPlugins;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screencap
{
    internal static class BundleLoader
    {
        static BundleLoader()
        {
            var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("screencap.unity3d"));
            MatOpaque = new Material(ab.LoadAsset<Shader>("opaque_screencap.shader"));
            MatOpaque.hideFlags = HideFlags.HideAndDontSave;
            Mat3d = new Material(ab.LoadAsset<Shader>("3d_screencap.shader"));
            Mat3d.hideFlags = HideFlags.HideAndDontSave;

            MatAccumulate = new Material(ab.LoadAsset<Shader>("accumulate.shader"));
            MatAccumulate.hideFlags = HideFlags.HideAndDontSave;
            MatNormalize = new Material(ab.LoadAsset<Shader>("normalize.shader"));
            MatNormalize.hideFlags = HideFlags.HideAndDontSave;

            MatScale = new Material(ab.LoadAsset<Shader>("resize.shader"));
            MatScale.hideFlags = HideFlags.HideAndDontSave;
            MatEquirectangularConverter = new Material(ab.LoadAsset<Shader>("EquirectangularConverter.shader"));
            MatEquirectangularConverter.hideFlags = HideFlags.HideAndDontSave;

            MatBlackout = new Material(ab.LoadAsset<Shader>("blackout.shader"));
            MatBlackout.hideFlags = HideFlags.HideAndDontSave;
            MatMask = new Material(ab.LoadAsset<Shader>("alphamask.shader"));
            MatMask.hideFlags = HideFlags.HideAndDontSave;
            MatComposite = new Material(ab.LoadAsset<Shader>("composite"));
            MatComposite.hideFlags = HideFlags.HideAndDontSave;
            MatRgAlpha = new Material(ab.LoadAsset<Shader>("rgAlpha2"));
            MatRgAlpha.hideFlags = HideFlags.HideAndDontSave;

            ab.Unload(false);
        }

        public static Material MatOpaque { get; }
        private static Material Mat3d { get; }

        private static Material MatAccumulate { get; }
        private static Material MatNormalize { get; }

        private static Material MatScale { get; }
        public static Material MatEquirectangularConverter { get; }

        public static Material MatBlackout { get; }
        public static Material MatMask { get; }
        public static Material MatComposite { get; }
        public static Material MatRgAlpha { get; }

        public static readonly int SampleCountNameID = Shader.PropertyToID("_SampleCount");
        public static readonly int KernelAndSizeNameID = Shader.PropertyToID("_KernelAndSize");
        public static readonly int TextureTwoNameID = Shader.PropertyToID("_TextureTwo");
        public static readonly int OverlapOffsetNameID = Shader.PropertyToID("_OverlapOffset");

        public static RenderTexture StitchImages(RenderTexture capture, RenderTexture capture2, float overlapOffset)
        {
            var xAdjust = (int)(capture.width * overlapOffset);
            var result = RenderTexture.GetTemporary((capture.width - xAdjust) * 2, capture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);

            Mat3d.SetTexture(TextureTwoNameID, capture2);
            Mat3d.SetFloat(OverlapOffsetNameID, overlapOffset);
            Graphics.Blit(capture, result, Mat3d);

            return result;
        }

        /// <summary>
        /// Scales a render texture to the target resolution using custom shader.
        /// Used for downscaling high resolution captures to final output size.
        /// </summary>
        public static RenderTexture ScaleTex(Texture input, int resolutionX, int resolutionY)
        {
            MatScale.SetVector(KernelAndSizeNameID, new Vector4(5, 5, resolutionX, resolutionY));
            var rt = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(input, rt, MatScale);
            if (input is RenderTexture rtInput)
                RenderTexture.ReleaseTemporary(rtInput);
            else
                Object.Destroy(input);
            RenderTexture.active = prev;
            return rt;
        }

        /// <summary>
        /// Performs multisampling by capturing multiple times from the camera each time with a different sub-pixel offset and accumulating them into a single render texture.
        /// </summary>
        /// <param name="resolutionX">The width of the screenshot in pixels.</param>
        /// <param name="resolutionY">The height of the screenshot in pixels.</param>
        /// <param name="sampleGridSize">The size of the grid used for sampling, determining the number of samples taken (total samples = size * size).</param>
        /// <param name="capture">A function that captures a render texture sample. Must be the same resolution as give in parameters.</param>
        /// <returns>A render texture containing the accumulated result of the samples.</returns>
        public static RenderTexture DoMultisampling(int resolutionX, int resolutionY, int sampleGridSize, Func<RenderTexture> capture)
        {
            var main = Camera.main ?? throw new ArgumentNullException("Camera.main");

            var gridCount = Math.Max(1, sampleGridSize);
            var sampleCount = gridCount * gridCount;

            if(sampleCount <= 1)
                return capture();

            var accumulator = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1);
            var prevActive = RenderTexture.active;
            RenderTexture.active = accumulator;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            RenderTexture.active = prevActive;

            try
            {
                for (int gy = 0; gy < gridCount; gy++)
                {
                    for (int gx = 0; gx < gridCount; gx++)
                    {
                        main.ResetProjectionMatrix();

                        var matrix = main.projectionMatrix;
                        float offsetX = (gx + 0.5f) / gridCount - 0.5f;
                        float offsetY = (gy + 0.5f) / gridCount - 0.5f;
                        if (main.orthographic)
                        {
                            matrix[0, 3] += 2f * offsetX / resolutionX;
                            matrix[1, 3] += 2f * offsetY / resolutionY;
                        }
                        else
                        {
                            matrix[0, 2] += 2f * offsetX / resolutionX;
                            matrix[1, 2] += 2f * offsetY / resolutionY;
                        }

                        main.projectionMatrix = matrix;

                        var sample = capture();

                        Graphics.Blit(sample, accumulator, MatAccumulate);
                        RenderTexture.ReleaseTemporary(sample);
                    }
                }

                main.ResetProjectionMatrix();

                var result = RenderTexture.GetTemporary(resolutionX, resolutionY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
                MatNormalize.SetFloat(SampleCountNameID, sampleCount);
                Graphics.Blit(accumulator, result, MatNormalize);
                return result;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(accumulator);
            }
        }
    }
}
