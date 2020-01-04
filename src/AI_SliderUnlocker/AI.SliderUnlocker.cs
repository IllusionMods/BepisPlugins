using BepInEx;
using BepisPlugins;
using CharaCustom;
using System.Collections;
using UnityEngine.SceneManagement;

namespace SliderUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class SliderUnlocker : BaseUnityPlugin
    {
        private void Main() => SceneManager.sceneLoaded += (s, lsm) => SetAllSliders();

        /// <summary>
        /// Set all sliders to their default min/max and set up events
        /// </summary>
        private static void SetAllSliders()
        {
            foreach (var x in FindObjectsOfType<CustomSliderSet>())
            {
                UnlockSlider(x.slider, x.slider.value);

                bool buttonClicked = false;

                x.input.characterLimit = 4;

                //After reset button click, reset the slider unlock state
                x.input.onValueChanged.AddListener(
                    _ =>
                    {
                        if (buttonClicked)
                        {
                            buttonClicked = false;
                            UnlockSliderFromInput(x.slider, x.input);
                        }
                    });


                //When the user types a value, unlock the sliders to accomodate
                x.input.onEndEdit.AddListener(_ => UnlockSliderFromInput(x.slider, x.input));

                //When the button is clicked set a flag used by InputFieldOnValueChanged
                x.button.onClick.AddListener(() => buttonClicked = true);
            }
        }
        internal static IEnumerator ResetAllSliders()
        {
            MaximizeSliders();
            yield return null;
            foreach (var x in FindObjectsOfType<CustomSliderSet>())
                UnlockSlider(x.slider, x.slider.value);
        }
        /// <summary>
        /// Unlock sliders to their maximum possible size
        /// </summary>
        internal static void MaximizeSliders()
        {
            foreach (var x in FindObjectsOfType<CustomSliderSet>())
            {
                x.slider.maxValue = SliderAbsoluteMax;
                x.slider.minValue = SliderAbsoluteMin;
            }
        }
        /// <summary>
        /// Lock sliders down based on their current value
        /// </summary>
        internal static void UnlockSliders()
        {
            foreach (var x in FindObjectsOfType<CustomSliderSet>())
                UnlockSlider(x.slider, x.slider.value);
        }
    }
}
