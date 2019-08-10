using BepInEx.Logging;
using System;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        public delegate void CardEventHandler(ChaFile file);
        public static event CardEventHandler CardBeingSaved;
        public static event CardEventHandler CardBeingLoaded;
        public delegate void CoordinateEventHandler(ChaFileCoordinate file);
        public static event CoordinateEventHandler CoordinateBeingSaved;
        public static event CoordinateEventHandler CoordinateBeingLoaded;

        internal static void CardWriteEvent(ChaFile file)
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

        internal static void CardReadEvent(ChaFile file)
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

        internal static void CoordinateWriteEvent(ChaFileCoordinate file)
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

        internal static void CoordinateReadEvent(ChaFileCoordinate file)
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
    }
}
