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
            private static void CustomCoordinatePostHook() => LoadEventsEnabled = true;
            #endregion
        }
    }
}