﻿using BepInEx;

namespace Sideloader
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency("AI_Fix_CultureFix", BepInDependency.DependencyFlags.SoftDependency)] // todo remove when culture fix is updated to be a preloader plugin
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Sideloader : BaseUnityPlugin
    {
        private static readonly string[] GameNameList = { "aigirl", "ai girl" };

        private static string FindKoiZipmodDir() => string.Empty;
    }
}
