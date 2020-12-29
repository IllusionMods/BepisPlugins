using System;
using System.Collections.Generic;
using BepInEx;
using BepisPlugins;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave : BaseUnityPlugin
    {
        internal static WeakKeyDictionary<SaveData, Dictionary<string, PluginData>> internalSaveDataDictionary = new WeakKeyDictionary<SaveData, Dictionary<string, PluginData>>();
        
        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a SaveData
        /// </summary>
        /// <param name="saveData">SaveData for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedData(SaveData saveData) => internalSaveDataDictionary.Get(saveData);

        /// <summary>
        /// Get PluginData for a SaveData for the specified extended save data ID
        /// </summary>
        /// <param name="saveData">SaveData for which to get extended save file</param>
        /// <param name="id">ID of the data saved to the save file</param>
        /// <returns>PluginData</returns>
        public static PluginData GetExtendedDataById(SaveData saveData, string id)
        {
            if (saveData == null || id == null)
                return null;

            var dict = internalSaveDataDictionary.Get(saveData);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }

        /// <summary>
        /// Set PluginData for a SaveData for the specified extended save data ID
        /// </summary>
        /// <param name="saveData">SaveData for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the save file</param>
        /// <param name="extendedFormatData">PluginData to save to the save file</param>
        public static void SetExtendedDataById(SaveData saveData, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> chaDictionary = internalSaveDataDictionary.Get(saveData);

            if (chaDictionary == null)
            {
                chaDictionary = new Dictionary<string, PluginData>();
                internalSaveDataDictionary.Set(saveData, chaDictionary);
            }

            chaDictionary[id] = extendedFormatData;
        }

        
        /// <summary> SaveData event handler </summary>
        public delegate void SaveDataEventHandler(SaveData saveData);
        /// <summary> Register methods to trigger on save file being saved </summary>
        public static event SaveDataEventHandler SaveDataBeingSaved;
        /// <summary> Register methods to trigger on save file being loaded </summary>
        public static event SaveDataEventHandler SaveDataBeingLoaded;
        

        internal static void SaveDataWriteEvent(SaveData saveData)
        {
            if (SaveDataBeingSaved == null)
                return;

            foreach (var entry in SaveDataBeingSaved.GetInvocationList())
            {
                var handler = (SaveDataEventHandler)entry;
                try
                {
                    handler.Invoke(saveData);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SaveDataBeingSaved)} - {ex}");
                }
            }
        }

        internal static void SaveDataReadEvent(SaveData saveData)
        {
            if (SaveDataBeingLoaded == null)
                return;

            foreach (var entry in SaveDataBeingLoaded.GetInvocationList())
            {
                var handler = (SaveDataEventHandler)entry;
                try
                {
                    handler.Invoke(saveData);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SaveDataBeingLoaded)} - {ex}");
                }
            }
        }
    }
}