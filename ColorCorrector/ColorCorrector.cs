using BepInEx;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

namespace ColorCorrector
{
    public class ColorCorrector : BaseUnityPlugin
    {
        public override string ID => "com.bepis.bepinex.colorcorrector";
        public override string Name => "Color Filter Remover";
        public override Version Version => new Version("1.2");

        #region Config properties
        private ConfigWrapper<bool> SaturationEnabled { get; set; }
        private ConfigWrapper<float> BloomStrength { get; set; }
        #endregion

        AmplifyColorEffect amplifyComponent;
        BloomAndFlares bloomComponent;

        public ColorCorrector()
        {
            SaturationEnabled = new ConfigWrapper<bool>("SaturationEnabled", this, true);
            BloomStrength = new ConfigWrapper<float>("BloomStrength", this, 1);
        }

        protected void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (Camera.main != null && Camera.main?.gameObject != null)
            {
                amplifyComponent = Camera.main.gameObject.GetComponent<AmplifyColorEffect>();
                bloomComponent = Camera.main.gameObject.GetComponent<BloomAndFlares>();

                SetEffects(SaturationEnabled.Value, BloomStrength.Value);
            }
        }

        void SetEffects(bool satEnabled, float bloomPower)
        {
            if (amplifyComponent != null)
                amplifyComponent.enabled = satEnabled;

            if (bloomComponent != null)
                bloomComponent.bloomIntensity = bloomPower;
        }

        #region MonoBehaviour
        void OnEnable()
        {
            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= LevelFinishedLoading;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F6))
            {
                showingUI = !showingUI;
            }
        }
        #endregion

        #region UI 
        private Rect UI = new Rect(20, 20, 200, 100);
        private bool showingUI = false;

        void OnGUI()
        {
            if (showingUI)
                UI = GUI.Window(Name.GetHashCode() + 0, UI, WindowFunction, "Filter settings");
        }

        void WindowFunction(int windowID)
        {
            bool satEnabled = GUI.Toggle(new Rect(10, 20, 180, 20), SaturationEnabled.Value, " Saturation filter enabled");

            GUI.Label(new Rect(10, 40, 180, 20), "Bloom filter strength");

            float bloomPower = GUI.HorizontalSlider(new Rect(10, 60, 180, 20), BloomStrength.Value, 0, 1);

            if (GUI.changed)
            {
                SaturationEnabled.Value = satEnabled;
                BloomStrength.Value = bloomPower;

                SetEffects(satEnabled, bloomPower);
            }

            GUI.DragWindow();
        }
        #endregion
    }
}
