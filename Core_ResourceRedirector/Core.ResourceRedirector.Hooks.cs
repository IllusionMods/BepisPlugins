using BepInEx.Harmony;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ResourceRedirector
{
    internal static partial class Hooks
    {
        public static void InstallHooks() => HarmonyWrapper.PatchAll(typeof(Hooks));

        #region List Loading
        [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.CheckItemID), new[] { typeof(int), typeof(int) })]
        public static bool CheckItemIDHook(int category, int id, ref byte __result, ChaListControl __instance)
        {
            int pid = ListLoader.CalculateGlobalID(category, id);

            byte result = __instance.CheckItemID(pid);

            if (result > 0)
            {
                __result = result;
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.AddItemID), new[] { typeof(int), typeof(int), typeof(byte) })]
        public static bool AddItemIDHook(int category, int id, byte flags, ChaListControl __instance)
        {
            int pid = ListLoader.CalculateGlobalID(category, id);

            byte result = __instance.CheckItemID(pid);

            if (result > 0)
            {
                __instance.AddItemID(pid, flags);
                return false;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), "LoadListInfoAll")]
        public static void LoadListInfoAllPostHook(ChaListControl __instance)
        {
            if (ResourceRedirector.EmulationEnabled)
            {
                string listPath = Path.Combine(ResourceRedirector.EmulatedDir, @"list\characustom");

                if (Directory.Exists(listPath))
                    foreach (string csvPath in Directory.GetFiles(listPath, "*.csv", SearchOption.AllDirectories))
                    {
                        var chaListData = ListLoader.LoadCSV(File.OpenRead(csvPath));
                        ListLoader.ExternalDataList.Add(chaListData);
                    }
            }

            ListLoader.LoadAllLists(__instance);
        }
        #endregion

        [HarmonyTranspiler, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetBundleInternal))]
        public static IEnumerable<CodeInstruction> LoadAssetBundleInternalTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            MethodInfo LoadMethod = typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadFromFile), AccessTools.all, null, new[] { typeof(string) }, null);

            int IndexLoadFromFile = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == LoadMethod);

            //Switch out a LoadFromFile call
            if (IndexLoadFromFile > 0)
                instructionsList[IndexLoadFromFile].operand = typeof(ResourceRedirector).GetMethod(nameof(ResourceRedirector.HandleAssetBundle), AccessTools.all);

            return instructions;
        }
    }
}