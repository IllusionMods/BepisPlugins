using System;
using Studio;

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

        /// <summary> PoseEventHandler </summary>
        public delegate void PoseEventHandler(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar);
        /// <summary> Register methods to trigger on pose being saved </summary>
        public static event PoseEventHandler PoseBeingSaved;
        /// <summary> Register methods to trigger on pose being loaded </summary>
        public static event PoseEventHandler PoseBeingLoaded;

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

        internal static void PoseWriteEvent(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar)
        {
            if (PoseBeingSaved == null)
                return;

            foreach (var entry in PoseBeingSaved.GetInvocationList())
            {
                var handler = (PoseEventHandler)entry;
                try
                {
                    handler.Invoke(poseName, fileInfo, ociChar);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(PoseBeingSaved)} - {ex}");
                }
            }
        }

        internal static void PoseReadEvent(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar)
        {
            if (PoseBeingLoaded == null)
                return;

            foreach (var entry in PoseBeingLoaded.GetInvocationList())
            {
                var handler = (PoseEventHandler)entry;
                try
                {
                    handler.Invoke(poseName, fileInfo, ociChar);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Subscriber crash in {nameof(ExtendedSave)}.{nameof(PoseBeingLoaded)} - {ex}");
                }
            }
        }
    }
}
