using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using H;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader.Image
{
    public class ImageTranslator
    {
        public static readonly string GlobalTextureTargetName = "_Global";
        private static bool GlobalTextureTargetExists { get; set; }

        private static string TL_DIR_ROOT;
        private static string TL_DIR_SCENE;

        private static readonly Dictionary<string, Dictionary<string, byte[]>> TextureLoadTargets =
            new Dictionary<string, Dictionary<string, byte[]>>();
        private static readonly Dictionary<string, HashSet<TextureMetadata>> TextureDumpTargets =
            new Dictionary<string, HashSet<TextureMetadata>>();

        private static readonly Dictionary<string, FileStream> FsTextureNameDump = new Dictionary<string, FileStream>();
        private static readonly Dictionary<string, StreamWriter> SwTextureNameDump = new Dictionary<string, StreamWriter>();

        private static readonly IEqualityComparer<TextureMetadata> Tmdc = new TextureMetadataComparer();

        private static readonly Dictionary<string, Texture2D> ReadableTextures = new Dictionary<string, Texture2D>();

        public static void LoadImageTranslations(string dirTranslation)
        {
            var diTl = new DirectoryInfo(Path.Combine(dirTranslation, "Images"));

            TL_DIR_ROOT = $"{diTl.FullName}/{Application.productName}";
            TL_DIR_SCENE = $"{TL_DIR_ROOT}/Scenes";

            var di = new DirectoryInfo(TL_DIR_SCENE);
            if (!di.Exists) di.Create();

            foreach (var t in new DirectoryInfo(TL_DIR_ROOT).GetFiles("*.txt"))
            {
                var sceneName = Path.GetFileNameWithoutExtension(t.Name);
                TextureLoadTargets[sceneName] = new Dictionary<string, byte[]>();
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
                    TextureLoadTargets[sceneName][tp[0]] = File.ReadAllBytes(path);
                }
            }

            GlobalTextureTargetExists = TextureLoadTargets.ContainsKey(GlobalTextureTargetName);

            SceneManager.sceneUnloaded += s =>
            {
                if (DynamicTranslator.IsDumpingEnabled.Value)
                {
                    var sn = DynamicTranslator.DumpingAllToGlobal.Value ? GlobalTextureTargetName : s.name;
                    if (SwTextureNameDump.TryGetValue(sn, out var sw))
                        sw.Flush();
                }
            };
        }

        internal static void PrepDumper(string s)
        {
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                if (TextureDumpTargets.ContainsKey(s)) return;
                TextureDumpTargets[s] = new HashSet<TextureMetadata>(Tmdc);
                FsTextureNameDump[s] =
                    new FileStream($"{TL_DIR_ROOT}/dump_{s}.txt", FileMode.Create, FileAccess.Write); //TODO: Sanitise scene name?
                SwTextureNameDump[s] = new StreamWriter(FsTextureNameDump[s]);
            }
        }

        internal static bool IsSwappedTexture(Texture t)
        {
            return t.name.StartsWith("*");
        }

        private static bool TryGetOverrideTexture(string texName, string sceneName, out byte[] tex)
        {
            if (TextureLoadTargets.ContainsKey(sceneName) &&
                TextureLoadTargets[sceneName].ContainsKey(texName))
            {
                tex = TextureLoadTargets[sceneName][texName];
                return true;
            }
            if (GlobalTextureTargetExists &&
                TextureLoadTargets[GlobalTextureTargetName].ContainsKey(texName))
            {
                tex = TextureLoadTargets[GlobalTextureTargetName][texName];
                return true;
            }
            tex = null;
            return false;
        }

        internal static void ReplaceTexture(Texture2D t2D, string path, string s)
        {
            if (t2D == null) return;

            if (TryGetOverrideTexture(t2D.name, s, out var tex) && !IsSwappedTexture(t2D))
            {
                t2D.LoadImage(tex);
                t2D.name = "*" + t2D.name;
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

        private static string GetAtlasTextureName(UnityEngine.UI.Image i)
        {
            var rect = i.sprite.textureRect;
            return $"[{rect.width},{rect.height},{rect.x},{rect.y}]{i.mainTexture.name}";
        }

        internal static void ReplaceTexture(UnityEngine.UI.Image img, string path, string s)
        {
            ReplaceTexture(img.material, path, s);

            if (img.sprite == null) return;

            if (!GlobalTextureTargetExists && !TextureLoadTargets.ContainsKey(s)) return;

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
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                if (tex == null) return;
                if (IsSwappedTexture(tex)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                var tm = new TextureMetadata(tex, path, s);
                if (TextureDumpTargets[s].Contains(tm)) return;
                TextureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        internal static void RegisterTexture(Sprite spr, string path, string s)
        {
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                if (spr == null) return;
                var tex = spr.texture;
                if (IsSwappedTexture(spr.texture)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                RegisterTexture(tex, path, s);

                var rect = spr.textureRect;
                if (rect == new Rect(0, 0, tex.width, tex.height)) return;
                if (!ReadableTextures.TryGetValue(tex.name, out var readable))
                {
                    ReadableTextures[tex.name] = TextureUtils.MakeReadable(tex);
                    readable = ReadableTextures[tex.name];
                }
                var cropped = readable.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                var nt2D = new Texture2D((int)rect.width, (int)rect.height);
                nt2D.SetPixels(cropped);
                nt2D.Apply();

                nt2D.name = spr.texture.name.ToLower().Contains("atlas") ? GetAtlasTextureName(spr) : spr.texture.name;
                var tm = new TextureMetadata(nt2D, path, s);

                if (TextureDumpTargets[s].Contains(tm)) return;
                TextureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        internal static void RegisterTexture(UnityEngine.UI.Image i, string path, string s)
        {
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                var tex = i.mainTexture;
                if (tex == null) return;
                if (IsSwappedTexture(tex)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                RegisterTexture(i.mainTexture, path, s);
                if (i.sprite == null) return;

                var rect = i.sprite.textureRect;
                if (rect == new Rect(0, 0, tex.width, tex.height))
                {
                    RegisterTexture(i.mainTexture, path, s);
                    return;
                }
                if (!ReadableTextures.TryGetValue(tex.name, out var readable))
                {
                    ReadableTextures[tex.name] = TextureUtils.MakeReadable(tex);
                    readable = ReadableTextures[tex.name];
                }
                var cropped = readable.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                var nt2D = new Texture2D((int)rect.width, (int)rect.height);
                nt2D.SetPixels(cropped);
                nt2D.Apply();

                nt2D.name = tex.name.ToLower().Contains("atlas") ? GetAtlasTextureName(i) : tex.name;
                var tm = new TextureMetadata(nt2D, path, s);

                if (TextureDumpTargets[s].Contains(tm)) return;
                TextureDumpTargets[s].Add(tm);
                DumpTexture(tm);
            }
        }

        internal static void ReplaceTexture(ref Sprite spr, string path, string s)
        {
            if (spr == null || spr.texture == null) return;

            if (!GlobalTextureTargetExists && !TextureLoadTargets.ContainsKey(s)) return;

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
                if (sprites[i] != null)
                {
                    RegisterTexture(sprites[i]?.texture, path, s);
                    var spr = sprites[i];
                    ReplaceTexture(ref spr, path, s);
                    sprites[i] = spr;
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
            if (ri.mainTexture is Texture2D ri2d)
                ReplaceTexture(ri2d, path, scene);
            else
                ReplaceTexture(ri, path, scene);
        }

        public static void TranslateImage(UnityEngine.UI.Image i, string path, string scene)
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
                switch (comp)
                {
                    case UnityEngine.UI.Image i:
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

        internal static void DumpTexture(TextureMetadata tm)
        {
            var dir = $"{TL_DIR_SCENE}/{tm.scene}";
            var di = new DirectoryInfo(dir);
            if (!di.Exists) di.Create();
            var path = $"{dir}/{tm.SafeID}.png";
            if (!File.Exists(path)) TextureUtils.SaveTex(tm.texture, path);

            var s = DynamicTranslator.DumpingAllToGlobal.Value ? GlobalTextureTargetName : tm.scene;
            var sw = SwTextureNameDump[s];
            if (sw == null) return;
            //if (sw.BaseStream == null) return;
            sw.WriteLine("{0}={1}", tm.SafeID, path.Replace(TL_DIR_SCENE + "/", ""));
            sw.Flush();
        }
    }
}