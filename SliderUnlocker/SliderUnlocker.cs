using BepInEx;
using ChaCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepisPlugins;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SliderUnlocker
{
    [BepInPlugin(GUID, "Slider Unlocker", Version)]
    public class SliderUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.sliderunlocker";
        internal const string Version = Metadata.PluginsVersion;

        private readonly List<Target> _targets = new List<Target>();

        protected void Awake()
        {
            Hooks.InstallHooks();

            foreach (var type in typeof(CvsAccessory).Assembly.GetTypes())
            {
                if (type.Name.StartsWith("Cvs", StringComparison.OrdinalIgnoreCase) &&
                    type != typeof(CvsDrawCtrl) &&
                    type != typeof(CvsColor))
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    var inputFields = fields.Where(x => typeof(TMP_InputField).IsAssignableFrom(x.FieldType)).ToList();
                    if (inputFields.Count == 0) continue;

                    var sliders = fields.Where(x => typeof(Slider).IsAssignableFrom(x.FieldType)).ToList();
                    if (sliders.Count == 0) continue;

                    _targets.Add(new Target(type, inputFields, sliders));
                }
            }
        }

        private void LevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            SetAllSliders(scene, Minimum.Value / 100f, Maximum.Value / 100f);
        }

        private void SetAllSliders(Scene scene, float minimum, float maximum)
        {
            var possibleDigitCountIncludingMinus = Math.Max(
                Minimum.Value.ToString(CultureInfo.InvariantCulture).Length, 
                Maximum.Value.ToString(CultureInfo.InvariantCulture).Length);
            
            var sceneObjects = scene.GetRootGameObjects();

            foreach (var target in _targets)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;
                    
                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider) x.GetValue(cvs);
                        if (slider != null)
                        {
                            // Has issues
                            if (x.Name == "sldWaistLowW")
                                continue;

                            slider.minValue = minimum;
                            slider.maxValue = maximum;
                        }
                    }

                    foreach (var x in target.Fields)
                    {
                        var inputField = (TMP_InputField) x.GetValue(cvs);
                        if (inputField != null)
                        {
                            inputField.characterLimit = possibleDigitCountIncludingMinus;
                        }
                    }
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

        private sealed class Target
        {
            public Target(Type type, List<FieldInfo> fields, List<FieldInfo> sliders)
            {
                Type = type;
                Fields = fields;
                Sliders = sliders;
            }
            public readonly Type Type;
            public readonly List<FieldInfo> Fields;
            public readonly List<FieldInfo> Sliders;
        }
    }
}