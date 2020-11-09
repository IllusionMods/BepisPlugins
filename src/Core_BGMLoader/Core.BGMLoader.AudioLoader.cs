using NAudio.Wave;
using NVorbis;
using UnityEngine;

namespace BGMLoader
{
    public static class AudioLoader
    {
        /// <summary>
        /// Requires MediaFoundation to be installed (non N edition of windows)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static AudioClip LoadGeneric(string filename)
        {
            using (MediaFoundationReader reader = new MediaFoundationReader(filename))
            {
                int sampleCount = (int)(reader.WaveFormat.Channels * reader.Length / reader.WaveFormat.BlockAlign);

                var audioData = new float[sampleCount];

                reader.ToSampleProvider().Read(audioData, 0, sampleCount);

                return LoadInternal(filename, audioData, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate);
            }
        }

        public static AudioClip LoadWave(string filename)
        {
            using (WaveFileReader reader = new WaveFileReader(filename))
            {
                int sampleCount = (int)(reader.SampleCount * reader.WaveFormat.Channels);

                var audioData = new float[sampleCount];

                reader.ToSampleProvider().Read(audioData, 0, sampleCount);

                return LoadInternal(filename, audioData, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate);
            }
        }

        public static AudioClip LoadVorbis(string filename)
        {
            using (VorbisReader reader = new VorbisReader(filename))
            {
                int sampleCount = (int)reader.TotalSamples * reader.Channels;

                var audioData = new float[sampleCount];

                reader.ReadSamples(audioData, 0, sampleCount);

                return LoadInternal(filename, audioData, reader.Channels, reader.SampleRate);
            }
        }

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

        private static AudioClip LoadInternal(string name, float[] data, int channels, int sampleRate)
        {
            var clip = AudioClip.Create(name, data.Length / channels, channels, sampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }
    }
}
