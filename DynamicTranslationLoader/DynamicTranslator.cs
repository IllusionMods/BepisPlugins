using BepInEx;
using BepInEx.Common;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DynamicTranslationLoader
{
    [BepInPlugin(GUID: "com.bepis.bepinex.dynamictranslator", Name: "Dynamic Translator", Version: "2.1")]
    public class DynamicTranslator : BaseUnityPlugin
    {
        private static Dictionary<string, string> translations = new Dictionary<string, string>();

        private static Dictionary<WeakReference, string> originalTranslations = new Dictionary<WeakReference, string>();

        private static HashSet<string> untranslated = new HashSet<string>();

        public static event Func<object, string, string> OnUnableToTranslateUGUI;

        public static event Func<object, string, string> OnUnableToTranslateTextMeshPro;



        Event ReloadTranslationsKeyEvent = Event.KeyboardEvent("f10");
        Event DumpUntranslatedTextKeyEvent = Event.KeyboardEvent("#f10");


        void Awake()
        {
            LoadTranslations();

            Hooks.InstallHooks();

            TranslateAll();
        }


        void LoadTranslations()
        {
            translations.Clear();

            string[] translation = Directory.GetFiles(Path.Combine(Utility.PluginsDirectory, "translation"), "*.txt", SearchOption.AllDirectories)
                .SelectMany(file => File.ReadAllLines(file))
                .ToArray();

            for (int i = 0; i < translation.Length; i++)
            {
                string line = translation[i];
                if (!line.Contains('='))
                    continue;

                string[] split = line.Split('=');

                translations[split[0]] = split[1];
            }
        }

        void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            //TranslateScene(scene);
        }

        public static string Translate(string input, object obj)
        {
            if(string.IsNullOrEmpty(input)) return input;

            // Consider changing this! You have a dictionary, but you iterate instead of making a lookup. Why do you not use the WeakKeyDictionary, you have instead? 
            if (!originalTranslations.Any(x => x.Key.Target == obj)) //check if we don't have the object in the dictionary
            {
                //add to untranslated list
                originalTranslations.Add(new WeakReference(obj), input);
            }

            string translation;
            if (translations.TryGetValue(input, out translation))
            { 
                return translation;
            }
            else if(obj is Text)
            {
                var immediatelyTranslated = OnUnableToTranslateUGUI?.Invoke( obj, input );
                if( immediatelyTranslated != null ) return immediatelyTranslated;
            }
            else if(obj is TMP_Text)
            {
                var immediatelyTranslated = OnUnableToTranslateTextMeshPro?.Invoke( obj, input );
                if( immediatelyTranslated != null ) return immediatelyTranslated;
            }
            
            // Consider changing this! You make a value lookup in a dictionary, which scales really poorly
            if (!untranslated.Contains(input) && !translations.ContainsValue(input))
                untranslated.Add(input);

            return input;
        }

        void TranslateAll()
        {
            foreach (TextMeshProUGUI gameObject in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                //gameObject.text = "Harsh is shit";

                gameObject.text = Translate(gameObject.text, gameObject);
            }
        }

        void UntranslateAll()
        {
            Hooks.TranslationHooksEnabled = false;

            int i = 0;

            foreach (var kv in originalTranslations)
            {
                if (kv.Key.IsAlive)
                {
                    i++;

                    if (kv.Key.Target is TMP_Text)
                    {
                        TMP_Text tmtext = (TMP_Text)kv.Key.Target;

                        tmtext.text = kv.Value;
                    }
                    else if (kv.Key.Target is TextMeshProUGUI)
                    {
                        TextMeshProUGUI tmtext = (TextMeshProUGUI)kv.Key.Target;

                        tmtext.text = kv.Value;
                    }
                    else if (kv.Key.Target is UnityEngine.UI.Text)
                    {
                        UnityEngine.UI.Text tmtext = (UnityEngine.UI.Text)kv.Key.Target;

                        tmtext.text = kv.Value;
                    }
                }
            }

            BepInEx.Logger.Log(LogLevel.Message, $"{i} translations reloaded.");

            Hooks.TranslationHooksEnabled = true;
        }

        void Retranslate()
        {
            UntranslateAll();

            LoadTranslations();

            TranslateAll();
        }

        void TranslateScene(Scene scene)
        {
            //foreach (GameObject obj in scene.GetRootGameObjects())
            //    foreach (TextMeshProUGUI gameObject in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            //    {
            //        //gameObject.text = "Harsh is shit";

            //        gameObject.text = Translate(gameObject.text, gameObject);
            //    }
        }

        void Dump()
        {
            string output = "";

            var fullUntranslated = originalTranslations
                .Where(x => !translations.ContainsKey(x.Value))
                .Select(x => x.Value)
                .Distinct()
                .Union(untranslated);

            foreach (var text in fullUntranslated)
                if (!Regex.Replace(text, @"[\d-]", string.Empty).IsNullOrWhiteSpace()
                        && !text.Contains("Reset"))
                    output += $"{text.Trim()}=\r\n";

            File.WriteAllText("dumped-tl.txt", output);
        }

        void Update()
        {
            if (Event.current == null) return;
            if (UnityEngine.Event.current.Equals(ReloadTranslationsKeyEvent))
            {
                Retranslate();
                BepInEx.Logger.Log(LogLevel.Message, $"Translation reloaded.");
            }
            if (UnityEngine.Event.current.Equals(DumpUntranslatedTextKeyEvent))
            {
                Dump();
                BepInEx.Logger.Log(LogLevel.Message, $"Text dumped to \"{Path.GetFullPath("dumped-tl.txt")}\"");
            }
        }


        #region MonoBehaviour
        //void OnEnable()
        //{
        //    SceneManager.sceneLoaded += LevelFinishedLoading;
        //}

        //void OnDisable()
        //{
        //    SceneManager.sceneLoaded -= LevelFinishedLoading;
        //}
        #endregion
    }
}
