using Character;
using System;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        /// <summary> CardEventHandler </summary>
        public delegate void CardEventHandler(CustomParameter file);
        /// <summary> Register methods to trigger on card being saved </summary>
        public static event CardEventHandler CardBeingSaved;
        /// <summary> Register methods to trigger on card being loaded </summary>
        public static event CardEventHandler CardBeingLoaded;
        /// <summary> CoordinateEventHandler </summary>
        public delegate void CoordinateEventHandler(CustomParameter file);
        /// <summary> Register methods to trigger on coordinate being saved </summary>
        public static event CoordinateEventHandler CoordinateBeingSaved;
        /// <summary> Register methods to trigger on coordinate being loaded </summary>
        public static event CoordinateEventHandler CoordinateBeingLoaded;

        internal static void CardWriteEvent(CustomParameter file)
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

        internal static void CardReadEvent(CustomParameter file)
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

        internal static void CoordinateWriteEvent(CustomParameter file)
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

        internal static void CoordinateReadEvent(CustomParameter file)
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
