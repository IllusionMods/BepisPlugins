﻿using BepInEx;
using BepisPlugins;
using Config;
using HarmonyLib;
using Illusion.Game;
using IllusionUtility.GetUtility;
using System.ComponentModel;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "HS2_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for HoneySelect 2";

        private static ConfigurationManager.ConfigurationManager _manager;

        private void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            bool mainGame = Application.productName == Constants.GameProcessName;
            if (mainGame)
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

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), nameof(ConfigWindow.Initialize))]
        private static void OnOpen(ConfigWindow __instance, ref Button[] ___buttons)
        {
            // Spawn a new button for plugin settings
            var original = __instance.transform.FindLoop("btnTitle").transform;
            var copy = Instantiate(original, original.parent);
            copy.name = "btnPluginSettings";

            copy.GetComponentInChildren<Text>().text = "Plugin settings";
            copy.GetComponent<ObservablePointerEnterTrigger>().onPointerEnter = original.GetComponent<ObservablePointerEnterTrigger>().onPointerEnter;

            var btn = copy.GetComponentInChildren<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() =>
            {
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
                Utils.Sound.Play(SystemSE.ok_s);
            });

            // Makes the game set up hover effect for our button as well, no other effect
            ___buttons = ___buttons.AddToArray(btn);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigWindow), nameof(ConfigWindow.Unload))]
        private static void OnClose() => _manager.DisplayingWindow = _manager.DisplayingWindow && !_manager.IsWindowFullscreen; // Keep the window open if user dragged it
    }
}
