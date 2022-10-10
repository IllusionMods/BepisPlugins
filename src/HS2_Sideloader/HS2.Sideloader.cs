using BepInEx;
using BepisPlugins;
using Sideloader.AutoResolver;
using System.Collections.Generic;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    public partial class Sideloader : BaseUnityPlugin
    {
        internal static readonly string[] GameNameList = { "hs2", "honeyselect2", "honey select 2" };
        
        private static string FindKoiZipmodDir() => string.Empty;
    }
}
