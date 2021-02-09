using BepInEx;
using BepInEx.Logging;
using BepisPlugins;
using Character;
using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    /// <summary>
    /// A set of tools for reading and writing extra data to card and scene files.
    /// </summary>
    [BepInProcess("PlayHome64bit")]
    [BepInProcess("PlayHomeStudio64bit")]
    [BepInProcess("VR GEDOU")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BaseUnityPlugin
    {
        /// <summary> Plugin GUID </summary>
        public const string GUID = "com.bepis.bepinex.extendedsave";
        /// <summary> Plugin name </summary>
        public const string PluginName = "Extended Save";
        /// <summary> Plugin version </summary>
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;
        /// <summary> Marker that indicates the extended save region on cards </summary>
        public static string Marker = "PHEx";
        /// <summary> Version of the extended save data on cards </summary>
        public static int DataVersion = 1;
        /// <summary>
        /// Whether extended data load events should be triggered. Temporarily disable it when extended data will never be used, for example loading lists of cards.
        /// </summary>
        public static bool LoadEventsEnabled = true;

        internal static WeakKeyDictionary<CustomParameter, Dictionary<string, PluginData>> internalCharaDictionary = new WeakKeyDictionary<CustomParameter, Dictionary<string, PluginData>>();

        internal static WeakKeyDictionary<CustomParameter, Dictionary<string, PluginData>> internalCoordinateDictionary = new WeakKeyDictionary<CustomParameter, Dictionary<string, PluginData>>();

        internal IEnumerator Start()
        {
            // HACK: Wait for PHIBL to load so we can patch it if it exists
            yield return null;
            Logger = base.Logger;
            Hooks.InstallHooks();
        }

        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a ChaFile
        /// </summary>
        /// <param name="file">ChaFile for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedData(CustomParameter file) => internalCharaDictionary.Get(file);

        /// <summary>
        /// Get PluginData for a ChaFile for the specified extended save data ID
        /// </summary>
        /// <param name="file">ChaFile for which to get extended data</param>
        /// <param name="id">ID of the data saved to the card</param>
        /// <returns>PluginData</returns>
        public static PluginData GetExtendedDataById(CustomParameter file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalCharaDictionary.Get(file);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }

        /// <summary>
        /// Set PluginData for a ChaFile for the specified extended save data ID
        /// </summary>
        /// <param name="file">ChaFile for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the card</param>
        /// <param name="extendedFormatData">PluginData to save to the card</param>
        public static void SetExtendedDataById(CustomParameter file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> chaDictionary = internalCharaDictionary.Get(file);

            if (chaDictionary == null)
            {
                chaDictionary = new Dictionary<string, PluginData>();
                internalCharaDictionary.Set(file, chaDictionary);
            }

            chaDictionary[id] = extendedFormatData;
        }

        /// <summary>
        /// Get a dictionary of ID, PluginData containing all extended data for a ChaFileCoordinate
        /// </summary>
        /// <param name="file">ChaFileCoordinate for which to get extended data</param>
        /// <returns>Dictionary of ID, PluginData</returns>
        public static Dictionary<string, PluginData> GetAllExtendedCoordData(CustomParameter file) => internalCoordinateDictionary.Get(file);

        /// <summary>
        /// Get PluginData for a ChaFileCoordinate for the specified extended save data ID
        /// </summary>
        /// <param name="file">ChaFileCoordinate for which to get extended data</param>
        /// <param name="id">ID of the data saved to the card</param>
        /// <returns>PluginData</returns>
        public static PluginData GetExtendedCoordDataById(CustomParameter file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalCoordinateDictionary.Get(file);

            return dict != null && dict.TryGetValue(id, out var extendedSection) ? extendedSection : null;
        }

        /// <summary>
        /// Set PluginData for a ChaFileCoordinate for the specified extended save data ID
        /// </summary>
        /// <param name="file">ChaFileCoordinate for which to set extended data</param>
        /// <param name="id">ID of the data to be saved to the card</param>
        /// <param name="extendedFormatData">PluginData to save to the card</param>
        public static void SetExtendedCoordDataById(CustomParameter file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> chaDictionary = internalCoordinateDictionary.Get(file);

            if (chaDictionary == null)
            {
                chaDictionary = new Dictionary<string, PluginData>();
                internalCoordinateDictionary.Set(file, chaDictionary);
            }

            chaDictionary[id] = extendedFormatData;
        }

        internal static byte[] MessagePackSerialize<T>(T obj)
        {
            try
            {
                return MessagePackSerializer.Serialize(obj, StandardResolver.Instance);
            }
            catch (FormatterNotRegisteredException)
            {
                return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Instance);
            }
            catch (InvalidOperationException)
            {
                Logger.LogWarning("Only primitive types are supported. Serialize your data first.");
                throw;
            }
        }

        internal static T MessagePackDeserialize<T>(byte[] obj)
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(obj, StandardResolver.Instance);
            }
            catch (FormatterNotRegisteredException)
            {
                return MessagePackSerializer.Deserialize<T>(obj, ContractlessStandardResolver.Instance);
            }
            catch (InvalidOperationException)
            {
                Logger.LogWarning("Only primitive types are supported. Serialize your data first.");
                throw;
            }
        }
    }
}
