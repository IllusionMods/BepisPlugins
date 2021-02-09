using BepInEx;
using BepisPlugins;
using HarmonyLib;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "PH_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for PlayHome";

        private static ConfigurationManager.ConfigurationManager _manager;

        public void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            var isStudio = Application.productName.Contains("PlayHomeStudio");
            if (isStudio)
                return;

            Harmony.CreateAndPatchAll(typeof(ConfigurationManagerWrapper));
            enabled = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance())
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Config), "Start")]
        private static void Config_Start_CreateButton(ref Toggle[] ___tabs)
        {
            var original = ___tabs[1].transform;

            var copy = Instantiate(original, original.parent);
            copy.name = "Tab Plugin Settings";
            copy.GetComponentInChildren<Text>().text = "Plugin settings";

            var btn = copy.GetComponentInChildren<Toggle>();
            btn.onValueChanged = new Toggle.ToggleEvent();
            btn.onValueChanged.AddListener((selected) =>
            {
                if (!selected)
                    return;

                _manager.DisplayingWindow = !_manager.DisplayingWindow;
                Singleton<AudioControl>.Instance.Play2DSE(Singleton<AudioControl>.Instance.systemSE_choice);

                btn.isOn = false;
            });
            btn.group = null;
        }
    }
}