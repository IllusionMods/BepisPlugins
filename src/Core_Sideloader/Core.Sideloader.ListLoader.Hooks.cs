using BepInEx.Harmony;
using HarmonyLib;
#if AI
using AIChara;
#endif

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        internal static partial class Hooks
        {
            internal static void InstallHooks() => HarmonyWrapper.PatchAll(typeof(Hooks));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.CheckItemID), typeof(int), typeof(int))]
            internal static bool CheckItemIDHook(int category, int id, ref byte __result, ChaListControl __instance)
            {
                int pid = Lists.CalculateGlobalID(category, id);

                byte result = __instance.CheckItemID(pid);

                if (result > 0)
                {
                    __result = result;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.AddItemID), typeof(int), typeof(int), typeof(byte))]
            internal static bool AddItemIDHook(int category, int id, byte flags, ChaListControl __instance)
            {
                int pid = Lists.CalculateGlobalID(category, id);

                byte result = __instance.CheckItemID(pid);

                if (result > 0)
                {
                    __instance.AddItemID(pid, flags);
                    return false;
                }

                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadListInfoAll))]
            internal static void LoadListInfoAllPostHook(ChaListControl __instance) => Lists.LoadAllLists(__instance);
        }
    }
}