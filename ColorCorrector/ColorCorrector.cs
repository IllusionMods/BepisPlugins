using BepInEx;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

namespace ColorCorrector
{
    [BepInPlugin(GUID: "com.bepis.bepinex.colorcorrector", Name: "Color Filter Remover", Version: "1.2")]
    public class ColorCorrector : BaseUnityPlugin
    {
        #region Config properties
        [DisplayName("Enable saturation filter")]
        [DefaultValue(true)]
        private ConfigWrapper<bool> SaturationEnabled { get; set; }

        [DisplayName("Strength of the bloom filter")]
        [DefaultValue(1f)]
        [AcceptableValueRange(0f, 1f)]
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
        private Rect UI = new Rect(20, 20, 300, 100);
        private bool showingUI = false;

        void OnGUI()
        {
            if (showingUI)
                UI = GUI.Window("com.bepis.bepinex.colorcorrector".GetHashCode() + 0, UI, WindowFunction, "Post processing settings");
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
