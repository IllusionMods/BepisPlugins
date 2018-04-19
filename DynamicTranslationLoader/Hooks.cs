using Harmony;
using TMPro;

namespace DynamicTranslationLoader
{
	public static class Hooks
	{
		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.dynamictranslationloader");
			harmony.PatchAll(typeof(Hooks));
		}

		public static bool TranslationHooksEnabled { get; set; } = true;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TMP_Text))]
		[HarmonyPatch(typeof(UnityEngine.UI.Text))]
		[HarmonyPatch("text", PropertyMethod.Setter)]
		public static void TextPropertyHook(ref string value, object __instance)
		{
			if (TranslationHooksEnabled)
				value = DynamicTranslator.Translate(value, __instance);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "SetText", new [] { typeof(string) })]
		public static void SetTextHook(ref string text, object __instance)
		{
			if (TranslationHooksEnabled)
				text = DynamicTranslator.Translate(text, __instance);
		}
	}
}