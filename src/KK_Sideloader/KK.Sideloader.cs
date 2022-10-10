using BepInEx;
using BepisPlugins;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInIncompatibility("com.bepis.bepinex.resourceredirector")]
    public partial class Sideloader : BaseUnityPlugin
    {
        internal static readonly string[] GameNameList = { "koikatsu", "koikatu", "コイカツ" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
