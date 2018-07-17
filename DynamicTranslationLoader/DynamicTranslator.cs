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
using BepInEx.Logging;
using H;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader
{
    [BepInPlugin(GUID: "com.bepis.bepinex.dynamictranslator", Name: "Dynamic Translator", Version: "3.0")]
    public class DynamicTranslator : BaseUnityPlugin
    {
        private static readonly Dictionary<string, string> translations = new Dictionary<string, string>();
        private static readonly Dictionary<WeakReference, string> originalTranslations = new Dictionary<WeakReference, string>();
        private static readonly HashSet<string> untranslated = new HashSet<string>();

        public static event Func<object, string, string> OnUnableToTranslateUGUI;
        public static event Func<object, string, string> OnUnableToTranslateTextMeshPro;

        // Settings
        public SavedKeyboardShortcut ReloadTranslations { get; }
        public SavedKeyboardShortcut DumpUntranslatedText { get; }
        public static ConfigWrapper<bool> IsDumpingEnabled { get; private set; }
        public static ConfigWrapper<bool> DumpingAllToGlobal { get; private set; }

        //ITL
        public static readonly string GlobalTextureTargetName = "_Global";
        private static bool GlobalTextureTargetExists { get; set; }
        private static string TL_DIR_ROOT;
        private static string TL_DIR_SCENE;
        private static readonly Dictionary<string, Dictionary<string, byte[]>> textureLoadTargets = new Dictionary<string, Dictionary<string, byte[]>>();
        private static readonly Dictionary<string, HashSet<TextureMetadata>> textureDumpTargets = new Dictionary<string, HashSet<TextureMetadata>>();
        private static readonly Dictionary<string, FileStream> fs_textureNameDump = new Dictionary<string, FileStream>();
        private static readonly Dictionary<string, StreamWriter> sw_textureNameDump = new Dictionary<string, StreamWriter>();
        private static readonly IEqualityComparer<TextureMetadata> tmdc = new TextureMetadataComparer();

        public DynamicTranslator()
        {
            IsDumpingEnabled = new ConfigWrapper<bool>("!Enable image dumping", this);
            DumpingAllToGlobal = new ConfigWrapper<bool>("Dump all images to global folder", this);
            ReloadTranslations = new SavedKeyboardShortcut("Reload translations", this, new KeyboardShortcut(KeyCode.F10));
            DumpUntranslatedText = new SavedKeyboardShortcut("Dump untranslated text", this, new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift));
        }

        private void Awake()
        {
            LoadTranslations();

            Hooks.InstallHooks();

            ResourceRedirector.ResourceRedirector.AssetResolvers.Add(RedirectHook);

            TranslateAll();
        }

        private void LoadTranslations()
        {
            translations.Clear();
            var dirTranslation = Path.Combine(Utility.PluginsDirectory, "translation");
            var dirTranslationText = Path.Combine(dirTranslation, "Text");

            if (!Directory.Exists(dirTranslationText))
                Directory.CreateDirectory(dirTranslationText);

            var translation = Directory.GetFiles(dirTranslationText, "*.txt", SearchOption.AllDirectories)
                .SelectMany(File.ReadAllLines)
                .ToArray();

            foreach (var line in translation)
            {
                if (!line.Contains('='))
                    continue;

                var split = line.Split('=');
                if (split.Length != 2)
                {
                    Logger.Log(LogLevel.Warning, "Invalid text translation entry: " + line);
                    continue;
                }

                translations[split[0].Trim()] = split[1];
            }

            //ITL
            var di_tl = new DirectoryInfo(Path.Combine(dirTranslation, "Images"));

            TL_DIR_ROOT = $"{di_tl.FullName}/{Application.productName}";
            TL_DIR_SCENE = $"{TL_DIR_ROOT}/Scenes";

            var di = new DirectoryInfo(TL_DIR_SCENE);
            if (!di.Exists) di.Create();

            foreach (var t in new DirectoryInfo(TL_DIR_ROOT).GetFiles("*.txt"))
            {
                var sceneName = Path.GetFileNameWithoutExtension(t.Name);
                textureLoadTargets[sceneName] = new Dictionary<string, byte[]>();
                foreach (var tl in File.ReadAllLines(t.FullName))
                {
                    var tp = tl.Split('=');
                    if (tp.Length != 2)
                    {
                        Logger.Log(LogLevel.Warning, "Invalid entry in " + t.Name + " - " + tl);
                        continue;
                    }
                    var path = $"{TL_DIR_SCENE}/{tp[1]}";
                    if (!File.Exists(path))
                    {
                        Logger.Log(LogLevel.Warning, "Missing TL image: " + path);
                        continue;
                    }
                    textureLoadTargets[sceneName][tp[0]] = File.ReadAllBytes(path);
                }
            }

            GlobalTextureTargetExists = textureLoadTargets.ContainsKey(GlobalTextureTargetName);

            SceneManager.sceneUnloaded += s =>
            {
                if (IsDumpingEnabled.Value)
                {
                    var sn = DumpingAllToGlobal.Value ? GlobalTextureTargetName : s.name;
                    if (sw_textureNameDump.TryGetValue(sn, out var sw))
                        sw.Flush();
                }
            };
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            //TranslateScene(scene);
        }

        public static string Translate(string input, object obj)
        {
            GUIUtility.systemCopyBuffer = input;

            if (string.IsNullOrEmpty(input)) return input;

            // Consider changing this! You have a dictionary, but you iterate instead of making a lookup. Why do you not use the WeakKeyDictionary, you have instead? 
            if (originalTranslations.All(x => x.Key.Target != obj)) //check if we don't have the object in the dictionary
            {
                //add to untranslated list
                originalTranslations.Add(new WeakReference(obj), input);
            }

            if (translations.TryGetValue(input.Trim(), out var translation))
                return translation;

            if (obj is Text)
            {
                var immediatelyTranslated = OnUnableToTranslateUGUI?.Invoke(obj, input);
                if (immediatelyTranslated != null) return immediatelyTranslated;
            }
            else if (obj is TMP_Text)
            {
                var immediatelyTranslated = OnUnableToTranslateTextMeshPro?.Invoke(obj, input);
                if (immediatelyTranslated != null) return immediatelyTranslated;
            }

            // Consider changing this! You make a value lookup in a dictionary, which scales really poorly
            if (!untranslated.Contains(input) && !translations.ContainsValue(input))
                untranslated.Add(input);

            return input;
        }

        private void TranslateAll()
        {
            foreach (var textMesh in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                //gameObject.text = "Harsh is shit";

                textMesh.text = Translate(textMesh.text, textMesh);
            }
        }

        private void UntranslateAll()
        {
            Hooks.TranslationHooksEnabled = false;

            var aliveCount = 0;

            foreach (var kv in originalTranslations)
            {
                if (!kv.Key.IsAlive) continue;

                aliveCount++;

                switch (kv.Key.Target)
                {
                    case TMP_Text tmtext:
                        tmtext.text = kv.Value;
                        break;

                    case Text tmtext:
                        tmtext.text = kv.Value;
                        break;
                }
            }

            Logger.Log(LogLevel.Message, $"{aliveCount} translations reloaded.");

            Hooks.TranslationHooksEnabled = true;
        }

        private void Retranslate()
        {
            UntranslateAll();

            LoadTranslations();

            TranslateAll();
        }

        private void TranslateScene(Scene scene)
        {
            //foreach (GameObject obj in scene.GetRootGameObjects())
            //    foreach (TextMeshProUGUI gameObject in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            //    {
            //        //gameObject.text = "Harsh is shit";

            //        gameObject.text = Translate(gameObject.text, gameObject);
            //    }
        }

        private void Dump()
        {
            var output = string.Empty;

            var fullUntranslated = originalTranslations
                .Where(x => !translations.ContainsKey(x.Value))
                .Select(x => x.Value)
                .Distinct()
                .Union(untranslated);

            foreach (var text in fullUntranslated)
            {
                if (!Regex.Replace(text, @"[\d-]", string.Empty).IsNullOrWhiteSpace()
                        && !text.Contains("Reset"))
                    output += $"{text.Trim()}=\r\n";
            }

            File.WriteAllText("dumped-tl.txt", output);
        }

        public void Update()
        {
            if (Event.current == null) return;
            if (ReloadTranslations.IsDown())
            {
                Retranslate();
                Logger.Log(LogLevel.Message, "Translation reloaded.");
            }
            else if (DumpUntranslatedText.IsDown())
            {
                Dump();
                Logger.Log(LogLevel.Message, $"Text dumped to \"{Path.GetFullPath("dumped-tl.txt")}\"");
            }
        }

        #region ITL
        internal static void PrepDumper(string s)
        {
            if (IsDumpingEnabled.Value)
            {
                if (DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                if (textureDumpTargets.ContainsKey(s)) return;
                textureDumpTargets[s] = new HashSet<TextureMetadata>(tmdc);
                fs_textureNameDump[s] = new FileStream($"{TL_DIR_ROOT}/dump_{s}.txt", FileMode.Create, FileAccess.Write);  //TODO: Sanitise scene name?
                sw_textureNameDump[s] = new StreamWriter(fs_textureNameDump[s]);
            }
        }

        internal static bool IsSwappedTexture(Texture t) => t.name.StartsWith("*");

        private static bool TryGetOverrideTexture(string texName, string sceneName, out byte[] tex)
        {
            if (textureLoadTargets.ContainsKey(sceneName) &&
                textureLoadTargets[sceneName].ContainsKey(texName))
            {
                tex = textureLoadTargets[sceneName][texName];
                return true;
            }
            if (GlobalTextureTargetExists &&
                     textureLoadTargets[GlobalTextureTargetName].ContainsKey(texName))
            {
                tex = textureLoadTargets[GlobalTextureTargetName][texName];
                return true;
            }
            tex = null;
            return false;
        }

        internal static void ReplaceTexture(Texture2D t2d, string path, string s)
        {
            if (t2d == null) return;

            if (TryGetOverrideTexture(t2d.name, s, out var tex) && !IsSwappedTexture(t2d))
            {
                t2d.LoadImage(tex);
                t2d.name = "*" + t2d.name;
            }
        }

        internal static void ReplaceTexture(Material mat, string path, string s)
        {
            if (mat == null) return;
            ReplaceTexture((Texture2D)mat.mainTexture, path, s);
        }

        private static string GetAtlasTextureName(Sprite spr)
        {
            var rect = spr.textureRect;
            return $"[{rect.width},{rect.height},{rect.x},{rect.y}]{spr.texture.name}";
        }

        private static string GetAtlasTextureName(Image i)
        {
            var rect = i.sprite.textureRect;
            return $"[{rect.width},{rect.height},{rect.x},{rect.y}]{i.mainTexture.name}";
        }

        internal static void ReplaceTexture(Image img, string path, string s)
        {
            ReplaceTexture(img.material, path, s);

            if (img.sprite == null) return;

            if (!GlobalTextureTargetExists && !textureLoadTargets.ContainsKey(s)) return;

            var mainTexture = img.mainTexture;
            if (mainTexture == null) return;

            var rect = img.sprite.textureRect;
            if (rect == new Rect(0, 0, mainTexture.width, mainTexture.height))
            {
                if (IsSwappedTexture(mainTexture)) return;
                if (string.IsNullOrEmpty(mainTexture.name)) return;

                if (TryGetOverrideTexture(img.mainTexture.name, s, out var newTexture))
                {
                    var t2D = new Texture2D(2, 2);
                    t2D.LoadImage(newTexture);
                    img.sprite = Sprite.Create(t2D, img.sprite.rect, img.sprite.pivot);
                    mainTexture.name = "*" + img.mainTexture.name;
                }
            }
            else
            {
                //Atlas
                if (IsSwappedTexture(img.sprite.texture)) return;
                var name = GetAtlasTextureName(img);
                if (TryGetOverrideTexture(img.mainTexture.name, s, out var newTex))
                {
                    img.sprite.texture.LoadImage(newTex);
                    img.sprite.texture.name = "*" + img.mainTexture.name;
                }
                else if (TryGetOverrideTexture(name, s, out newTex))
                {
                    var t2D = new Texture2D(2, 2);
                    t2D.LoadImage(newTex);
                    img.sprite = Sprite.Create(t2D, new Rect(0, 0, t2D.width, t2D.height), Vector2.zero);
                    img.sprite.texture.name = "*" + name;
                }
            }
        }

        internal static void ReplaceTexture(RawImage img, string path, string s)
        {
            ReplaceTexture(img.material, path, s);
        }

        internal static void RegisterTexture(Texture tex, string path, string s)
        {
            if (IsDumpingEnabled.Value)
            {
                if (tex == null) return;
                if (IsSwappedTexture(tex)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                var tm = new TextureMetadata(tex, path, s);
                if (textureDumpTargets[s].Contains(tm)) return;
                textureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        private static readonly Dictionary<string, Texture2D> readableTextures = new Dictionary<string, Texture2D>();

        internal static void RegisterTexture(Sprite spr, string path, string s)
        {
            if (IsDumpingEnabled.Value)
            {
                if (spr == null) return;
                var tex = spr.texture;
                if (IsSwappedTexture(spr.texture)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                RegisterTexture(tex, path, s);

                var rect = spr.textureRect;
                if (rect == new Rect(0, 0, tex.width, tex.height)) return;
                if (!readableTextures.TryGetValue(tex.name, out var readable))
                {
                    readableTextures[tex.name] = TextureUtils.MakeReadable(tex);
                    readable = readableTextures[tex.name];
                }
                var cropped = readable.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                var nt2d = new Texture2D((int)rect.width, (int)rect.height);
                nt2d.SetPixels(cropped);
                nt2d.Apply();

                nt2d.name = spr.texture.name.ToLower().Contains("atlas") ? GetAtlasTextureName(spr) : spr.texture.name;
                var tm = new TextureMetadata(nt2d, path, s);

                if (textureDumpTargets[s].Contains(tm)) return;
                textureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        internal static void RegisterTexture(Image i, string path, string s)
        {
            if (IsDumpingEnabled.Value)
            {
                var tex = i.mainTexture;
                if (tex == null) return;
                if (IsSwappedTexture(tex)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                RegisterTexture(i.mainTexture, path, s);
                if (i.sprite == null) return;

                var rect = i.sprite.textureRect;
                if (rect == new Rect(0, 0, tex.width, tex.height))
                {
                    RegisterTexture(i.mainTexture, path, s);
                    return;
                }
                if (!readableTextures.TryGetValue(tex.name, out var readable))
                {
                    readableTextures[tex.name] = TextureUtils.MakeReadable(tex);
                    readable = readableTextures[tex.name];
                }
                var cropped = readable.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                var nt2d = new Texture2D((int)rect.width, (int)rect.height);
                nt2d.SetPixels(cropped);
                nt2d.Apply();

                nt2d.name = tex.name.ToLower().Contains("atlas") ? GetAtlasTextureName(i) : tex.name;
                var tm = new TextureMetadata(nt2d, path, s);

                if (textureDumpTargets[s].Contains(tm)) return;
                textureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        internal static void ReplaceTexture(ref Sprite spr, string path, string s)
        {
            if (spr == null || spr.texture == null) return;

            if (!GlobalTextureTargetExists && !textureLoadTargets.ContainsKey(s)) return;

            if (IsSwappedTexture(spr.texture)) return;

            if (TryGetOverrideTexture(spr.texture.name, s, out var newTex))
            {
                if (spr.texture.name.ToLower().Contains("atlas"))
                {
                    spr.texture.LoadImage(newTex);
                }
                else
                {
                    var t2D = new Texture2D(2, 2);
                    t2D.LoadImage(newTex);

                    spr = Sprite.Create(t2D, spr.rect, spr.pivot);
                }
            }
            else
            {
                var name = GetAtlasTextureName(spr);
                if (!TryGetOverrideTexture(name, s, out var tex)) return;

                Console.WriteLine(name);

                var t2D = new Texture2D(2, 2);
                t2D.LoadImage(tex);

                spr = Sprite.Create(t2D, spr.rect, spr.pivot);
            }
            spr.texture.name = "*" + spr.texture.name;
        }

        internal static void RegisterSpriteState(ref SpriteState sprState, string path, string s)
        {
            if (sprState.disabledSprite != null)
            {
                RegisterTexture(sprState.disabledSprite?.texture, path, s);
                var spr = sprState.disabledSprite;
                ReplaceTexture(ref spr, path, s);
                sprState.disabledSprite = spr;
            }
            if (sprState.highlightedSprite != null)
            {
                RegisterTexture(sprState.highlightedSprite?.texture, path, s);
                var spr = sprState.highlightedSprite;
                ReplaceTexture(ref spr, path, s);
                sprState.highlightedSprite = spr;
            }
            if (sprState.pressedSprite != null)
            {
                RegisterTexture(sprState.pressedSprite?.texture, path, s);
                var spr = sprState.pressedSprite;
                ReplaceTexture(ref spr, path, s);
                sprState.pressedSprite = spr;
            }
        }

        internal static void RegisterSprites(ref Sprite[] sprites, string path, string s)
        {
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    RegisterTexture(sprites[i]?.texture, path, s);
                    var spr = sprites[i];
                    ReplaceTexture(ref spr, path, s);
                    sprites[i] = spr;
                }
            }
        }

        private static void TranslateButton(Button b, string path, string scene)
        {
            var ss = b.spriteState;
            RegisterSpriteState(ref ss, path, scene);
            b.spriteState = ss;
        }

        private static void TranslateRawImage(RawImage ri, string path, string scene)
        {
            RegisterTexture(ri.mainTexture, path, scene);
            ReplaceTexture(ri, path, scene);
        }

        public static void TranslateImage(Image i, string path, string scene)
        {
            RegisterTexture(i, path, scene);
            ReplaceTexture(i, path, scene);
        }

        private static void TranslateHSpriteChangeCtrl(SpriteChangeCtrl hscc, string path, string scene)
        {
            var sprs = hscc.sprites;
            RegisterSprites(ref sprs, path, scene);
            hscc.sprites = sprs;
        }

        public static void TranslateComponents(GameObject go)
        {
            var zettai = GameObjectUtils.AbsoluteTransform(go);
            var scene = go.scene.name;
            if (scene == "DontDestroyOnLoad")
                scene = SceneManager.GetActiveScene().name;
            foreach (var comp in go.GetComponents<Component>())
            {
                switch (comp)
                {
                    case Image i:
                        TranslateImage(i, zettai, scene);
                        break;
                    case RawImage i:
                        TranslateRawImage(i, zettai, scene);
                        break;
                    case Button b:
                        TranslateButton(b, zettai, scene);
                        break;
                    case SpriteChangeCtrl s:
                        TranslateHSpriteChangeCtrl(s, zettai, scene);
                        break;
                }
            }
        }

        internal static void DumpTexture(TextureMetadata tm)
        {
            var dir = $"{TL_DIR_SCENE}/{tm.scene}";
            var di = new DirectoryInfo(dir);
            if (!di.Exists) di.Create();
            var path = $"{dir}/{tm.SafeID}.png";
            if (!File.Exists(path)) TextureUtils.SaveTex(tm.texture, path);

            var s = DumpingAllToGlobal.Value ? GlobalTextureTargetName : tm.scene;
            var sw = sw_textureNameDump[s];
            if (sw == null) return;
            //if (sw.BaseStream == null) return;
            sw.WriteLine("{0}={1}", tm.SafeID, path.Replace(TL_DIR_SCENE + "/", ""));
            sw.Flush();
        }

        #endregion

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

        #region Scenario & Communication Translation
        private static readonly string scenarioDir = Path.Combine(Utility.PluginsDirectory, "translation\\scenario");
        private static readonly string communicationDir = Path.Combine(Utility.PluginsDirectory, "translation\\communication");

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : UnityEngine.Object
        {
            var path = $@"{Application.dataPath}\..\{(string.IsNullOrEmpty(manifest) ? "abdata" : manifest)}\{bundle}";

            var assetBundle = AssetBundle.LoadFromFile(path);

            var output = assetBundle.LoadAsset<T>(asset);
            assetBundle.Unload(false);

            return output;
        }

        protected IEnumerable<IEnumerable<string>> SplitAndEscape(string source)
        {
            StringBuilder bodyBuilder = new StringBuilder();

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
                var scenarioPath = Path.Combine(scenarioDir, Path.Combine(assetBundleName, $"{assetName}.csv"))
                    .Replace('/', '\\')
                    .Replace(".unity3d", "")
                    .Replace(@"adv\scenario\", "");

                if (File.Exists(scenarioPath))
                {
                    var rawData = ManualLoadAsset<ScenarioData>(assetBundleName, assetName, manifestAssetBundleName);

                    rawData.list.Clear();

                    foreach (IEnumerable<string> line in SplitAndEscape(File.ReadAllText(scenarioPath, Encoding.UTF8)))
                    {
                        var data = line.ToArray();

                        var args = new string[data.Length - 4];

                        Array.Copy(data, 4, args, 0, args.Length);

                        var param = new ScenarioData.Param(bool.Parse(data[3]), (Command)int.Parse(data[2]), args);

                        param.SetHash(int.Parse(data[0]));

                        rawData.list.Add(param);
                    }

                    result = new AssetBundleLoadAssetOperationSimulation(rawData);
                    return true;
                }
            }
            else if (type == typeof(ExcelData))
            {
                var communicationPath = Path.Combine(communicationDir,
                    Path.Combine(assetBundleName.Replace("communication/", ""), $"{assetName}.csv"))
                    .Replace('/', '\\')
                    .Replace(".unity3d", "");

                if (File.Exists(communicationPath))
                {
                    var rawData = ManualLoadAsset<ExcelData>(assetBundleName, assetName, manifestAssetBundleName);

                    rawData.list.Clear();

                    foreach (IEnumerable<string> line in SplitAndEscape(File.ReadAllText(communicationPath, Encoding.UTF8)))
                    {
                        var param = new ExcelData.Param
                        {
                            list = line.ToList()
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
        #endregion
    }
}