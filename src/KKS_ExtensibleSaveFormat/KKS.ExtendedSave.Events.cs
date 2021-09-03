using BepInEx.Logging;
using System;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        /// <summary> ImportEventHandler </summary>
        public delegate void ImportEventHandler(Dictionary<string, PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping);

        /// <summary>
        /// Contains all extended data read from the KK card. Key is data GUID.
        /// Convert your data and write it back to the dictionary to get it saved.
        /// </summary>
        public static event ImportEventHandler CardBeingImported;

        private static void CardImportEvent(Dictionary<string, PluginData> data, Dictionary<int, int?> coordinateMapping)
        {
            if (CardBeingImported != null)
            {
                foreach (var entry in CardBeingImported.GetInvocationList())
                {
                    var handler = (ImportEventHandler)entry;
                    try
                    {
                        handler.Invoke(data, coordinateMapping);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingLoaded)} - {ex}");
                    }
                }
            }
        }
    }
}
