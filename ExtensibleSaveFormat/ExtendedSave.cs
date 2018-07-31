using BepInEx;
using BepisPlugins;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID: "com.bepis.bepinex.extendedsave", Name: "Extended Save", Version: "1.5")]
    public class ExtendedSave : BaseUnityPlugin
    {
        void Awake()
        {
            Hooks.InstallHooks();
        }

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

        internal static void cardWriteEvent(ChaFile file)
        {
            CardBeingSaved?.Invoke(file);
        }

	    internal static void cardReadEvent(ChaFile file)
	    {
		    CardBeingLoaded?.Invoke(file);
	    }

	    internal static void coordinateWriteEvent(ChaFileCoordinate file)
	    {
		    CoordinateBeingSaved?.Invoke(file);
	    }

	    internal static void coordinateReadEvent(ChaFileCoordinate file)
	    {
		    CoordinateBeingLoaded?.Invoke(file);
	    }

        internal static void sceneWriteEvent(string path)
        {
            SceneBeingSaved?.Invoke(path);
        }

        internal static void sceneReadEvent(string path)
        {
            SceneBeingLoaded?.Invoke(path);
        }

        internal static void sceneImportEvent(string path)
        {
            SceneBeingImported?.Invoke(path);
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