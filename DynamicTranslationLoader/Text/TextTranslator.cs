using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ADV;
using BepInEx;
using BepInEx.Logging;
using TARC.Compiler;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = BepInEx.Logger;
using Object = UnityEngine.Object;

namespace DynamicTranslationLoader.Text
{
    public class TextTranslator
    {
	    public static List<Archive> TLArchives = new List<Archive>();
	    private static readonly Dictionary<string, CompiledLine> Translations = new Dictionary<string, CompiledLine>();
	    private static readonly Dictionary<Regex, CompiledLine> regexTranslations = new Dictionary<Regex, CompiledLine>();

        private static readonly Dictionary<WeakReference, string> OriginalTranslations = new Dictionary<WeakReference, string>();
        private static readonly HashSet<string> Untranslated = new HashSet<string>();

        private static readonly string ScenarioDir = Path.Combine(Paths.PluginPath, @"translation\scenario");
        private static readonly string CommunicationDir = Path.Combine(Paths.PluginPath, @"translation\communication");

	    private static readonly string CurrentExe = Paths.ProcessName;

        public static void LoadTextTranslations(string dirTranslation)
        {
	        Logger.Log(LogLevel.Debug, "Loading all translations");
	        //TODO: load .bin files here
			
	        Logger.Log(LogLevel.Debug, $"Loaded {TLArchives.Count} archives");
	        TLArchives.Clear();
	        var dirTranslationText = Path.Combine(dirTranslation, "Text");
	        if (!Directory.Exists(dirTranslationText))
		        Directory.CreateDirectory(dirTranslationText);
	        try
	        {
		        TLArchives.Add(new MarkupCompiler().CompileArchive(dirTranslationText));
		        Logger.Log(LogLevel.Debug, $"Loaded {TLArchives.Last().Sections.Sum(x => x.Lines.Count)} lines from text");
	        }
	        catch (Exception ex)
	        {
		        Logger.Log(LogLevel.Error | LogLevel.Message, "Unable to load translations from text!");
		        Logger.Log(LogLevel.Error, ex);
	        }

        }

	    private static bool TryGetRegex(string input, out CompiledLine line, out Match regexMatch)
	    {
	        foreach (var kv in regexTranslations)
		    {
		        Match match = kv.Key.Match(input);
		        if (!match.Success)
		            continue;

		        line = kv.Value;
		        regexMatch = match;
		        return true;
		    }

		    line = null;
		    regexMatch = null;
		    return false;
	    }

        public static string TranslateText(string input, object obj)
        {
            if (DynamicTranslator.IsPastingToClipboard.Value)
                GUIUtility.systemCopyBuffer = input;

	        if(string.IsNullOrEmpty(input)) 
		        return input;

            // Consider changing this! You have a dictionary, but you iterate instead of making a lookup. Why do you not use the WeakKeyDictionary, you have instead? 
            if (OriginalTranslations.All(x => x.Key.Target != obj)
            ) //check if we don't have the object in the dictionary
                OriginalTranslations.Add(new WeakReference(obj), input);

	        if (Translations.TryGetValue(input.Trim(), out CompiledLine translation))
	        {
				return translation.TranslatedLine;
	        }

            if (TryGetRegex(input, out translation, out Match match))
            {
                return translation.TranslatedLine;
            }

            if (obj is UnityEngine.UI.Text)
            {
                var immediatelyTranslated = DynamicTranslator.OnOnUnableToTranslateUgui(obj, input);
                if (immediatelyTranslated != null) return immediatelyTranslated;
            }
            else if (obj is TMP_Text)
            {
                var immediatelyTranslated = DynamicTranslator.OnOnUnableToTranslateTextMeshPro(obj, input);
                if (immediatelyTranslated != null) return immediatelyTranslated;
            }
			
            if (!Untranslated.Contains(input))
                Untranslated.Add(input);

            return input;
        }

        public static void TranslateTextAll()
        {
            foreach (var textMesh in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                //gameObject.text = "Harsh is shit";

                textMesh.text = TranslateText(textMesh.text, textMesh);
            }
        }

        private static void UntranslateTextAll()
        {
            TextHooks.TranslationHooksEnabled = false;

            var aliveCount = 0;

            foreach (var kv in OriginalTranslations)
            {
                if (!kv.Key.IsAlive) continue;

                aliveCount++;

                switch (kv.Key.Target)
                {
                    case TMP_Text tmtext:
                        tmtext.text = kv.Value;
                        break;

                    case UnityEngine.UI.Text tmtext:
                        tmtext.text = kv.Value;
                        break;
                }
            }

            Logger.Log(LogLevel.Message, $"{aliveCount} translations reloaded.");

            TextHooks.TranslationHooksEnabled = true;
        }

        internal static void RetranslateText()
        {
            UntranslateTextAll();

            LoadTextTranslations(Path.Combine(Paths.PluginPath, "translation"));

            TranslateTextAll();
        }

