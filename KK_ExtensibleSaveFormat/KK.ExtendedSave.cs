using BepInEx;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        internal static Dictionary<string, PluginData> internalSceneDictionary = new Dictionary<string, PluginData>();

        public static PluginData GetSceneExtendedDataById(string id)
        {
            if (id == null)
                return null;

            if (internalSceneDictionary.TryGetValue(id, out var extendedSection))
                return extendedSection;

            return null;
        }

        public static void SetSceneExtendedDataById(string id, PluginData extendedFormatData) => internalSceneDictionary[id] = extendedFormatData;
    }
}