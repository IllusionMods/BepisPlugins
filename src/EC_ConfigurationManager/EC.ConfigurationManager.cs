using BepInEx;
using BepisPlugins;
using HarmonyLib;
using Illusion.Game;
using System.Collections;
using System.ComponentModel;
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
        public const string GUID = "EC_" + ConfigurationManager.ConfigurationManager.GUID;
        public const string PluginName = "Configuration Manager wrapper for EmotionCreators";
        private static ConfigurationManager.ConfigurationManager _manager;

        private void Start()
        {
            _manager = GetComponent<ConfigurationManager.ConfigurationManager>();
            _manager.OverrideHotkey = true;

            Harmony.CreateAndPatchAll(typeof(ConfigurationManagerWrapper));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigScene), "Start")]
        private static void OnOpen(ref object __result)
        {
            __result = new[] { __result, CreateButton() }.GetEnumerator();

            // Scene is rebuilt every time so the button has to be created again
            IEnumerator CreateButton()
            {
                var parent = GameObject.Find("ConfigScene/Canvas/Node ShortCut");
                var orig = parent.transform.Find("ShortCutButton(Clone)");

                var copy = Instantiate(orig.gameObject, orig.parent);
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

                // Refuses to move unless we wait
                yield return new WaitForEndOfFrame();

                // Move all buttons to the left to compensate for the new button
                for (var i = 0; i < parent.transform.childCount; i++)
                    parent.transform.GetChild(i).localPosition += new Vector3(-177, 0, 0);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigScene), "Unload")]
        private static void OnClose() => _manager.DisplayingWindow = false;
    }
}