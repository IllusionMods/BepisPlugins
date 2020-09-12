using BepInEx.Harmony;
using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
#if AI || HS2
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
                int pid = CalculateGlobalID(category, id);

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
                int pid = CalculateGlobalID(category, id);

                byte result = __instance.CheckItemID(pid);

                if (result > 0)
                {
                    __instance.AddItemID(pid, flags);
                    return false;
                }

                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadListInfoAll))]
            internal static void LoadListInfoAllPostHook(ChaListControl __instance) => LoadAllLists(__instance);

            [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.AddItemID), typeof(int), typeof(int), typeof(byte))]
            internal static bool AddItemIDHook(int category, int id)
            {
                if (id >= UniversalAutoResolver.BaseSlotID)
                {
                    ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)category, id);
                    if (!CheckItemList.ContainsKey(Info.GUID))
                        CheckItemList[Info.GUID] = new Dictionary<int, HashSet<int>>();
                    if (!CheckItemList[Info.GUID].ContainsKey(category))
                        CheckItemList[Info.GUID][category] = new HashSet<int>();
                    if (!CheckItemList[Info.GUID][category].Contains(Info.Slot))
                    {
                        CheckItemList[Info.GUID][category].Add(Info.Slot);
                        SaveCheckItemList();
                    }

                    return false;
                }
                return true;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.CheckItemID), typeof(int), typeof(int))]
            internal static bool CheckItemIDHook(int category, int id, ref byte __result)
            {
                if (id >= UniversalAutoResolver.BaseSlotID)
                {
                    {
                        ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)category, id);

                        if (CheckItemList.TryGetValue(Info.GUID, out var x))
                            if (x.TryGetValue(category, out var y))
                                if (y.Contains(Info.Slot))
                                {
                                    __result = 2; //Not new
                                    return false;
                                }

                        __result = 1; //New
                        return false;
                    }
                }
                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadItemID))]
            internal static void LoadItemIDHook() => LoadCheckItemList();
            [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.SaveItemID))]
            internal static void SaveItemIDHook() => SaveCheckItemList();
        }
    }
}