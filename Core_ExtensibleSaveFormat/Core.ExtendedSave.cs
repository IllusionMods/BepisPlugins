using BepInEx.Logging;
using BepisPlugins;
using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
#if AI
using AIChara;
#endif

namespace ExtensibleSaveFormat
{
    /// <summary>
    /// A set of tools for reading and writing extra data to card and scene files.
    /// </summary>
    public partial class ExtendedSave
    {
        public const string GUID = "com.bepis.bepinex.extendedsave";
        public const string PluginName = "Extended Save";
        public const string Version = Metadata.PluginsVersion;
        internal static new ManualLogSource Logger;
        /// <summary>
        /// Whether extended data load events should be triggered. Temporarily disable it when extended data will never be used, for example loading lists of cards.
        /// </summary>
        public static bool LoadEventsEnabled = true;

        internal static WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>> internalCharaDictionary = new WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>>();

        internal static WeakKeyDictionary<ChaFileCoordinate, Dictionary<string, PluginData>> internalCoordinateDictionary = new WeakKeyDictionary<ChaFileCoordinate, Dictionary<string, PluginData>>();

        private void Awake()
        {
            Logger = base.Logger;
            Hooks.InstallHooks();
        }

        public static Dictionary<string, PluginData> GetAllExtendedData(ChaFile file) => internalCharaDictionary.Get(file);

        public static PluginData GetExtendedDataById(ChaFile file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalCharaDictionary.Get(file);

            if (dict != null && dict.TryGetValue(id, out var extendedSection))
                return extendedSection;

            return null;
        }

        public static void SetExtendedDataById(ChaFile file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> chaDictionary = internalCharaDictionary.Get(file);

            if (chaDictionary == null)
            {
                chaDictionary = new Dictionary<string, PluginData>();
                internalCharaDictionary.Set(file, chaDictionary);
            }

            chaDictionary[id] = extendedFormatData;
        }

        public static Dictionary<string, PluginData> GetAllExtendedData(ChaFileCoordinate file) => internalCoordinateDictionary.Get(file);


        public static PluginData GetExtendedDataById(ChaFileCoordinate file, string id)
        {
            if (file == null || id == null)
                return null;

            var dict = internalCoordinateDictionary.Get(file);

            if (dict != null && dict.TryGetValue(id, out var extendedSection))
                return extendedSection;

            return null;
        }

        public static void SetExtendedDataById(ChaFileCoordinate file, string id, PluginData extendedFormatData)
        {
            Dictionary<string, PluginData> chaDictionary = internalCoordinateDictionary.Get(file);

            if (chaDictionary == null)
            {
                chaDictionary = new Dictionary<string, PluginData>();
                internalCoordinateDictionary.Set(file, chaDictionary);
            }

            chaDictionary[id] = extendedFormatData;
        }

        public static byte[] MessagePackSerialize<T>(T obj)
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

        public static T MessagePackDeserialize<T>(byte[] obj)
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
