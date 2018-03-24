using Harmony;
using System.Reflection;
using TMPro;

namespace DynamicTranslationLoader
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.dynamictranslationloader");


            MethodInfo original = AccessTools.Property(typeof(TMP_Text), "text").GetSetMethod();

            HarmonyMethod prefix = new HarmonyMethod(typeof(Hooks).GetMethod("TextPropertyHook"));

            harmony.Patch(original, prefix, null);


            original = AccessTools.Method(typeof(TMP_Text), "SetText", new[] { typeof(string) });

            prefix = new HarmonyMethod(typeof(Hooks).GetMethod("SetTextHook"));

            harmony.Patch(original, prefix, null);


            original = AccessTools.Property(typeof(UnityEngine.UI.Text), "text").GetSetMethod();

            prefix = new HarmonyMethod(typeof(Hooks).GetMethod("TextPropertyHook"));

            harmony.Patch(original, prefix, null);
        }

        public static bool TranslationHooksEnabled { get; set; } = true;

        public static void TextPropertyHook(ref string value, object __instance)
        {
            if (TranslationHooksEnabled)
                value = DynamicTranslator.Translate(value, __instance);
        }

        public static void SetTextHook(ref string text, object __instance)
        {
            if (TranslationHooksEnabled)
                text = DynamicTranslator.Translate(text, __instance);
        }
    }
}
