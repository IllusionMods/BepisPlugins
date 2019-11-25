using BepInEx;

namespace Sideloader
{
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "koikatsu", "koikatu", "コイカツ" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
