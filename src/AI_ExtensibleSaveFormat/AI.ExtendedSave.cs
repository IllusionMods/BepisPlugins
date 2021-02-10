using System.Collections.Generic;
using AIChara;
using AIProject.SaveData;
using BepInEx;
using BepisPlugins;
using Housing;

namespace ExtensibleSaveFormat
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        /// <summary>
        /// Internal dictionary for saving each save slot's data.
        /// Serialize operation will take place when the main game save is being saved.
        /// TODO: inspect the policy.
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once InconsistentNaming
        internal static WeakKeyDictionary<WorldData, Dictionary<string, PluginData>> internalWorldDataDictionary =
            new WeakKeyDictionary<WorldData, Dictionary<string, PluginData>>();

        /// <summary>
        /// internal dictionary for craftinfo dictionary.
        /// </summary>
        internal static WeakKeyDictionary<CraftInfo, Dictionary<string, PluginData>> internalCraftInfoDictionary =
            new WeakKeyDictionary<CraftInfo, Dictionary<string, PluginData>>();


        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a WorldData
        /// </summary>
        /// <param name="file">WorldData for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedData(WorldData file) => internalWorldDataDictionary.Get(file);


        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a CraftInfo
        /// </summary>
        /// <param name="file">CraftInfo for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedData(CraftInfo file) => internalCraftInfoDictionary.Get(file);


        /// <summary>
        /// Set PluginData for a WorldData for the specified extended save data ID
        /// </summary>
        /// <param name="file">WorldData for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the card</param>
        /// <param name="extendedFormatData">PluginData to save to the card</param>
        public static void SetExtendedDataById(WorldData file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> worldDictionary = internalWorldDataDictionary.Get(file);

            if (worldDictionary == null)
            {
                worldDictionary = new Dictionary<string, PluginData>();
                internalWorldDataDictionary.Set(file, worldDictionary);
            }

            worldDictionary[id] = extendedFormatData;
        }

        /// <summary>
        /// Set PluginData for a CraftInfo for the specified extended save data ID
        /// </summary>
        /// <param name="file">CraftInfo for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the card</param>
        /// <param name="extendedFormatData">PluginData to save to the card</param>
        public static void SetExtendedDataById(CraftInfo file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> craftInfoDictionary = internalCraftInfoDictionary.Get(file);

            if (craftInfoDictionary == null)
            {
                craftInfoDictionary = new Dictionary<string, PluginData>();
                internalCraftInfoDictionary.Set(file, craftInfoDictionary);
            }

            craftInfoDictionary[id] = extendedFormatData;
        }

        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a WorldData
        /// </summary>
        /// <param name="file">WorldData for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static PluginData GetExtendedDataById(WorldData file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalWorldDataDictionary.Get(file);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }

        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a CraftInfo
        /// </summary>
        /// <param name="file">CraftInfo for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static PluginData GetExtendedDataById(CraftInfo file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalCraftInfoDictionary.Get(file);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }
    }
}
