using ChaCustom;
using HarmonyLib;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region Extended Data Override Hooks
            [HarmonyPostfix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
            public static void CustomCoordinatePostHook() => ExtendedSave.LoadEventsEnabled = true;
            #endregion
        }
    }
}