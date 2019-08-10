using BepInEx.Logging;
using System;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        public delegate void MapInfoEventHandler(HEdit.HEditData data);
        public delegate void ImportEventHandler(Dictionary<string, PluginData> importedExtendedData);

        /// <summary>
        /// Contains all extended data read from the KK card. Key is data GUID.
        /// Convert your data and write it back to the dictionary to get it saved.
        /// </summary>
        public static event ImportEventHandler CardBeingImported;

        /// <summary>
        /// Contains all extended data read from the KK card. Key is data GUID.
        /// Convert your data and write it back to the dictionary to get it saved.
        /// </summary>
        public static event ImportEventHandler CoordinateBeingImported;

        public static event MapInfoEventHandler HEditDataBeingSaved;
        public static event MapInfoEventHandler HEditDataBeingLoaded;

        private static void CardImportEvent(Dictionary<string, PluginData> data)
        {
            if (CardBeingImported != null)
            {
                foreach (var entry in CardBeingImported.GetInvocationList())
                {
                    var handler = (ImportEventHandler)entry;
                    try
                    {
                        handler.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingLoaded)} - {ex}");
                    }
                }
            }
        }

        private static void CoordinateImportEvent(Dictionary<string, PluginData> data)
        {
            if (CoordinateBeingImported != null)
            {
                foreach (var entry in CoordinateBeingImported.GetInvocationList())
                {
                    var handler = (ImportEventHandler)entry;
                    try
                    {
                        handler.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingLoaded)} - {ex}");
                    }
                }
            }
        }

        internal static void HEditDataWriteEvent(HEdit.HEditData data)
        {
            if (HEditDataBeingSaved == null)
                return;

            foreach (var entry in HEditDataBeingSaved.GetInvocationList())
            {
                var handler = (MapInfoEventHandler)entry;
                try
                {
                    handler.Invoke(data);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(HEditDataBeingSaved)} - {ex}");
                }
            }
        }

        internal static void HEditDataReadEvent(HEdit.HEditData data)
        {
            if (!LoadEventsEnabled || HEditDataBeingLoaded == null)
                return;

            foreach (var entry in HEditDataBeingLoaded.GetInvocationList())
            {
                var handler = (MapInfoEventHandler)entry;
                try
                {
                    handler.Invoke(data);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Subscriber crash in {nameof(ExtendedSave)}.{nameof(HEditDataBeingLoaded)} - {ex}");
                }
            }
        }
    }
}
