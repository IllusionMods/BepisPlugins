using System;
using AIProject.SaveData;
using Housing;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        /// <summary> MainGameSaveEventHandler </summary>
        public delegate void MainGameSaveEventHandler(SaveData file);

        /// <summary> Register methods to trigger on main game data being saved </summary>
        public static event MainGameSaveEventHandler MainGameSaveBeingSaved;

        /// <summary> Register methods to trigger on main game data being loaded </summary>
        public static event MainGameSaveEventHandler MainGameSaveBeingLoaded;

        /// <summary> HousingEventHandler </summary>
        public delegate void HousingEventHandler(CraftInfo file);

        /// <summary> Register methods to trigger on housing data being saved </summary>
        public static event HousingEventHandler HousingBeingSaved;

        /// <summary> Register methods to trigger on housing data being loaded </summary>
        public static event HousingEventHandler HousingBeingLoaded;

        internal static void MainGameSaveWriteEvent(SaveData file)
        {
            if (MainGameSaveBeingSaved == null)
                return;

            foreach (var entry in MainGameSaveBeingSaved.GetInvocationList())
            {
                var handler = (MainGameSaveEventHandler) entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(MainGameSaveBeingSaved)} - {ex}");
                }
            }
        }

        internal static void MainGameSaveReadEvent(SaveData file)
        {
            if (MainGameSaveBeingLoaded == null)
                return;

            foreach (var entry in MainGameSaveBeingLoaded.GetInvocationList())
            {
                var handler = (MainGameSaveEventHandler) entry;
                try
                {
                    handler.Invoke(file);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(MainGameSaveBeingLoaded)} - {ex}");
                }
            }
        }

        internal static void HousingWriteEvent(CraftInfo craftInfo)
        {
            if (HousingBeingSaved == null)
                return;

            foreach (var entry in HousingBeingSaved.GetInvocationList())
            {
                var handler = (HousingEventHandler) entry;
                try
                {
                    handler.Invoke(craftInfo);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(HousingBeingSaved)} - {ex}");
                }
            }
        }

        internal static void HousingReadEvent(CraftInfo craftInfo)
        {
            if (HousingBeingLoaded == null)
                return;

            foreach (var entry in HousingBeingLoaded.GetInvocationList())
            {
                var handler = (HousingEventHandler) entry;
                try
                {
                    handler.Invoke(craftInfo);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(HousingBeingLoaded)} - {ex}");
                }
            }
        }
    }
}
