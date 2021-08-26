using BepInEx;
using BepisPlugins;
using HarmonyLib;
using System.ComponentModel;
using Illusion.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigurationManagerWrapper
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [Browsable(false)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class ConfigurationManagerWrapper : BaseUnityPlugin
    {
        public const string Version = Metadata.PluginsVersion;
        public const string GUID = "KKS_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for Koikatsu Sunshine";

        private static ConfigurationManager.ConfigurationManager _manager;

        private void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            bool mainGame = Application.productName == Constants.GameProcessName;
            if (mainGame)
            {
                var harmony = Harmony.CreateAndPatchAll(typeof(ConfigurationManagerWrapper));
                
                var iteratorType = typeof(ConfigScene).GetNestedType("<Start>d__40", AccessTools.all);
                var iteratorMethod = AccessTools.Method(iteratorType, "MoveNext");
                var postfix = new HarmonyMethod(typeof(ConfigurationManagerWrapper), nameof(OnOpen));
                harmony.Patch(iteratorMethod, null, postfix);
                
                //Main game is handled by the hooks, disable this plugin to prevent Update from running
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !Manager.Scene.IsNowLoadingFade)
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
        }

        private static void OnOpen()
        {
            var existing = GameObject.Find("ConfigScene(Clone)/Canvas/Node ShortCut/Plugin Settings");
            if (existing != null)
                return;
            
            var original = GameObject.Find("ConfigScene(Clone)/Canvas/Node ShortCut/ShortCutButton(Clone)");
            if (original == null)
                return;

            var copy = Instantiate(original, original.transform.parent);
            copy.name = "Plugin Settings";
            
            copy.GetComponentInChildren<TextMeshProUGUI>().text = copy.name;
            
            var button = copy.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                _manager.DisplayingWindow = !_manager.DisplayingWindow;
                Utils.Sound.Play(SystemSE.sel);
            });
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(ConfigScene), nameof(ConfigScene.Unload))]
        private static void OnClose() => _manager.DisplayingWindow = false;
    }
}