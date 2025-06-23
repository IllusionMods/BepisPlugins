﻿using HarmonyLib;

namespace Screencap
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.CreateCaptureFileName))]
        private static bool CreateCaptureFileNamePreHook() => false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture))]
        private static bool UnityCapturePreHook() => false;
    }
}
