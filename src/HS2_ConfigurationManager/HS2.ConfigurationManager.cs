using AIProject;
using BepInEx;
using BepInEx.Harmony;
using BepisPlugins;
using Config;
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
            //if (!isStudio)
            //{
            //    HarmonyWrapper.PatchAll(typeof(ConfigurationManagerWrapper));
            //    // Main game is handled by the hooks, Update is only for studio
            //    enabled = false;
            //}
        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.IsNowLoadingFade)
            if (Input.GetKeyDown(KeyCode.F1) && !Manager.Scene.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }
    }
}
