using BepInEx;
using BepisPlugins;
using Illusion.Game;
using System.IO;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace BGMLoader
{
    /// <summary>
    /// Place .ogg files in BepInEx/plugins/bgm folder with the name BGM00.ogg, BGM01.ogg, etc. to load them in place of the game's BGM
    /// Place .wav files in BepInEx/plugins/introclips folder to load them in place of startup sounds
    /// </summary>
    public partial class BGMLoader
    {
        public const string GUID = "com.bepis.bgmloader";
        public const string PluginName = "BGM Loader";
        public const string Version = Metadata.PluginsVersion;
        public static string IntroClipsDirectory = Path.Combine(Paths.PluginPath, "introclips");
        public static string BGMDirectory = Path.Combine(Paths.PluginPath, "bgm");

        public void Awake()
        {
            ResourceRedirection.EnableSyncOverAsyncAssetLoads();

            if (Directory.Exists(IntroClipsDirectory))
                ResourceRedirection.RegisterAsyncAndSyncAssetLoadingHook(LoadIntroClips);
            if (Directory.Exists(BGMDirectory))
                ResourceRedirection.RegisterAsyncAndSyncAssetLoadingHook(LoadBGM);
        }

        public void LoadIntroClips(IAssetLoadingContext context)
        {
            if (context.Bundle.name.Length > 30 &&
                (context.Bundle.name.StartsWith("sound/data/systemse/brandcall/") || context.Bundle.name.StartsWith("sound/data/systemse/titlecall/")))
            {
                var files = Directory.GetFiles(IntroClipsDirectory, "*.wav");

                if (files.Length == 0)
                    return;

                var path = files[Random.Range(0, files.Length - 1)];

                context.Asset = AudioLoader.LoadAudioClip(path);
                context.Complete();
            }
        }

        public void LoadBGM(IAssetLoadingContext context)
        {
            if (context.Parameters.Name != null && TryGetOverrideFileName(context, out var overrideFileName))
            {
                var path = Path.Combine(BGMDirectory, overrideFileName);

                if (File.Exists(path))
                {
                    Logger.LogDebug($"Loading BGM track \"{context.Parameters.Name}\" from {path}");

                    context.Asset = AudioLoader.LoadAudioClip(path);
                    context.Complete();
                }
            }
        }
    }
}