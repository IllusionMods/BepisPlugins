using BepInEx;
using BepisPlugins;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        internal static readonly WeakKeyDictionary<KoikatsuCharaFile.ChaFile, Dictionary<string, PluginData>> _internalCharaImportDictionary =
            new WeakKeyDictionary<KoikatsuCharaFile.ChaFile, Dictionary<string, PluginData>>();

        internal static readonly WeakKeyDictionary<KoikatsuCharaFile.ChaFileCoordinate, Dictionary<string, PluginData>> _internalCoordinateImportDictionary =
            new WeakKeyDictionary<KoikatsuCharaFile.ChaFileCoordinate, Dictionary<string, PluginData>>();

        internal static readonly WeakKeyDictionary<HEdit.HEditData, Dictionary<string, PluginData>> _internalHEditDataDictionary =
            new WeakKeyDictionary<HEdit.HEditData, Dictionary<string, PluginData>>();

        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a HEditData
        /// </summary>
        /// <param name="data">HEditData for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedData(HEdit.HEditData data) => _internalHEditDataDictionary.Get(data);

        /// <summary>
        /// Get PluginData for a HEditData for the specified extended save data ID
        /// </summary>
        /// <param name="data">HEditData for which to get extended data</param>
        /// <param name="id">ID of the data saved to the card</param>
        /// <returns>PluginData</returns>
        public static PluginData GetExtendedDataById(HEdit.HEditData data, string id)
        {
            if (data == null || id == null)
                return null;

            var dict = _internalHEditDataDictionary.Get(data);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }

        /// <summary>
        /// Set PluginData for a HEditData for the specified extended save data ID
        /// </summary>
        /// <param name="data">HEditData for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the card</param>
        /// <param name="extendedFormatData">PluginData to save to the card</param>
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
