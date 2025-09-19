using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepisPlugins;
using HarmonyLib;

namespace BGMLoader
{
    /// <summary>
    /// Place .wav files in BepInEx/plugins/introclips folder to load them in place of startup sounds
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class BGMLoader : BasePlugin
    {
        public const string GUID = "com.bepis.bgmloader";
        public const string PluginName = "BGM Loader";
        public const string Version = Metadata.PluginsVersion;
        public static string IntroClipsDirectory = Path.Combine(Paths.PluginPath, "introclips");

        private static ManualLogSource Logger = null!;
        private static FileInfo[] _clips;

        public override void Load()
        {
            Logger = Log;

            Task.Run(() =>
            {
                try
                {
                    var dir = Directory.CreateDirectory(IntroClipsDirectory);
                    _clips = dir.GetFiles("*.wav", SearchOption.AllDirectories);
                    Logger.LogInfo("Found " + _clips.Length + " custom intro clips");
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load custom intro clips - " + e);
                }
            });

            Harmony.CreateAndPatchAll(typeof(BGMLoader), GUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play), typeof(Manager.Sound.Loader))]
        private static bool TitleCallOverride(Manager.Sound.Loader loader)
        {
            if (loader.Bundle != null && loader.Bundle.StartsWith("sound/data/se/system/titlecall/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (_clips != null)
                    {
                        var pick = _clips[UnityEngine.Random.Range(0, _clips.Length)];
                        var clipData = File.ReadAllBytes(pick.FullName);
                        var clip = WavUtility.ToAudioClip(clipData); // slowest step mostly because of AudioClip calls

                        var source = Manager.Sound.Play(Manager.Sound.Type.SystemSE, clip);

                        // Fix popping sound at start/end in clips that don't have enough silence at the start and end (Unity issue)
                        // no easy way to fix the end popping sound, but this is good enough
                        source.time = 0.05f;

                        Logger.LogInfo("Playing custom intro clip - " + pick.Name);

                        return false;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to play custom intro clip - " + e);
                }
            }

            return true;
        }

    }
}