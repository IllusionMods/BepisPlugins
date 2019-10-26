using System;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        /// <summary> SceneEventHandler </summary>
        public delegate void SceneEventHandler(string path);
        /// <summary> Register methods to trigger on scene being saved </summary>
        public static event SceneEventHandler SceneBeingSaved;
        /// <summary> Register methods to trigger on scene being loaded </summary>
        public static event SceneEventHandler SceneBeingLoaded;
        /// <summary> Register methods to trigger on scene being imported </summary>
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingSaved)} - {ex}");
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingLoaded)} - {ex}");
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
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(SceneBeingImported)} - {ex}");
                }
            }
        }
    }
}
