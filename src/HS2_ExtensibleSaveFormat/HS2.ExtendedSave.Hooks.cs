using System.Collections;
using CharaCustom;
using CoordinateFileSystem;
using HarmonyLib;
using HS2;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            //Override ExtSave for list loading at game startup
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Config.ConfigCharaSelectUI), "CreateList")]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(CoordinateFileInfoAssist), nameof(CoordinateFileInfoAssist.CreateCharaFileInfoList))]
            internal static void CreateListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Config.ConfigCharaSelectUI), "CreateList")]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(CoordinateFileInfoAssist), nameof(CoordinateFileInfoAssist.CreateCharaFileInfoList))]
            internal static void CreateListPostfix() => LoadEventsEnabled = true;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(TitleScene), "LoadChara")]
            internal static void FixTitleLoadCharaRace(ref IEnumerator __result)
            {
                // Title character gets loaded in Start and causes a race condition with extended data stuff. Delay the load 1 frame to avoid this
                var orig = __result;
                __result = new[] { null, orig }.GetEnumerator();
            }
        }
    }
}