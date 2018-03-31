using BepInEx;
using BepisPlugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtensibleSaveFormat
{
    [BepInPlugin(GUID: "com.bepis.bepinex.extendedsave", Name: "Extended Save", Version: "1.2")]
    public class ExtendedSave : BaseUnityPlugin
    {
        void Awake()
        {
            Hooks.InstallHooks();
        }

        internal static WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>> internalDictionary = new WeakKeyDictionary<ChaFile, Dictionary<string, PluginData>>();

        #region Events

        public delegate void CardEventHandler(ChaFile file);

        public static event CardEventHandler CardBeingSaved;

        public static event CardEventHandler CardBeingLoaded;

        internal static void writeEvent(ChaFile file)
        {
            CardBeingSaved?.Invoke(file);
        }

        internal static void readEvent(ChaFile file)
        {
            CardBeingLoaded?.Invoke(file);
        }

        #endregion

        public static Dictionary<string, PluginData> GetAllExtendedData(ChaFile file)
        {
            return internalDictionary.Get(file);
        }

        public static PluginData GetExtendedDataById(ChaFile file, string id)
        {
            PluginData extendedSection = null;
            if (internalDictionary.Get(file).TryGetValue(id, out extendedSection))
                return extendedSection;
            return null;
        }

        public static void SetExtendedDataById(ChaFile file, string id, PluginData extendedFormatData)
        {
            internalDictionary.Get(file)[id] = extendedFormatData;
        }
    }
}
