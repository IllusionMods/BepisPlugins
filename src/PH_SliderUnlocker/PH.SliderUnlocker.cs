using BepInEx;
using BepisPlugins;
using HarmonyLib;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class SliderUnlocker : BaseUnityPlugin
    {
        internal void Main() => SceneManager.sceneLoaded += (s, lsm) => StartCoroutine(SetAllSliders());

        /// <summary>
        /// Set all sliders to their default min/max and set up events
        /// </summary>
        private IEnumerator SetAllSliders()
        {
            yield return null;

            var editMode = FindObjectOfType<EditMode>();
            if (editMode == null) yield break;

            foreach (var x in editMode.GetComponentsInChildren<InputSliderUI>(true))
            {
                CheckableSlider slider = Traverse.Create(x).Field("slider").GetValue() as CheckableSlider;
                InputField inputField = Traverse.Create(x).Field("inputField").GetValue() as InputField;
                Button defButton = Traverse.Create(x).Field("defButton").GetValue() as Button;

                slider.minValue = SliderMin;
                slider.maxValue = SliderMax;

                bool buttonClicked = false;

                inputField.characterLimit = 4;

                //When the user types a value, unlock the sliders to accomodate
                inputField.onEndEdit.AddListener(_ => UnlockSliderFromInput(slider, inputField));
                
                //When the button is clicked set a flag used by InputFieldOnValueChanged
                defButton.onClick.AddListener(() => UnlockSliderFromInput(slider, inputField));
            }
        }
        /// <summary>
        /// Unlock sliders to their maximum possible size
        /// </summary>
        internal static void MaximizeSliders()
        {
            foreach (var x in FindObjectsOfType<InputSliderUI>())
            {
                CheckableSlider slider = Traverse.Create(x).Field("slider").GetValue() as CheckableSlider;
                slider.maxValue = SliderAbsoluteMax / 100;
                slider.minValue = SliderAbsoluteMin / 100;
            }
        }
        /// <summary>
        /// Lock sliders down based on their current value
        /// </summary>
        internal static void UnlockSliders()
        {
            foreach (var x in FindObjectsOfType<InputSliderUI>())
            {
                CheckableSlider slider = Traverse.Create(x).Field("slider").GetValue() as CheckableSlider;
                UnlockSlider(slider, slider.value);
            }
        }
    }
}
