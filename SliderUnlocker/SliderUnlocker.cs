using BepInEx;
using ChaCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInPlugin(GUID: "com.bepis.bepinex.sliderunlocker", Name: "Slider Unlocker", Version: "1.5")]
    public class SliderUnlocker : BaseUnityPlugin
    {
        protected void Awake()
        {
            Hooks.InstallHooks();
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            SetAllSliders(scene, Minimum.Value / 100f, Maximum.Value / 100f);
        }

        public void SetAllSliders(Scene scene, float minimum, float maximum)
        {
            List<object> cvsInstances = new List<object>();

            Assembly illusion = typeof(CvsAccessory).Assembly;

            var sceneObjects = scene.GetRootGameObjects();

            foreach (Type type in illusion.GetTypes())
            {
                if (type.Name.ToUpper().StartsWith("CVS") &&
                    type != typeof(CvsDrawCtrl) &&
                    type != typeof(CvsColor))
                {
                    foreach (var obj in sceneObjects)
                    {
                        cvsInstances.AddRange(obj.GetComponentsInChildren(type));
                    }
                }
            }

            foreach (object cvs in cvsInstances)
            {
                if (cvs == null)
                    continue;

                var fields = cvs.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (Slider slider in fields.Where(x => typeof(Slider).IsAssignableFrom(x.FieldType)).Select(x => x.GetValue(cvs)))
                {
                    if (slider == null)
                        continue;

                    slider.minValue = minimum;
                    slider.maxValue = maximum;
                }

                var possibleDigitCountIncludingMinus = Math.Max(Minimum.Value.ToString(CultureInfo.InvariantCulture).Length, Maximum.Value.ToString(CultureInfo.InvariantCulture).Length);
                foreach (TMP_InputField inputField in fields.Where(x => typeof(TMP_InputField).IsAssignableFrom(x.FieldType)).Select(x => x.GetValue(cvs)))
                {
                    if (inputField == null)
                        continue;

                    inputField.characterLimit = possibleDigitCountIncludingMinus;
                }
            }
        }

        #region MonoBehaviour

        protected void OnEnable()
        {
            SceneManager.sceneLoaded += LevelFinishedLoading;
        }

        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= LevelFinishedLoading;
        }
        #endregion

        #region Settings
        [DisplayName("Minimum slider value")]
        [Description("Changes will take effect next time the editor is lodaded.")]
        [AcceptableValueRange(-500, 0, false)]
        public ConfigWrapper<int> Minimum { get; }

        [DisplayName("Maximum slider value")]
        [Description("Changes will take effect next time the editor is lodaded.")]
        [AcceptableValueRange(100, 500, false)]
        public ConfigWrapper<int> Maximum { get; }

        public SliderUnlocker()
        {
            Minimum = new ConfigWrapper<int>("wideslider-minimum", this, -100);
            Maximum = new ConfigWrapper<int>("wideslider-maximum", this, 200);
        }
        #endregion
    }
}