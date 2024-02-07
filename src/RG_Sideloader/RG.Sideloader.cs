using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils.Collections;
using BepInEx.Logging;
using BepisPlugins;
using System.Collections;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace Sideloader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class Sideloader : BasePlugin
    {
        private const string DefaultGameNameList = "RG;RoomGirl;Room Girl";
        internal static string[] GameNameList;

        private static Sideloader Instance;
        internal static ManualLogSource Logger;
        private MonoBehaviour Behaviour;

        private static string FindKoiZipmodDir() => string.Empty;

        /// <inheritdoc/>
        public override void Load()
        {
            Instance = this;
            Logger = Log;

            XUnity.ResourceRedirector.Hooks.ResourceRedirectorIl2CppFix.PatchAll();

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MonoBehaviour>();
                var gameObject = new GameObject(nameof(Sideloader));
                gameObject.hideFlags |= HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(gameObject);
                Behaviour = gameObject.AddComponent<MonoBehaviour>();
            }
            catch
            {
                Log.LogError($"FAILED to Register Il2Cpp Type: {nameof(Sideloader)}.{nameof(MonoBehaviour)}!");
            }
        }

        private class MonoBehaviour : UnityEngine.MonoBehaviour
        {
            private void Awake() => Instance.Awake();
        }

        internal static Coroutine StartCoroutine(IEnumerator routine) =>
            Instance.Behaviour.StartCoroutine(new Il2CppManagedEnumerator(routine).Cast<Il2CppSystem.Collections.IEnumerator>());
    }
}
