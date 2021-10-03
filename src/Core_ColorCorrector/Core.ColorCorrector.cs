using BepInEx.Configuration;
using BepisPlugins;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

namespace ColorCorrector
{
    /// <summary>
    /// Allows toggling on/off the color saturation filter and adjusting the strength of bloom.
    /// </summary>
    public partial class ColorCorrector
    {
        public const string GUID = "com.bepis.bepinex.colorcorrector";
        public const string PluginName = "Color Filter Remover";
        public const string Version = Metadata.PluginsVersion;

        private ConfigEntry<bool> SaturationEnabled { get; set; }
        private ConfigEntry<float> BloomStrength { get; set; }

        private AmplifyColorEffect _amplifyComponent;
        private BloomAndFlares _bloomComponent;

#if KK || EC
        private const float DefaultBloomStrength = 1.5f;
#elif KKS
        private const float DefaultBloomStrength = 0.5f;
#endif
        private const float MaxBloomStrength = 2.0f;

        private void Start()
        {
            SaturationEnabled = Config.Bind("Post Processing Settings", "Enable saturation filter", true, new ConfigDescription("Whether default saturation filter will be applied to the game. This setting has no effect in Studio."));
            BloomStrength = Config.Bind("Post Processing Settings", "Bloom strength", DefaultBloomStrength, new ConfigDescription("Strength of the bloom filter. Not active in Studio, control bloom settings through the in game Scene Effects menu.", new AcceptableValueRange<float>(0f, MaxBloomStrength)));
            SaturationEnabled.SettingChanged += OnSettingChanged;
            BloomStrength.SettingChanged += OnSettingChanged;

            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (Camera.main != null)
            {
                _amplifyComponent = Camera.main.gameObject.GetComponent<AmplifyColorEffect>();
                _bloomComponent = Camera.main.gameObject.GetComponent<BloomAndFlares>();

#if KK || EC
                SetEffects(SaturationEnabled.Value, BloomStrength.Value);
#elif KKS
                StartCoroutine(DelayMethod(() =>
                {
                    SetEffects(SaturationEnabled.Value, BloomStrength.Value);
                }));
#endif
            }
        }

        private void SetEffects(bool satEnabled, float bloomPower)
        {
            if (_amplifyComponent != null)
                _amplifyComponent.enabled = satEnabled;

            if (_bloomComponent != null)
                _bloomComponent.bloomIntensity = bloomPower;
        }
        private IEnumerator DelayMethod(Action action)
        {
            yield return null;
            action();
        }

        private void OnSettingChanged(object sender, System.EventArgs e) => SetEffects(SaturationEnabled.Value, BloomStrength.Value);
    }
}
