using BepInEx;
using BepisPlugins;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        internal static readonly WeakKeyDictionary<KoikatsuCharaFile.ChaFile, Dictionary<string, PluginData>> _internalCharaImportDictionary =
            new WeakKeyDictionary<KoikatsuCharaFile.ChaFile, Dictionary<string, PluginData>>();

        internal static readonly WeakKeyDictionary<KoikatsuCharaFile.ChaFileCoordinate, Dictionary<string, PluginData>> _internalCoordinateImportDictionary =
            new WeakKeyDictionary<KoikatsuCharaFile.ChaFileCoordinate, Dictionary<string, PluginData>>();

        internal static readonly WeakKeyDictionary<HEdit.HEditData, Dictionary<string, PluginData>> _internalHEditDataDictionary =
            new WeakKeyDictionary<HEdit.HEditData, Dictionary<string, PluginData>>();

        public static Dictionary<string, PluginData> GetAllExtendedData(HEdit.HEditData data) => _internalHEditDataDictionary.Get(data);

        public static PluginData GetExtendedDataById(HEdit.HEditData data, string id)
        {
            if (data == null || id == null)
                return null;

            var dict = _internalHEditDataDictionary.Get(data);

            if (dict != null && dict.TryGetValue(id, out var extendedSection))
                return extendedSection;

            return null;
        }

        public static void SetExtendedDataById(HEdit.HEditData data, string id, PluginData extendedFormatData)
        {
            var dict = _internalHEditDataDictionary.Get(data);

            if (dict == null)
            {
                dict = new Dictionary<string, PluginData>();
                _internalHEditDataDictionary.Set(data, dict);
            }

            dict[id] = extendedFormatData;
        }
    }
}
