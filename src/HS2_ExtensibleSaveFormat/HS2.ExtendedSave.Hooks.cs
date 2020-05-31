using HarmonyLib;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            //Override ExtSave for list loading at game startup
            [HarmonyPrefix, HarmonyPatch(typeof(Config.ConfigCharaSelectUI), "CreateList")]
            internal static void CreateListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(Config.ConfigCharaSelectUI), "CreateList")]
            internal static void CreateListPostfix() => LoadEventsEnabled = true;
        }
    }
}