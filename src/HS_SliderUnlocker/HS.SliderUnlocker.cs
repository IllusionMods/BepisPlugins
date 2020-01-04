using BepInEx;
using BepisPlugins;
using CustomMenu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInProcess(Constants.BattleArenaProcessName)]
    [BepInProcess(Constants.BattleArenaProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class SliderUnlocker : BaseUnityPlugin
    {
        private static readonly List<SliderSet> SliderSetList = new List<SliderSet>();

        private void OnLevelWasLoaded(int level)
        {
            CustomScene customScene = FindObjectOfType<CustomScene>();
            if (customScene == null)
                SliderSetList.Clear();
            else
                SetAllSliders(customScene);
        }
        /// <summary>
        /// Set all sliders to their default min/max and set up events
        /// </summary>
        private void SetAllSliders(CustomScene customScene)
        {
            SliderSetList.Clear();

            SubMenuItem[] smItem = customScene.customControl.subMenuCtrl.smItem;
            for (int i = 0; i < smItem.Length; i++)
            {
                GameObject objTop = smItem[i].objTop;
                FindSliders(objTop.transform);
            }

            foreach (var target in SliderSetList)
            {
                target.Slider.minValue = SliderMin;
                target.Slider.maxValue = SliderMax;

                bool buttonClicked = false;

                target.InputField.characterLimit = 4;

                //After reset button click, reset the slider unlock state
                target.InputField.onValueChanged.AddListener(
                    _ =>
                    {
                        if (buttonClicked)
                        {
                            buttonClicked = false;
                            UnlockSliderFromInput(target.Slider, target.InputField);
                        }
                    });

                //When the user types a value, unlock the sliders to accomodate
                target.InputField.onEndEdit.AddListener(_ => UnlockSliderFromInput(target.Slider, target.InputField));

                //When the button is clicked set a flag used by InputFieldOnValueChanged
                target.Button.onClick.AddListener(() => buttonClicked = true);
            }
        }
        /// <summary>
        /// Find all sliders that have an associated input field and button
        /// </summary>
        private void FindSliders(Transform t)
        {
            Slider slider = t.gameObject.GetComponent<Slider>();
            if (slider != null)
            {
                InputField inputField = slider.transform.parent.GetComponentInChildren<InputField>();
                Button button = slider.transform.parent.GetComponentInChildren<Button>();

                if (inputField != null && button != null)
                    SliderSetList.Add(new SliderSet(slider, inputField, button));
            }

            for (int i = 0; i < t.childCount; i++)
                FindSliders(t.GetChild(i));
        }
        /// <summary>
        /// Unlock sliders to their maximum possible size
        /// </summary>
        internal static void MaximizeSliders()
        {
            foreach (var target in SliderSetList)
            {
                if (target.Slider.IsActive())
                {
                    target.Slider.maxValue = SliderAbsoluteMax;
                    target.Slider.minValue = SliderAbsoluteMin;
                }
            }
        }
        /// <summary>
        /// Lock sliders down based on their current value
        /// </summary>
        internal static void UnlockSliders()
        {
            foreach (var target in SliderSetList)
            {
                if (target.Slider.IsActive())
                    UnlockSlider(target.Slider, target.Slider.value);
            }
        }
        /// <summary>
        /// A set of associated slider, inputfield, and button
        /// </summary>
        private sealed class SliderSet
        {
            public readonly Slider Slider;
            public readonly InputField InputField;
            public readonly Button Button;

            public SliderSet(Slider slider, InputField inputField, Button button)
            {
                Slider = slider;
                InputField = inputField;
                Button = button;
            }
        }
    }
}
