using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TARC.Compiler;
using TMPro;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace DynamicTranslationLoader.Text
{
    public static class TextHooks
    {
        public static void InstallHooks()
        {
            try
            {
                var harmony = HarmonyInstance.Create("com.bepis.bepinex.dynamictranslationloader");
                harmony.PatchAll(typeof(TextHooks));
            }
            catch (System.Exception e)
            {
                Logger.Log(LogLevel.Error, e);
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
                    var newText = TextTranslator.TranslateText(__instance.text, __instance);
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
                    var newText = TextTranslator.TranslateText(__instance.text, __instance);
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

        [HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.UI.Text), "OnEnable")]
        public static void OnEnableHook(UnityEngine.UI.Text __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    var newText = TextTranslator.TranslateText(__instance.text, __instance);
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

        #region Text Change Hooks

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
                    value = TextTranslator.TranslateText(value, __instance);
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
                    value = TextTranslator.TranslateText(value, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }
        #endregion

        #region GUI Text Hooks
        [HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "SetText", new[] { typeof(string), typeof(bool) })]
        public static void SetTextHook1(ref string text, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    text = TextTranslator.TranslateText(text, __instance);
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
                    text = new StringBuilder(TextTranslator.TranslateText(text.ToString(), __instance));
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
                    text = TextTranslator.TranslateText(text, __instance);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoLabel")]
        public static void DoLabel(GUIContent content, object __instance)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    content.text = TextTranslator.TranslateTextAlternate(content.text);
                    content.tooltip = TextTranslator.TranslateTextAlternate(content.tooltip);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoButton")]
        public static void DoButton(GUIContent content)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    content.text = TextTranslator.TranslateTextAlternate(content.text);
                    content.tooltip = TextTranslator.TranslateTextAlternate(content.tooltip);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoToggle")]
        public static void DoToggle(GUIContent content)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    content.text = TextTranslator.TranslateTextAlternate(content.text);
                    content.tooltip = TextTranslator.TranslateTextAlternate(content.tooltip);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoWindow")]
        public static void DoWindow(GUIContent title)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    title.text = TextTranslator.TranslateTextAlternate(title.text);
                    title.tooltip = TextTranslator.TranslateTextAlternate(title.tooltip);
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoButtonGrid")]
        public static void DoButtonGrid(GUIContent[] contents)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    foreach (GUIContent content in contents)
                    {
                        content.text = TextTranslator.TranslateTextAlternate(content.text);
                        content.tooltip = TextTranslator.TranslateTextAlternate(content.tooltip);
                    }
                }
                finally
                {
                    TranslationHooksEnabled = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI), "DoTextField", new[] { typeof(Rect), typeof(int), typeof(GUIContent), typeof(bool), typeof(int), typeof(GUIStyle), typeof(string), typeof(char) })]
        public static void DoTextField(GUIContent content)
        {
            if (TranslationHooksEnabled)
            {
                TranslationHooksEnabled = false;
                try
                {
                    content.text = TextTranslator.TranslateTextAlternate(content.text);
                    content.tooltip = TextTranslator.TranslateTextAlternate(content.tooltip);
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

        #region Text break hooks

        //Replicate vanilla linebreak handling in the event that an untranslated ADV line is found.
        private static readonly char[] HYP_LATIN = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>=/().,".ToCharArray();
        private static readonly char[] HYP_BACK = "(（[｛〔〈《「『【〘〖〝‘“｟«".ToCharArray();
        private static readonly char[] HYP_FRONT = ",)]｝、。）〕〉》」』】〙〗〟’”｠»ァィゥェォッャュョヮヵヶっぁぃぅぇぉっゃゅょゎ‐゠–〜ー?!！？‼⁇⁈⁉・:;。.".ToCharArray();
        private static HashSet<char> JPChars = new HashSet<char>("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわんアイウエカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン");

        private static bool IsLatin(char s) => HYP_LATIN.Contains(s);
        private static bool CHECK_HYP_BACK(char s) => HYP_BACK.Contains(s);
        private static bool CHECK_HYP_FRONT(char s) => HYP_BACK.Contains(s);
        public static bool IsJapanese (string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (JPChars.Contains(text[i]))
                    return true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HyphenationJpn), "GetWordList")]
        public static bool GetWordList(string tmpText, ref List<string> __result)
        {
            if (IsJapanese(tmpText))
            {
                List<string> list = new List<string>();
                StringBuilder stringBuilder = new StringBuilder();
                char c = '\0';
                for (int i = 0; i < tmpText.Length; i++)
                {
                    char c2 = tmpText[i];
                    char s = (i >= tmpText.Length - 1) ? c : tmpText[i + 1];
                    char s2 = (i <= 0) ? c : tmpText[i - 1];
                    stringBuilder.Append(c2);
                    if ((IsLatin(c2) && IsLatin(s2) && IsLatin(c2) && !IsLatin(s2)) || (!IsLatin(c2) && CHECK_HYP_BACK(s2)) || (!IsLatin(s) && !CHECK_HYP_FRONT(s) && !CHECK_HYP_BACK(c2)) || i == tmpText.Length - 1)
                    {
                        list.Add(stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                    }
                }
                __result = list;
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HyphenationJpn), "IsLatin")]
        public static bool UpdateText(ref bool __result, ref char s)
        {
            // Break only on space?
            __result = s != ' ';
            return false;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(HyphenationJpn), "GetFormatedText")]
        public static void GetFormatedText(ref string __result)
        {
            // When the width of the text is greater than its container, a space is inserted.
            // This can throw off our formatting, so we remove all occurrences of it.

            __result = __result.Replace("\u3000", "");
            __result = string.Join("\n", __result.Split('\n').Select(x => x.Trim()).ToArray());
        }
        #endregion

        [HarmonyTranspiler, HarmonyPatch(typeof(GlobalMethod), nameof(GlobalMethod.LoadAllListText))]
        public static IEnumerable<CodeInstruction> LoadAllListTextTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

            int InstructionIndex = newInstructionSet.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && instruction.operand == typeof(TextAsset).GetMethod("get_text"));

            //Find the Ldloc_0 OpCode
            int StartIndex = 0;
            for (int i = 0; i < InstructionIndex; i++)
            {
                if (newInstructionSet[InstructionIndex - i].opcode == OpCodes.Ldloc_0)
                {
                    StartIndex = InstructionIndex - i;
                    break;
                }
            }
            // +1 because we don't want to remove it
            StartIndex++;

            //Find the Pop OpCode
            int EndIndex = 0;
            for (int i = 0; i < InstructionIndex; i++)
            {
                if (newInstructionSet[InstructionIndex + i].opcode == OpCodes.Pop)
                {
                    EndIndex = InstructionIndex + i;
                    break;
                }
            }

            //Remove everything between the Ldloc_0 and Pop opcodes
            newInstructionSet.RemoveRange(StartIndex, EndIndex - StartIndex);

            //Add new instructions which contain a call to the TranslateListText function
            var textAssetOperand = newInstructionSet.Where(x => x.operand != null && x.operand.ToString().StartsWith("UnityEngine.TextAsset (")).Select(x => x.operand).FirstOrDefault();
            List<CodeInstruction> newInstructionSetAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_S, textAssetOperand),
                new CodeInstruction(OpCodes.Callvirt, typeof(TextAsset).GetMethod("get_text")),
                new CodeInstruction(OpCodes.Call, typeof(TextTranslator).GetMethod(nameof(TextTranslator.TranslateListText))),
                new CodeInstruction(OpCodes.Callvirt, typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), AccessTools.all, null, new Type[] { typeof(string) }, null))
            };

            newInstructionSet.InsertRange(StartIndex, newInstructionSetAdd);

            return newInstructionSet;
        }
    }
}