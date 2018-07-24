﻿using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using H;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader
{
    public class ImageTranslator
    {
        public static readonly string GlobalTextureTargetName = "_Global";
        private static string TL_DIR_ROOT;
        private static string TL_DIR_SCENE;
        private static readonly Dictionary<string, Dictionary<string, byte[]>> textureLoadTargets = new Dictionary<string, Dictionary<string, byte[]>>();
        private static readonly Dictionary<string, HashSet<TextureMetadata>> textureDumpTargets = new Dictionary<string, HashSet<TextureMetadata>>();
        private static readonly Dictionary<string, FileStream> fs_textureNameDump = new Dictionary<string, FileStream>();
        private static readonly Dictionary<string, StreamWriter> sw_textureNameDump = new Dictionary<string, StreamWriter>();
        private static readonly IEqualityComparer<TextureMetadata> tmdc = new TextureMetadataComparer();
        private static readonly Dictionary<string, Texture2D> readableTextures = new Dictionary<string, Texture2D>();
        private static bool GlobalTextureTargetExists { get; set; }

        public static void LoadImageTranslations(string dirTranslation)
        {
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
                if (DynamicTranslator.IsDumpingEnabled.Value)
                {
                    var sn = DynamicTranslator.DumpingAllToGlobal.Value ? GlobalTextureTargetName : s.name;
                    if (sw_textureNameDump.TryGetValue(sn, out var sw))
                        sw.Flush();
                }
            };
        }

        internal static void PrepDumper(string s)
        {
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

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
            if (DynamicTranslator.IsDumpingEnabled.Value)
            {
                if (tex == null) return;
                if (IsSwappedTexture(tex)) return;
                if (string.IsNullOrEmpty(tex.name)) return;

                if (DynamicTranslator.DumpingAllToGlobal.Value) s = GlobalTextureTargetName;

                PrepDumper(s);
                var tm = new TextureMetadata(tex, path, s);
                if (textureDumpTargets[s].Contains(tm)) return;
                textureDumpTargets[s].Add(tm);
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

            var s = DynamicTranslator.DumpingAllToGlobal.Value ? GlobalTextureTargetName : tm.scene;
            var sw = sw_textureNameDump[s];
            if (sw == null) return;
            //if (sw.BaseStream == null) return;
            sw.WriteLine("{0}={1}", tm.SafeID, path.Replace(TL_DIR_SCENE + "/", ""));
            sw.Flush();
        }
    }
}