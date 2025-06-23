using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Screencap
{
    /// <summary>
    /// Disable built-in screenshots
    /// </summary>
    internal static partial class Hooks
    {
        public static void InstallHooks()
        {
            var h = Harmony.CreateAndPatchAll(typeof(Hooks), ScreenshotManager.GUID);

            var msvoType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.MultiScaleVO, Unity.Postprocessing.Runtime");
            if (msvoType == null) throw new ArgumentNullException(nameof(msvoType));
            h.Patch(AccessTools.Method(msvoType, "PushAllocCommands"), transpiler: new HarmonyMethod(typeof(Hooks), nameof(AoBandingFix)));
        }

        // Separate screenshot class for the studio
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), nameof(Studio.GameScreenShot.Capture), typeof(string))]
        private static bool StudioCaptureOverride()
        {
            return false;
        }

        // Fix AO banding in downscaled screenshots
        private static IEnumerable<CodeInstruction> AoBandingFix(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var i in instructions)
            {
                if (i.opcode == OpCodes.Ldc_I4_S)
                {
                    if ((int)RenderTextureFormat.RHalf == Convert.ToInt32(i.operand))
                        i.operand = (sbyte)RenderTextureFormat.RFloat;
                    else if ((int)RenderTextureFormat.RGHalf == Convert.ToInt32(i.operand))
                        i.operand = (sbyte)RenderTextureFormat.RGFloat;
                }
                yield return i;
            }
        }
    }
}
