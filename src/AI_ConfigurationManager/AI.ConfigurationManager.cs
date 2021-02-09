using AIProject;
using BepInEx;
using BepisPlugins;
using ConfigScene;
using HarmonyLib;
using IllusionUtility.GetUtility;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "AI_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for AI-Shoujo";

        private static ConfigurationManager.ConfigurationManager _manager;

        private void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            var isStudio = Application.productName == "StudioNEOV2";
            if (!isStudio)
            {
                Harmony.CreateAndPatchAll(typeof(ConfigurationManagerWrapper));
                // Main game is handled by the hooks, Update is only for studio
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.Instance.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CustomControl), "Update")]
        private static void ConfigScene_Toggle()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !Manager.Scene.Instance.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), "Open")]
        private static void OnOpen(ConfigWindow __instance, ref Button[] ___buttons)
        {
            // Spawn a new button for plugin settings
            var original = __instance.transform.FindLoop("btnTitle").transform;
            var copy = Instantiate(original, original.parent);
            copy.name = "btnPluginSettings";

            copy.GetComponentInChildren<Text>().text = "Plugin settings";

            var btn = copy.GetComponentInChildren<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() =>
            {
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
                Singleton<Manager.Resources>.Instance.SoundPack.Play(SoundPack.SystemSE.OK_S);
            });

            // Makes the game set up hover effect for our button as well, no other effect
            ___buttons = ___buttons.AddToArray(btn);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), nameof(ConfigWindow.Unload))]
        private static void OnClose() => _manager.DisplayingWindow = false;
    }
}
