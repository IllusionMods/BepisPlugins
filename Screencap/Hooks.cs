using Harmony;

namespace Screencap
{
	public static class Hooks
	{
		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.screenshotmanager");
			harmony.PatchAll(typeof(Hooks));
		}

		[HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), "Capture")]
		public static bool CapturePreHook()
		{
			//cancel the vanilla screenshot
			return false;
		}
	}
}
