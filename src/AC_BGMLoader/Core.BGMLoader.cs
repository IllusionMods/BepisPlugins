using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepisPlugins;
using HarmonyLib;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private static FileInfo[] _clips = null!;

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
                    Harmony.CreateAndPatchAll(typeof(BGMLoader), GUID);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load custom intro clips - " + e);
                }
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play), typeof(Manager.Sound.Loader))]
        private static bool TitleCallOverride(Manager.Sound.Loader loader)
        {
            Console.WriteLine($"XX {loader.Type} | {loader.Asset}");
            var newClip = TryGetIntroclip(loader.Type, loader.Asset);
            if (newClip != null)
            {
                Console.WriteLine($"XX hit");
                Manager.Sound.Play(loader.Type, newClip, loader.FadeTime);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play), typeof(Manager.Sound.Type), typeof(AudioClip), typeof(float))]
        private static void TitleCallOverride(Manager.Sound.Type type, ref AudioClip clip)
        {
            Console.WriteLine($"{type} | {clip.name}");
            var newclip = TryGetIntroclip(type, clip?.name);
            if (newclip != null) clip = newclip;
        }

        private static AudioClip TryGetIntroclip(Manager.Sound.Type type, string clipName)
        {
            if (type == Manager.Sound.Type.SystemSE && clipName != null && Regex.IsMatch(clipName, @"^ac_sv_\d\d_\d\d\d$"))
            {
                try
                {
                    // Need to make sure it's not some other sound effect, this seems to work
                    var sceneName = SceneManager.GetActiveScene().name;
                    if (sceneName != "Logo" && sceneName != "Title") return null;

                    var pick = _clips[UnityEngine.Random.Range(0, _clips.Length)];
                    var clipData = File.ReadAllBytes(pick.FullName);

                    Logger.LogInfo("Playing custom intro clip - " + pick.Name);

                    // BUG Some clips are not giving sound, but they are playing
                    return WavUtility.ToAudioClip(clipData); // slowest step mostly because of AudioClip calls
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to play custom intro clip - " + e);
                }
            }

            return null;
        }
    }
}