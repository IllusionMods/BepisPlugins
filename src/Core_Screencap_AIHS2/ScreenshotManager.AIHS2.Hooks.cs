using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Screencap
{
    public partial class ScreenshotManager
    {
        /// <summary>
        /// Disable built-in screenshots
        /// </summary>
        private static class Hooks
        {
            public static void InstallHooks()
            {
                var h = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

                var msvoType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.MultiScaleVO, Unity.Postprocessing.Runtime");
                h.Patch(AccessTools.Method(msvoType, "PushAllocCommands"), transpiler: new HarmonyMethod(typeof(Hooks), nameof(AoBandingFix)));
            }

#if AI
            // Hook here instead of hooking GameScreenShot.Capture to not affect the Photo functionality
            [HarmonyPrefix, HarmonyPatch(typeof(AIProject.Scene.MapScene), nameof(AIProject.Scene.MapScene.CaptureSS))]
            private static bool CaptureSSOverride() => false;
#elif HS2
            public static bool SoundWasPlayed;

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.Capture), typeof(string))]
            private static bool CaptureOverride()
            {
                SoundWasPlayed = true;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture), typeof(string))]
            private static bool CaptureOverride2()
            {
                SoundWasPlayed = true;
                return false;
            }
#endif

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
}
