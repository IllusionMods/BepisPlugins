using BepInEx;
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
        [DisplayName("!Enable saturation filter")]
        [Category("Post processing settings")]
        private ConfigWrapper<bool> SaturationEnabled { get; set; }

        [DisplayName("Strength of the bloom filter")]
        [Category("Post processing settings")]
        [AcceptableValueRange(0f, 1f)]
        private ConfigWrapper<float> BloomStrength { get; set; }
        #endregion

        AmplifyColorEffect amplifyComponent;
        BloomAndFlares bloomComponent;

        public ColorCorrector()
        {
            SaturationEnabled = new ConfigWrapper<bool>("SaturationEnabled", this, true);
            BloomStrength = new ConfigWrapper<float>("BloomStrength", this, 1);
            SaturationEnabled.SettingChanged += OnSettingChanged;
            BloomStrength.SettingChanged += OnSettingChanged;
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

        private void SetEffects(bool satEnabled, float bloomPower)
        {
            //TODO allow to modify amplifyComponent.Exposure and others if possible
            if (amplifyComponent != null)
                amplifyComponent.enabled = satEnabled;

            if (bloomComponent != null)
                bloomComponent.bloomIntensity = bloomPower;
        }

        protected void OnEnable()
        {
            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= LevelFinishedLoading;
        }

        private void OnSettingChanged(object sender, System.EventArgs e)
        {
            SetEffects(SaturationEnabled.Value, BloomStrength.Value);
        }
    }
}
