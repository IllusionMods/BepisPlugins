﻿using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Studio;

namespace Sideloader
{
    public partial class Sideloader
    {
        /// <summary>
        /// The EX 0408 and some later updates introduced filename checks of list files.
        /// If the name isn't a number, it is now ignored. It was done to sort list files by their number.
        /// These patches sort the files in a compatible way while removing the filename limitation.
        /// GameInfo.LoadInfo doesn't need fixing since the filename check was always there.
        /// Program.FindADVBundleFilePath doesn't need fixing since no mods put bundles inside adv/scenario
        /// </summary>
        internal static class KKSEX0408fix
        {
            public static void ApplyIfNeeded(Harmony harmony)
            {
                // This was added in the offical 0408 patch and needs to be patched or it will filter out most of the modded studio items
                var listProp = AccessTools.PropertyGetter(typeof(Studio.Info.FileListInfo), "PathList");
                if (listProp != null)
                {
                    try
                    {
                        // Fix all studio list files with non-number filenames being stripped by the new sorting algorithm
                        harmony.Patch(
                            original: listProp,
                            prefix: new HarmonyMethod(typeof(KKSEX0408fix), nameof(StudioInfoPathListOverride)));
                        // Fix crash if multiple zipmods have list files with number names that are the same
                        harmony.Patch(
                            original: AccessTools.Constructor(typeof(Info.FileListInfo), new[] { typeof(List<string>) }),
                            transpiler: new HarmonyMethod(typeof(KKSEX0408fix), nameof(StudioInfoRemoveDictTpl)));
                        // Fix all character list files with non-number filenames being randomly ordered in front of everything else by the new sorting algorithm
                        harmony.Patch(
                            original: AccessTools.Method(typeof(ChaListControl), nameof(ChaListControl.LoadListInfoAll)),
                            transpiler: new HarmonyMethod(typeof(KKSEX0408fix), nameof(LoadListInfoAllSortReplaceTpl)));
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning("Failed to apply a fix for the KKS EX 0408 patch, many modded items might not work. Cause: " + e);
                    }
                }
            }

            private static IEnumerable<CodeInstruction> StudioInfoRemoveDictTpl(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(SortedDictionary<Int32, string>), nameof(SortedDictionary<Int32, string>.Add))))
                    .ThrowIfInvalid("No SortedDictionary<Int32, string>.Add found")
                    .SetAndAdvance(OpCodes.Pop, null)
                    .Insert(new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Pop))
                    .Instructions();
            }

            private static IEnumerable<CodeInstruction> LoadListInfoAllSortReplaceTpl(IEnumerable<CodeInstruction> instructions)
            {
                var cm = new CodeMatcher(instructions);
                cm.MatchForward(true, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CommonLib), nameof(CommonLib.GetAssetBundleNameListFromPath))))
                    .ThrowIfNotMatchForward("No OrderBy found", new CodeMatch(ci => ci.opcode == OpCodes.Call && ((MethodInfo)ci.operand).Name == "OrderBy"))
                    .Advance(1);

                // Get rid of the original sorting code
                while (cm.Opcode != OpCodes.Stloc_3)
                    cm.SetAndAdvance(OpCodes.Nop, null);

                cm.ThrowIfInvalid("Stloc_3 not found, the IL has changed");
                cm.Advance(-1);
                cm.Set(OpCodes.Call, AccessTools.Method(typeof(KKSEX0408fix), nameof(KKSEX0408fix.CustomListSort)));

                return cm.Instructions();
            }

            private static List<string> CustomListSort(IEnumerable<string> originalList)
            {
                var sortedList = originalList.OrderBy(Path.GetFileNameWithoutExtension, NativeMethods.StrCmpLogicalW).ToList();
                //Console.WriteLine("CustomListSort NEW ORDER\n" + string.Join("\n", sortedList));
                return sortedList;
            }

            private static bool StudioInfoPathListOverride(Info.FileListInfo __instance, out List<string> __result)
            {
                __result = CustomListSort(__instance.dicFile.Keys);
                return false;
            }

            private static class NativeMethods
            {
                /// <summary>
                /// String comparer that is equivalent to the one used by Windows Explorer to sort files (e.g. 2 will go before 10, unlike normal compare).
                /// </summary>
                [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
                public static extern int StrCmpLogicalW(string x, string y);
            }
        }
    }
}