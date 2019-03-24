using BepInEx;
using BepInEx.Logging;
using BepisPlugins;
using System;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID: GUID, Name: "Extended Save", Version: Version)]
    public class ExtendedSave : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.extendedsave";
        public const string Version = Metadata.PluginsVersion;
        /// <summary>
        /// Whether extended data load events should be triggered. Temporarily disable it when extended data will never be used, for example loading lists of cards.
        /// </summary>
        public static bool LoadEventsEnabled = true;

        internal static WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>> internalCharaDictionary = new WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>>();

        internal static WeakKeyDictionary<ChaFileCoordinate, Dictionary<string, PluginData>> internalCoordinateDictionary = new WeakKeyDictionary<ChaFileCoordinate, Dictionary<string, PluginData>>();

        internal static Dictionary<string, PluginData> internalSceneDictionary = new Dictionary<string, PluginData>();

        #region Events

        public delegate void CardEventHandler(ChaFile file);

        public static event CardEventHandler CardBeingSaved;

        public static event CardEventHandler CardBeingLoaded;

        public delegate void CoordinateEventHandler(ChaFileCoordinate file);

        public static event CoordinateEventHandler CoordinateBeingSaved;

        public static event CoordinateEventHandler CoordinateBeingLoaded;

        public delegate void SceneEventHandler(string path);

        public static event SceneEventHandler SceneBeingSaved;

        public static event SceneEventHandler SceneBeingLoaded;

        public static event SceneEventHandler SceneBeingImported;

        void Awake()
        {
            Hooks.InstallHooks();
        }

        internal static void cardWriteEvent(ChaFile file)
        {
            if (CardBeingSaved == null)
                return;

            foreach (var entry in CardBeingSaved.GetInvocationList())
            {
                var handler = (CardEventHandler)entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingSaved)} - {ex}");
                }
            }
        }

        internal static void cardReadEvent(ChaFile file)
        {
            if (!LoadEventsEnabled || CardBeingLoaded == null)
                return;

            foreach (var entry in CardBeingLoaded.GetInvocationList())
            {
                var handler = (CardEventHandler)entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingLoaded)} - {ex}");
                }
            }
        }

        internal static void coordinateWriteEvent(ChaFileCoordinate file)
        {
            if (CoordinateBeingSaved == null)
                return;

            foreach (var entry in CoordinateBeingSaved.GetInvocationList())
            {
                var handler = (CoordinateEventHandler)entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CoordinateBeingSaved)} - {ex}");
                }
            }
        }

        internal static void coordinateReadEvent(ChaFileCoordinate file)
        {
            if (!LoadEventsEnabled || CoordinateBeingLoaded == null)
                return;

            foreach (var entry in CoordinateBeingLoaded.GetInvocationList())
            {
                var handler = (CoordinateEventHandler)entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CoordinateBeingLoaded)} - {ex}");
                }
            }
        }

        internal static void sceneWriteEvent(string path)
        {
            if (SceneBeingSaved == null)
                return;

            foreach (var entry in SceneBeingSaved.GetInvocationList())
            {
                var handler = (SceneEventHandler)entry;
                try
                {
                    handler.Invoke(path);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingSaved)} - {ex}");
                }
            }
        }

        internal static void sceneReadEvent(string path)
        {
            if (SceneBeingLoaded == null)
                return;

            foreach (var entry in SceneBeingLoaded.GetInvocationList())
            {
                var handler = (SceneEventHandler)entry;
                try
                {
                    handler.Invoke(path);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingLoaded)} - {ex}");
                }
            }
        }

        internal static void sceneImportEvent(string path)
        {
            if (SceneBeingImported == null)
                return;

            foreach (var entry in SceneBeingImported.GetInvocationList())
            {
                var handler = (SceneEventHandler)entry;
                try
                {
                    handler.Invoke(path);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingImported)} - {ex}");
                }
            }
        }

        #endregion

        public static Dictionary<string, PluginData> GetAllExtendedData(ChaFile file)
        {
            return internalCharaDictionary.Get(file);
        }

        public static Dictionary<string, PluginData> GetAllExtendedData(ChaFileCoordinate file)
        {
            return internalCoordinateDictionary.Get(file);
        }

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

        public static PluginData GetSceneExtendedDataById(string id)
        {
            if (id == null)
                return null;

            if (internalSceneDictionary.TryGetValue(id, out var extendedSection))
                return extendedSection;

            return null;
        }

        public static void SetSceneExtendedDataById(string id, PluginData extendedFormatData)
        {
            internalSceneDictionary[id] = extendedFormatData;
        }
    }
}