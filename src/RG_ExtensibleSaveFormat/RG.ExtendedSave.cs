using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils.Collections;
using BepInEx.Logging;
using BepisPlugins;
using System.Collections;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace ExtensibleSaveFormat
{
    [BepInIncompatibility("com.bogus.RGExtendedSave")]
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ExtendedSave : BasePlugin
    {
        private static ExtendedSave Instance;
        internal static ManualLogSource Logger;
        private MonoBehaviour Behaviour;

        /// <inheritdoc/>
        public override void Load()
        {
            Instance = this;
            Logger = Log;

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MonoBehaviour>();
                var gameObject = new GameObject(nameof(ExtensibleSaveFormat));
                gameObject.hideFlags |= HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(gameObject);
                Behaviour = gameObject.AddComponent<MonoBehaviour>();
            }
            catch
            {
                Log.LogError($"FAILED to Register Il2Cpp Type: {nameof(ExtendedSave)}.{nameof(MonoBehaviour)}!");
            }
        }

        private class MonoBehaviour : UnityEngine.MonoBehaviour
        {
            private void Awake() => Instance.Awake();
        }

        internal static Coroutine StartCoroutine(IEnumerator routine) =>
            Instance.Behaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}
