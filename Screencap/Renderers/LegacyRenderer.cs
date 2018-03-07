using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Screencap
{
    public static class LegacyRenderer
    {
        public static byte[] RenderCamera(int ResolutionX, int ResolutionY, int DownscalingRate, int AntiAliasing)
        {
            Camera renderCam = CameraUtils.CopyCamera(Camera.main);

            renderCam.targetTexture = new RenderTexture(ResolutionX * DownscalingRate, ResolutionY * DownscalingRate, 32); //((int)cam.pixelRect.width, (int)cam.pixelRect.height, 32);
            renderCam.aspect = renderCam.targetTexture.width / (float)renderCam.targetTexture.height;
            renderCam.targetTexture.antiAliasing = AntiAliasing;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = renderCam.targetTexture;

            renderCam.clearFlags = CameraClearFlags.Skybox;

            renderCam.Render();
            Texture2D image = new Texture2D(ResolutionX * DownscalingRate, ResolutionY * DownscalingRate);
            image.ReadPixels(new Rect(0, 0, ResolutionX * DownscalingRate, ResolutionY * DownscalingRate), 0, 0);

            TextureScale.Bilinear(image, ResolutionX, ResolutionY);

            image.Apply();
            RenderTexture.active = currentRT;
            GameObject.Destroy(renderCam.targetTexture);
            GameObject.Destroy(renderCam);

            byte[] result = image.EncodeToPNG();

            GameObject.Destroy(image);

            return result;
        }

        
    }
}
