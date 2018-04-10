using Harmony;

namespace Sideloader
{
    public static class Hooks
	{
		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.sideloader");
			harmony.PatchAll(typeof(Hooks));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AssetBundleCheck), nameof(AssetBundleCheck.IsFile))]
		public static void IsFileHook(string assetBundleName, ref bool __result)
		{
		    if (BundleManager.Bundles.ContainsKey(assetBundleName))
		    {
		        __result = true;
		    }
		}
	}
}
