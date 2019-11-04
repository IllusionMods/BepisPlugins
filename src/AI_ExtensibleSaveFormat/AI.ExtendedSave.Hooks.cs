using HarmonyLib;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region Extended Data Override Hooks

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CustomCharaFileInfoAssist), "AddList")]
            internal static void LoadCharacterListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CustomCharaFileInfoAssist), "AddList")]
            internal static void LoadCharacterListPostfix() => LoadEventsEnabled = true;

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaLoad), "UpdateCharasList")]
            internal static void CvsO_CharaLoadUpdateCharasListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaLoad), "UpdateCharasList")]
            internal static void CvsO_CharaLoadUpdateCharasListPostfix() => LoadEventsEnabled = true;

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaSave), "UpdateCharasList")]
            internal static void CvsO_CharaSaveUpdateCharasListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaSave), "UpdateCharasList")]
            internal static void CvsO_CharaSaveUpdateCharasListPostfix() => LoadEventsEnabled = true;

            #endregion
        }
    }
}