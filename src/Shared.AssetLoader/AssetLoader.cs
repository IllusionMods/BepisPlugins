using System.IO;
using UnityEngine;

namespace Shared
{
    internal static class AssetLoader
    {
        public static AudioClip LoadAudioClip(string path, AudioType type)
        {
            using (WWW loadGachi = new WWW(BepInEx.Utility.ConvertToWWWFormat(path)))
            {
                AudioClip clip = loadGachi.GetAudioClipCompressed(false, type);

                //force single threaded loading instead of using a coroutine
                while (clip.loadState != AudioDataLoadState.Loaded) { }

                return clip;
            }
        }

        public static Texture2D LoadTexture(string path) => LoadTexture(File.ReadAllBytes(path));

        public static Texture2D LoadTexture(Stream stream) => LoadTexture(stream, (int)stream.Length);

        public static Texture2D LoadTexture(Stream stream, int length) => LoadTexture(stream, length, TextureFormat.RGBA32, true);

        public static Texture2D LoadTexture(Stream stream, int length, TextureFormat format, bool mipmap)
        {
            byte[] buffer = new byte[length];

            stream.Read(buffer, 0, length);

            return LoadTexture(buffer, format, mipmap);
        }

        public static Texture2D LoadTexture(byte[] data) => LoadTexture(data, TextureFormat.RGBA32, true);

        public static Texture2D LoadTexture(byte[] data, TextureFormat format, bool mipmap)
        {
            Texture2D tex = new Texture2D(2, 2, format, mipmap);
            tex.LoadImage(data);
            return tex;
        }
    }
}
