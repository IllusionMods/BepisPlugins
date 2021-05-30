using UnityEngine;

namespace BGMLoader
{
    public static class AudioLoader
    {
        public static AudioClip LoadAudioClip(string path)
        {
#pragma warning disable 618 // Disable the obsolete warning
            using (WWW www = new WWW(BepInEx.Utility.ConvertToWWWFormat(path)))
#pragma warning restore 618
            {
                AudioClip clip = www.GetAudioClip();

                //Wait for the clip to be loaded before returning it
                while (clip.loadState != AudioDataLoadState.Loaded) { }

                return clip;
            }
        }
    }
}
