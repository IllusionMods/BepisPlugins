using ActionGame;
using BepInEx.Logging;
using Harmony;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader.Image
{
    public static class ImageHooks
    {
        public static void InstallHooks()
        {
            try
            {
                var harmony = HarmonyInstance.Create("com.bepis.bepinex.dynamictranslationloader");
                harmony.PatchAll(typeof(ImageHooks));
            }
            catch (System.Exception e)
            {
                Logger.Log(LogLevel.Error, e);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GraphicRegistry), "RegisterGraphicForCanvas")]
        public static bool RegisterGraphicForCanvasHook(ref Canvas c, ref Graphic graphic)
        {
            if (graphic)
            {
                var go = graphic.gameObject;
                if (go == null) return true;
                ImageTranslator.TranslateComponents(go);
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Cursor), "SetCursor", new[] { typeof(Texture2D), typeof(Vector2), typeof(CursorMode) })]
        public static bool SetCursorHook(ref Texture2D texture)
        {
            var scene = "Cursors";

            ImageTranslator.RegisterTexture(texture, string.Empty, scene);
            ImageTranslator.ReplaceTexture(texture, string.Empty, scene);
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Selectable), "DoSpriteSwap")]
        public static void DoSpriteSwapHook(ref Selectable __instance, ref Sprite newSprite)
        {
            if (newSprite == null) newSprite = __instance.image.sprite;
            var go = __instance.gameObject;
            var path = GameObjectUtils.AbsoluteTransform(go);
            var scene = go.scene.name;

            ImageTranslator.RegisterTexture(newSprite, path, scene);
            ImageTranslator.ReplaceTexture(ref newSprite, path, scene);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Illusion.Game.Utils.Bundle), "LoadSprite")]
        public static void LoadSpriteHook(ref UnityEngine.UI.Image image)
        {
            var go = image.gameObject;
            var path = GameObjectUtils.AbsoluteTransform(go);
            var scene = go.scene.name;

            ImageTranslator.TranslateImage(image, path, scene);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ActionChangeUI), "Set", new[] { typeof(int) })]
        public static void ActionChangeUISetHook(ref ActionChangeUI __instance)
        {
            var image = (UnityEngine.UI.Image)AccessTools.Field(typeof(ActionChangeUI), "target").GetValue(__instance);
            if (image == null) return;
            var go = __instance.gameObject;
            var path = GameObjectUtils.AbsoluteTransform(go);
            var scene = go.scene.name;

            ImageTranslator.RegisterTexture(image, path, scene);
            ImageTranslator.ReplaceTexture(image, path, scene);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CheckScene), "Start")]
        public static void CheckSceneHook(ref Studio.CheckScene __instance)
        {
            foreach (var image in __instance.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                image.enabled = false;
                image.enabled = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.NotificationScene), "Awake")]
        public static void NotificationSceneHook(ref Studio.NotificationScene __instance)
        {
            foreach (var image in __instance.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                image.enabled = false;
                image.enabled = true;
            }
        }
    }
}