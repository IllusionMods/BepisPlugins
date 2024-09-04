using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepisPlugins;
using HarmonyLib;
using SV;
using SV.Config;
using Il2CppInterop.Runtime.Injection;
using ILLGames.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BasePlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "HC_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for HoneyCome";

        // localization table
        private static ReadOnlyDictionary<string, string> localizePluginSettings = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "en", "Plugin settings" },
                { "ja-jp", "プラグイン設定" },
                { "ko-lr", "플러그인 설정" },
                { "zh-cn" , "插件设置" },
                { "zh-tw" , "插件設置" },
            });

        private static ConfigurationManager.ConfigurationManager _manager;
        private static readonly string pluginSettingsText;

        static ConfigurationManagerWrapper()
        {
            // localize
            var lang = Thread.CurrentThread.CurrentCulture.Name;
            if (!localizePluginSettings.TryGetValue(lang, out pluginSettingsText))
            {
                var index = lang.IndexOf('-');
                if (index == -1 ||
                    !localizePluginSettings.TryGetValue(lang = lang.Substring(0, index), out pluginSettingsText))
                {
                    lang += '-';
                    pluginSettingsText =
                        localizePluginSettings.FirstOrDefault(kvp =>
                            kvp.Key.StartsWith(lang, StringComparison.OrdinalIgnoreCase)).Value
                        ?? localizePluginSettings["en"];
                }
            }
        }

        public override void Load()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MonoBehaviour>();
                var gameObject = new GameObject(nameof(ConfigurationManagerWrapper));
                gameObject.hideFlags |= HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<MonoBehaviour>();
            }
            catch
            {
                Log.LogError($"FAILED to Register Il2Cpp Type: {nameof(ConfigurationManagerWrapper)}.{nameof(MonoBehaviour)}!");
            }
        }

        private class MonoBehaviour : UnityEngine.MonoBehaviour
        {
            private void Start()
            {
                if (!IL2CPPChainloader.Instance.Plugins.TryGetValue(ConfigurationManager.ConfigurationManager.GUID, out var pluginInfo))
                    return;
                _manager = pluginInfo.Instance as ConfigurationManager.ConfigurationManager;
                if (_manager is null)
                    return;
                _manager.OverrideHotkey = true;

                if (!Constants.InsideStudio)
                {
                    Harmony.CreateAndPatchAll(typeof(ConfigurationManagerWrapper), GUID);
                    //Main game is handled by the hooks, disable this plugin to prevent Update from running
                    enabled = false;
                }
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.F1) && !Manager.Scene.IsNowLoadingFade)
                    _manager.DisplayingWindow = !_manager.DisplayingWindow;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), nameof(ConfigWindow.Start))]
        private static void OnOpen(ConfigWindow __instance)
        {
            // Spawn a new button for plugin settings
            var original = __instance.transform.FindLoop("btnTitle").transform;
            var copy = Object.Instantiate(original, original.parent);
            copy.name = "btnPluginSettings";
            copy.gameObject.SetActiveIfDifferent(true);

            copy.GetComponentInChildren<TextMeshProUGUI>().text = pluginSettingsText;

            (copy.GetComponent<Button>().onClick ??= new()).AddListener((UnityAction)new System.Action(() =>
            {
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
                SV.Sound.Play(SystemSE.ok);
            }));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), nameof(ConfigWindow.Unload))]
        private static void OnClose() => _manager.DisplayingWindow = _manager.DisplayingWindow && !_manager.IsWindowFullscreen; // Keep the window open if user dragged it
    }
}
