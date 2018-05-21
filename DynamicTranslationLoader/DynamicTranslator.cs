using BepInEx;
using BepInEx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ADV;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DynamicTranslationLoader
{
    [BepInDependency("com.bepis.bepinex.resourceredirector")]
    [BepInPlugin(GUID: "com.bepis.bepinex.dynamictranslator", Name: "Dynamic Translator", Version: "3.0")]
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

            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);

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

            BepInLogger.Log($"{i} translations reloaded.");

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
                BepInLogger.Log($"Translation reloaded.", true);
            }
            if (UnityEngine.Event.current.Equals(DumpUntranslatedTextKeyEvent))
            {
                Dump();
                BepInLogger.Log($"Text dumped to \"{Path.GetFullPath("dumped-tl.txt")}\"", true);
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

        

        private static FieldInfo f_commandPacks =
            typeof(TextScenario).GetField("commandPacks", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly string scenarioDir = Path.Combine(Utility.PluginsDirectory, "translation\\scenario");


        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : UnityEngine.Object
        {
            string path = $@"{Application.dataPath}\..\{(string.IsNullOrEmpty(manifest) ? "abdata" : manifest)}\{bundle}";

            var assetBundle = AssetBundle.LoadFromFile(path);

            T output = assetBundle.LoadAsset<T>(asset);
            assetBundle.Unload(false);

            return output;
        }

        protected IEnumerable<IEnumerable<string>> SplitAndEscape(string source)
        {
            StringBuilder bodyBuilder = new StringBuilder();

            // here we build rows, one by one
            int i = 0;
            var row = new List<string>();
            var limit = source.Length;
            bool inQuote = false;

            while (i < limit)
            {
                if (source[i] == '\r')
                {
                    //( ͠° ͜ʖ °)
                }
                else if (source[i] == ',' && !inQuote)
                {
                    row.Add(bodyBuilder.ToString());
                    bodyBuilder.Length = 0; //.NET 2.0 ghetto clear
                }
                else if (source[i] == '\n' && !inQuote)
                {
                    if (bodyBuilder.Length != 0 || row.Count != 0)
                    {
                        row.Add(bodyBuilder.ToString());
                        bodyBuilder.Length = 0; //.NET 2.0 ghetto clear
                    }

                    yield return row;
                    row.Clear();
                }
                else if (source[i] == '"')
                {
                    if (!inQuote)
                        inQuote = true;
                    else
                    {
                        if (i + 1 < limit
                            && source[i + 1] == '"')
                        {
                            bodyBuilder.Append('"');
                            i++;
                        }
                        else
                            inQuote = false;
                    }
                }
                else
                {
                    bodyBuilder.Append(source[i]);
                }

                i++;
            }

            if (bodyBuilder.Length > 0)
                row.Add(bodyBuilder.ToString());

            if (row.Count > 0)
                yield return row;
        }


        protected bool RedirectHook(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            if (type == typeof(ScenarioData))
            {
                string scenarioPath = Path.Combine(scenarioDir, Path.Combine(assetBundleName, $"{assetName}.csv")).Replace('/', '\\').Replace(".unity3d", "").Replace(@"adv\scenario\", "");
                
                if (File.Exists(scenarioPath))
                {
                    var rawData = ManualLoadAsset<ScenarioData>(assetBundleName, assetName, manifestAssetBundleName);

                    rawData.list.Clear();

                    foreach (IEnumerable<string> line in SplitAndEscape(File.ReadAllText(scenarioPath, Encoding.UTF8)))
                    {
                        string[] data = line.ToArray();
                        BepInLogger.Log(string.Join(",", data));

                        string[] args = new string[data.Length - 4];

                        Array.Copy(data, 4, args, 0, args.Length);

                        ScenarioData.Param param = new ScenarioData.Param(bool.Parse(data[3]), (Command)int.Parse(data[2]), args);

                        param.SetHash(int.Parse(data[0]));

                        rawData.list.Add(param);
                    }

                    result = new AssetBundleLoadAssetOperationSimulation(rawData);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
