using BepInEx;
using BepInEx.Logging;
using BepisPlugins;
using DynamicTranslationLoader.Image;
using DynamicTranslationLoader.Text;
using System;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader
{
    [BepInPlugin(GUID: GUID, Name: "Dynamic Translator", Version: Version)]
    public class DynamicTranslator : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.dynamictranslator";
        public const string Version = Metadata.PluginsVersion;

        public static event Func<object, string, string> OnUnableToTranslateUGUI;
        public static event Func<object, string, string> OnUnableToTranslateTextMeshPro;

        public static string dirTranslation = Path.Combine(Paths.PluginPath, "translation");

        // Settings
        public static SavedKeyboardShortcut ReloadTranslations { get; set; }
        public static SavedKeyboardShortcut DumpUntranslatedText { get; set; }

        [DisplayName("!Enable image dumping")]
        [Description("Extract and save all in-game UI images to BepInEx\\translation\\Images\nWarning: Very slow, disable when not needed")]
        [Advanced(true)]
        public static ConfigWrapper<bool> IsDumpingEnabled { get; set; }
        [DisplayName("Dump all images to global folder")]
        [Advanced(true)]
        public static ConfigWrapper<bool> DumpingAllToGlobal { get; set; }

        [DisplayName("Enable pasting text to clipboard")]
        public static ConfigWrapper<bool> IsPastingToClipboard { get; set; }

        public DynamicTranslator()
        {
            IsDumpingEnabled = new ConfigWrapper<bool>("dumping", this);
            IsPastingToClipboard = new ConfigWrapper<bool>("paste-to-clipboard", this);
            DumpingAllToGlobal = new ConfigWrapper<bool>("dump-to-global", this);
            ReloadTranslations = new SavedKeyboardShortcut("Reload translations", this, new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl));
            DumpUntranslatedText = new SavedKeyboardShortcut("Dump untranslated text", this, new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift));
        }

        public void Awake()
        {
            SetupTextTl(dirTranslation);
            SetupImageTl(dirTranslation);
        }

        public void Update()
        {
            if (Event.current == null)
                return;
            if (ReloadTranslations.IsDown())
            {
                TextTranslator.RetranslateText();
                Logger.Log(LogLevel.Message, "Translation reloaded.");
            }
            else if (DumpUntranslatedText.IsDown())
            {
                TextTranslator.DumpText();
                Logger.Log(LogLevel.Message, $"Text dumped to \"{Path.GetFullPath("dumped-tl.txt")}\"");
            }
        }

        public void OnEnable()
        {
            SceneManager.sceneLoaded += TextTranslator.TranslateScene;
        }

        public void OnDisable()
        {
            SceneManager.sceneLoaded -= TextTranslator.TranslateScene;
        }

        private static void SetupTextTl(string dirTranslation)
        {
            TextTranslator.LoadTextTranslations(dirTranslation);

            TextHooks.InstallHooks();

            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(TextTranslator.RedirectHook);

            TextTranslator.TranslateTextAll();
        }

        private static void SetupImageTl(string dirTranslation)
        {
            ImageTranslator.LoadImageTranslations(dirTranslation);

            ImageHooks.InstallHooks();
        }

        internal static string OnOnUnableToTranslateUgui(object arg1, string arg2)
        {
            return OnUnableToTranslateUGUI?.Invoke(arg1, arg2);
        }

        internal static string OnOnUnableToTranslateTextMeshPro(object arg1, string arg2)
        {
            return OnUnableToTranslateTextMeshPro?.Invoke(arg1, arg2);
        }
    }
}