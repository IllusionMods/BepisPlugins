using System;
#if AI || HS2
using AIChara;
#endif

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        /// <summary> CardEventHandler </summary>
        public delegate void CardEventHandler(ChaFile file);
        /// <summary> Register methods to trigger on card being saved </summary>
        public static event CardEventHandler CardBeingSaved;
        /// <summary> Register methods to trigger on card being loaded </summary>
        public static event CardEventHandler CardBeingLoaded;
        /// <summary> CoordinateEventHandler </summary>
        public delegate void CoordinateEventHandler(ChaFileCoordinate file);
        /// <summary> Register methods to trigger on coordinate being saved </summary>
        public static event CoordinateEventHandler CoordinateBeingSaved;
        /// <summary> Register methods to trigger on coordinate being loaded </summary>
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingSaved)} - {ex}");
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CardBeingLoaded)} - {ex}");
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CoordinateBeingSaved)} - {ex}");
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(CoordinateBeingLoaded)} - {ex}");
                }
            }
        }
    }
}
