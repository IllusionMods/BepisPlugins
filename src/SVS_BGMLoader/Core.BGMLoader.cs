using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepisPlugins;
using HarmonyLib;
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
                    if (_clips.Length > 0)
                        Harmony.CreateAndPatchAll(typeof(BGMLoader), GUID);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load custom intro clips - " + e);
                }
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play), typeof(Manager.Sound.Type), typeof(AudioClip), typeof(float))]
        private static void TitleCallOverride(Manager.Sound.Type type, ref AudioClip clip)
        {
            if (type == Manager.Sound.Type.SystemSE && clip.name != null && Regex.IsMatch(clip.name, @"^sv_\d\d\d_se_\d\d\d_\d\d\d$"))
            {
                try
                {
                    // Need to make sure it's not some other sound effect, this seems to work (map000 is title screen itself)
                    var sceneName = SceneManager.GetActiveScene().name;
                    if (sceneName != "Logo" && sceneName != "map000") return;

                    var pick = _clips[UnityEngine.Random.Range(0, _clips.Length)];
                    var clipData = File.ReadAllBytes(pick.FullName);

                    // BUG Some clips are not giving sound, but they are playing
                    clip = WavUtility.ToAudioClip(clipData); // slowest step mostly because of AudioClip calls

                    Logger.LogInfo("Playing custom intro clip - " + pick.Name);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to play custom intro clip - " + e);
                }
            }
        }
    }
}