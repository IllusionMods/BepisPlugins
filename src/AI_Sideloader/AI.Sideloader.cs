using BepInEx;
using BepisPlugins;
using Sideloader.AutoResolver;
using System.Collections.Generic;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class Sideloader : BaseUnityPlugin
    {
        internal static readonly string[] GameNameList = { "aigirl", "ai girl" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
