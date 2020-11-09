using AIProject.UI;
using CharaCustom;
using HarmonyLib;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            //Override ExtSave for list loading at game startup
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(GameCoordinateFileInfoAssist), nameof(GameCoordinateFileInfoAssist.CreateCoordinateFileInfoList))]
            private static void CreateListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(GameCoordinateFileInfoAssist), nameof(GameCoordinateFileInfoAssist.CreateCoordinateFileInfoList))]
            private static void CreateListPostfix() => LoadEventsEnabled = true;
        }
    }
}