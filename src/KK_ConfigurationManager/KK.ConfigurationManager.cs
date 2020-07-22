﻿using System.Collections;
using BepInEx;
using HarmonyLib;
using BepisPlugins;
using System.ComponentModel;
using BepInEx.Harmony;
using Illusion.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "KK_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for Koikatsu";

        private static ConfigurationManager.ConfigurationManager _manager;
        
        private void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            var isStudio = Application.productName == "CharaStudio";
            if (!isStudio)
            {
                HarmonyWrapper.PatchAll(typeof(ConfigurationManagerWrapper));
                // Main game is handled by the hooks, Update is only for studio
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && Singleton<Studio.Studio>.IsInstance() && !Manager.Scene.Instance.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ConfigScene), "Start")]
        private static void OnOpen(ref object __result)
        {
            __result = new[] { __result, CreateButton() }.GetEnumerator();
            
            IEnumerator CreateButton()
            {
                // Scene is rebuilt every time so the button has to be created again
                var orig = GameObject.Find("ConfigScene/Canvas/Node ShortCut/ShortCutButton(Clone)");
            
                var copy = Instantiate(orig, orig.transform.parent);
                copy.name = "Plugin Settings";

                var text = copy.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                text.text = "Plugin Settings";
                text.fontSize = 20;
                
                var button = copy.GetComponentInChildren<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate
                {
                    _manager.DisplayingWindow = !_manager.DisplayingWindow;
                    Utils.Sound.Play(SystemSE.sel);
                });
                
                yield break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ConfigScene), "Unload")]
        private static void OnClose() => _manager.DisplayingWindow = false;
    }
}
