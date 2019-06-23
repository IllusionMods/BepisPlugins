using BepInEx;
using System.ComponentModel;
using BepisPlugins;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

namespace ColorCorrector
{
    [BepInPlugin(GUID: GUID, Name: "Color Filter Remover", Version: Version)]
    public class ColorCorrector : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.colorcorrector";
        public const string Version = Metadata.PluginsVersion;

        #region Config properties
        [DisplayName("!Enable saturation filter")]
        [Category("Post processing settings")]
        private ConfigWrapper<bool> SaturationEnabled { get; set; }

        [DisplayName("Strength of the bloom filter")]
        [Category("Post processing settings")]
        [Description("Strength of the bloom filter. Not active in Studio, control bloom settings through the in game Scene Effects menu.")]
        [AcceptableValueRange(0f, 1f)]
        private ConfigWrapper<float> BloomStrength { get; set; }
        #endregion

        AmplifyColorEffect amplifyComponent;
        BloomAndFlares bloomComponent;

        private void Start()
        {
            if(Application.productName == "CharaStudio")
            {
                enabled = false;
                return;
            }

            SaturationEnabled = new ConfigWrapper<bool>("SaturationEnabled", this, true);
            BloomStrength = new ConfigWrapper<float>("BloomStrength", this, 1);
            SaturationEnabled.SettingChanged += OnSettingChanged;
            BloomStrength.SettingChanged += OnSettingChanged;

            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (Camera.main != null)
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

        private void OnSettingChanged(object sender, System.EventArgs e)
        {
            SetEffects(SaturationEnabled.Value, BloomStrength.Value);
        }
    }
}
