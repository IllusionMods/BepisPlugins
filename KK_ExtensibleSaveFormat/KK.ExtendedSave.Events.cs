using BepInEx.Logging;
using System;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        public delegate void SceneEventHandler(string path);
        public static event SceneEventHandler SceneBeingSaved;
        public static event SceneEventHandler SceneBeingLoaded;
        public static event SceneEventHandler SceneBeingImported;

        internal static void SceneWriteEvent(string path)
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

        internal static void SceneReadEvent(string path)
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

        internal static void SceneImportEvent(string path)
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
    }
}
