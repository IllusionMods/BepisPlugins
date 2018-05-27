using System.Text;
using Harmony;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicTranslationLoader
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            try
            {
                var harmony = HarmonyInstance.Create("com.bepis.bepinex.dynamictranslationloader");
                harmony.PatchAll(typeof(Hooks));
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        public static bool TranslationHooksEnabled { get; set; } = true;

        #region Initialization Hooks
        // With these hooks, we do not need the sceneLoaded event to translate texts

        [HarmonyPostfix, HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
        public static void AwakeHook(TextMeshProUGUI __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    var newText = DynamicTranslator.Translate(__instance.text, __instance);
                    if (newText != null)
                    {
                        __instance.text = newText;
                    }
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TextMeshPro), "Awake")]
        public static void AwakeHook(TextMeshPro __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    var newText = DynamicTranslator.Translate(__instance.text, __instance);
                    if (newText != null)
                    {
                        __instance.text = newText;
                    }
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Text), "OnEnable")]
        public static void OnEnableHook(Text __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    var newText = DynamicTranslator.Translate(__instance.text, __instance);
                    if (newText != null)
                    {
                        __instance.text = newText;
                    }
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        #endregion

        #region Text Chaange Hooks

        // NOTE: Splitting these two hooks AWAY from eachother fixed the primary problem
        // I do not think it is allowed to patch two classes in the same method...
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityEngine.UI.Text))]
        [HarmonyPatch("text", PropertyMethod.Setter)]
        public static void TextPropertyHook1(ref string value, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    value = DynamicTranslator.Translate(value, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_Text))]
        [HarmonyPatch("text", PropertyMethod.Setter)]
        public static void TextPropertyHook2(ref string value, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    value = DynamicTranslator.Translate(value, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "SetText", new[] { typeof(string), typeof(bool) })]
        public static void SetTextHook1(ref string text, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    text = DynamicTranslator.Translate(text, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "SetText", new[] { typeof(StringBuilder) })]
        public static void SetTextHook2(ref StringBuilder text, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    text = new StringBuilder(DynamicTranslator.Translate(text.ToString(), __instance));
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "SetText", new[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
        public static void SetTextHook3(ref string text, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    text = DynamicTranslator.Translate(text, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        #endregion

        #region Hooks, I think are not needed for KK
        //// There's also 3 SetCharArray methods that should be hooked, but they are kinda annoying to implement with prefix
        //// Luckily, I do not think they are used in KK
        //[HarmonyPostfix, HarmonyPatch( typeof( TMP_Text ), "SetCharArray", new[] { typeof( char[] ) } )]
        //public static void SetCharArray1( TMP_Text __instance )
        //{
        //   if( TranslationHooksEnabled )
        //   {
        //      TranslationHooksEnabled = false;
        //      try
        //      {
        //         var newText = DynamicTranslator.Translate( __instance.text, __instance );
        //         if( newText != null )
        //         {
        //            __instance.text = newText;
        //         }
        //      }
        //      finally
        //      {
        //         TranslationHooksEnabled = true;
        //      }
        //   }
        //}

        //[HarmonyPostfix, HarmonyPatch( typeof( TMP_Text ), "SetCharArray", new[] { typeof( char[] ), typeof( int ), typeof( int ) } )]
        //public static void SetCharArray2( TMP_Text __instance )
        //{
        //   if( TranslationHooksEnabled )
        //   {
        //      TranslationHooksEnabled = false;
        //      try
        //      {
        //         var newText = DynamicTranslator.Translate( __instance.text, __instance );
        //         if( newText != null )
        //         {
        //            __instance.text = newText;
        //         }
        //      }
        //      finally
        //      {
        //         TranslationHooksEnabled = true;
        //      }
        //   }
        //}

        //[HarmonyPostfix, HarmonyPatch( typeof( TMP_Text ), "SetCharArray", new[] { typeof( int[] ), typeof( int ), typeof( int ) } )]
        //public static void SetCharArray3( TMP_Text __instance )
        //{
        //   if( TranslationHooksEnabled )
        //   {
        //      TranslationHooksEnabled = false;
        //      try
        //      {
        //         var newText = DynamicTranslator.Translate( __instance.text, __instance );
        //         if( newText != null )
        //         {
        //            __instance.text = newText;
        //         }
        //      }
        //      finally
        //      {
        //         TranslationHooksEnabled = true;
        //      }
        //   }
        //}
        #endregion

        #region ITL

        [HarmonyPrefix, HarmonyPatch(typeof(GraphicRegistry), "RegisterGraphicForCanvas")]
        public static bool RegisterGraphicForCanvasHook(ref Canvas c, ref Graphic graphic)
        {
            if (graphic)
            {
                var go = graphic.gameObject;
                if (go == null) return true;
                DynamicTranslator.TranslateComponents(go);
            }
            return true;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(Cursor), "SetCursor", new[] { typeof(Texture2D), typeof(Vector2), typeof(CursorMode) })]
        public static bool SetCursorHook(ref Texture2D texture)
        {
            var scene = "Cursors";

            DynamicTranslator.RegisterTexture(texture, string.Empty, scene);
            DynamicTranslator.ReplaceTexture(texture, string.Empty, scene);
            return true;
        }

        #endregion
    }
}