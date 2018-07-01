using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IPALoader
{
    public class IPAPlugin : MonoBehaviour
    {
        public IPlugin BasePlugin { get; set; }

        bool init = false;

        public IPAPlugin()
        {
            BasePlugin = IPALoader.pluginToLoad;
        }

        void Awake()
        {
            BasePlugin.OnApplicationStart();

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                BasePlugin.OnLevelWasLoaded(scene.buildIndex);
                init = true;
            };
        }

        void Start()
        {
            BasePlugin.OnLevelWasLoaded(SceneManager.GetActiveScene().buildIndex);
        }

        void Update()
        {
            if (init)
            {
                BasePlugin.OnLevelWasInitialized(SceneManager.GetActiveScene().buildIndex);
                init = false;
            }

            BasePlugin.OnUpdate();
        }

        void LateUpdate()
        {
            if (BasePlugin is IEnhancedPlugin)
                ((IEnhancedPlugin)BasePlugin).OnLateUpdate();
        }

        void FixedUpdate()
        {
            BasePlugin.OnFixedUpdate();
        }
        
        void OnApplicationQuit()
        {
            BasePlugin.OnApplicationQuit();
        }
    }
}
