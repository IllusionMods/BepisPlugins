using BepInEx;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InputUnlocker
{
    [BepInPlugin("com.bepis.bepinex.inputunlocker", "Input Length Unlocker", "1.0.1")]
    internal class InputUnlocker : BaseUnityPlugin
    {
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