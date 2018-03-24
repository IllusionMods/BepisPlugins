using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Screencap
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.screenshotmanager");


            MethodInfo original = AccessTools.Method(typeof(GameScreenShot), "Capture");

            HarmonyMethod prefix = new HarmonyMethod(typeof(Hooks).GetMethod(nameof(CapturePreHook)));

            harmony.Patch(original, prefix, null);
        }

        public static bool CapturePreHook()
        {
            //cancel the vanilla screenshot
            return false;
        }
    }
}
