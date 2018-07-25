using System.IO;
using BepInEx.Common;
using UnityEngine;

namespace ResourceRedirector
{
    public static class AssetLoader
    {
        public static AudioClip LoadAudioClip(string path, AudioType type)
        {
            using (WWW loadGachi = new WWW(Utility.ConvertToWWWFormat(path)))
            {
                AudioClip clip = loadGachi.GetAudioClipCompressed(false, type);

                //force single threaded loading instead of using a coroutine
                while (!clip.isReadyToPlay) { }

                return clip;
            }
        }
        

        public static Texture2D LoadTexture(string path)
        {
            return LoadTexture(File.ReadAllBytes(path));
        }

        public static Texture2D LoadTexture(Stream stream)
        {
            return LoadTexture(stream, (int)stream.Length);
        }

        public static Texture2D LoadTexture(Stream stream, int length)
        {
            byte[] buffer = new byte[length];

            stream.Read(buffer, 0 , length);

            return LoadTexture(buffer);
        }

        public static Texture2D LoadTexture(byte[] data)
        {
            Texture2D tex = new Texture2D(2, 2);

            //DDS method
            //tex.LoadRawTextureData

            tex.LoadImage(data);

            return tex;
        }
    }
}