	    private static void LoadSceneTranslations(int sceneIndex)
	    {
		    Logger.Log(LogLevel.Debug, $"Loading translations for scene {sceneIndex}");
		    Translations.Clear();
		    regexTranslations.Clear();
		    foreach (Archive arc in TLArchives)
		    foreach (Section section in arc.Sections)
		    {
			    if (!section.Exe.Equals("all", StringComparison.OrdinalIgnoreCase) && !section.Exe.Equals(CurrentExe, StringComparison.OrdinalIgnoreCase))
				    continue;
			    foreach (var line in section.Lines)
			    {
				    if (line.Levels.Any(x => x == (byte)sceneIndex || x == 255))
				    {
					    if (line.Flags.IsOriginalRegex)
						    regexTranslations[new Regex(line.OriginalLine)] = line;
					    else
						    Translations[line.OriginalLine] = line;
				    }
			    }
		    }

			
		    Logger.Log(LogLevel.Debug, $"Filtered {Translations.Count} / {TLArchives.Sum(x => x.Sections.Sum(y => y.Lines.Count))} lines");
	    }

        public static void TranslateScene(Scene scene, LoadSceneMode loadMode)
        {
			LoadSceneTranslations(scene.buildIndex);

	        Logger.Log(LogLevel.Debug, $"Translating scene {scene.buildIndex}");
	        foreach (GameObject obj in scene.GetRootGameObjects())
	        foreach (TextMeshProUGUI gameObject in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
	        {
		        //gameObject.text = "Harsh is shit";
		        gameObject.text = TranslateText(gameObject.text, gameObject);
	        }
        }

        internal static void DumpText()
        {
            var output = string.Empty;

            foreach (var text in Untranslated)
                if (!Regex.Replace(text, @"[\d-]", string.Empty).IsNullOrWhiteSpace()
                    && !text.Contains("Reset"))
                    output += $"{text.Trim()}=\r\n";

            File.WriteAllText("dumped-tl.txt", output);
        }

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
        {
	        AssetBundleManager.LoadAssetBundleInternal(bundle, false, manifest);
	        var assetBundle = AssetBundleManager.GetLoadedAssetBundle(bundle, out string error, manifest);

	        T output = assetBundle.m_AssetBundle.LoadAsset<T>(asset);

            return output;
        }

        protected static IEnumerable<IEnumerable<string>> SplitAndEscape(string source)
        {
            var bodyBuilder = new StringBuilder();

            // here we build rows, one by one
            var i = 0;
            var row = new List<string>();
            var limit = source.Length;
            var inQuote = false;

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
                    {
                        inQuote = true;
                    }
                    else
                    {
                        if (i + 1 < limit
                            && source[i + 1] == '"')
                        {
                            bodyBuilder.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuote = false;
                        }
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

        public static bool RedirectHook(string assetBundleName, string assetName, Type type,
            string manifestAssetBundleName, out AssetBundleLoadAssetOperation result)
        {
            if (type == typeof(ScenarioData))
            {
                var scenarioPath = Path.Combine(ScenarioDir, Path.Combine(assetBundleName, $"{assetName}.csv"))
                    .Replace('/', '\\')
                    .Replace(".unity3d", "")
                    .Replace(@"adv\scenario\", "");

                if (File.Exists(scenarioPath))
                {
                    var rawData = ManualLoadAsset<ScenarioData>(assetBundleName, assetName, manifestAssetBundleName);

                    rawData.list.Clear();

                    foreach (var line in SplitAndEscape(File.ReadAllText(scenarioPath, Encoding.UTF8)))
                    {
                        var data = line.ToArray();

                        var args = new string[data.Length - 4];

                        Array.Copy(data, 4, args, 0, args.Length);

                        var param = new ScenarioData.Param(bool.Parse(data[3]), (Command) int.Parse(data[2]), args);

                        param.SetHash(int.Parse(data[0]));

                        rawData.list.Add(param);
                    }

                    result = new AssetBundleLoadAssetOperationSimulation(rawData);
                    return true;
                }
            }
            else if (type == typeof(ExcelData))
            {
                var communicationPath = Path.Combine(CommunicationDir,
                        Path.Combine(assetBundleName.Replace("communication/", ""), $"{assetName}.csv"))
                    .Replace('/', '\\')
                    .Replace(".unity3d", "");

                if (File.Exists(communicationPath))
                {
                    var rawData = ManualLoadAsset<ExcelData>(assetBundleName, assetName, manifestAssetBundleName);

                    rawData.list.Clear();

                    foreach (var line in SplitAndEscape(File.ReadAllText(communicationPath, Encoding.UTF8)))
                    {
	                    var list = line.ToList();

                        var param = new ExcelData.Param
                        {
                            list = list
                        };

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