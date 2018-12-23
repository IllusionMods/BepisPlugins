using BepisPlugins;
using BepInEx;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InputUnlocker
{
    [BepInPlugin(GUID, "Input Length Unlocker", Version)]
    internal class InputUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.inputunlocker";
        public const string Version = Metadata.PluginsVersion;

        protected void Awake()
        {
            foreach (var inputFieldObject in FindObjectsOfType<InputField>())
                UnlockInput(inputFieldObject);
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            foreach (var obj in scene.GetRootGameObjects())
            foreach (var inputFieldObject in obj.GetComponentsInChildren<InputField>(true))
                UnlockInput(inputFieldObject);
        }

        private void UnlockInput(InputField input)
        {
            input.characterLimit = 999;
        }
        
        protected void OnEnable()
        {
            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= LevelFinishedLoading;
        }
    }
}