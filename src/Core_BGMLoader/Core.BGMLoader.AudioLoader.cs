using UnityEngine;

namespace BGMLoader
{
    public static class AudioLoader
    {
        public static AudioClip LoadAudioClip(string path)
        {
            using (WWW www = new WWW(BepInEx.Utility.ConvertToWWWFormat(path)))
            {
                AudioClip clip = www.GetAudioClip();

                //Wait for the clip to be loaded before returning it
                while (clip.loadState != AudioDataLoadState.Loaded) { }

                return clip;
            }
        }
    }
}
