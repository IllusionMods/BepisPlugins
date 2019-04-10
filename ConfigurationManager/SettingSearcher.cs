using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ConfigurationManager.Utilities;

namespace ConfigurationManager
{
    internal static class SettingSearcher
    {
        private static readonly ICollection<string> _updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        public static void CollectSettings(out IEnumerable<PropSettingEntry> results, out List<string> modsWithoutSettings, bool showDebug)
        {
            var baseSettingType = typeof(ConfigWrapper<>);
            results = Enumerable.Empty<PropSettingEntry>();
            modsWithoutSettings = new List<string>();
            foreach (var plugin in Utils.FindPlugins())
            {
                var type = plugin.GetType();

                var pluginInfo = MetadataHelper.GetMetadata(type);
                if (pluginInfo == null)
                {
                    Logger.Log(LogLevel.Error, $"Plugin {type.FullName} is missing the BepInPlugin attribute!");
                    modsWithoutSettings.Add(type.FullName);
                    continue;
                }

                if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .Any(x => !x.Browsable))
                {
                    modsWithoutSettings.Add(pluginInfo.Name);
                    continue;
                }

                var detected = new List<PropSettingEntry>();

                // Config wrappers ------

                var settingProps = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FilterBrowsable(true, true);

                var settingFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(f => !f.IsSpecialName)
                    .FilterBrowsable(true, true)
                    .Select(f => new FieldToPropertyInfoWrapper(f));

                var settingEntries = settingProps.Concat(settingFields.Cast<PropertyInfo>())
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                detected.AddRange(settingEntries.Select(x => PropSettingEntry.FromConfigWrapper(plugin, x, pluginInfo, plugin)).Where(x => x != null));

                // Config wrappers static ------

                var settingStaticProps = type
                    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FilterBrowsable(true, true);

                var settingStaticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Where(f => !f.IsSpecialName)
                    .FilterBrowsable(true, true)
                    .Select(f => new FieldToPropertyInfoWrapper(f));

                var settingStaticEntries = settingStaticProps.Concat(settingStaticFields.Cast<PropertyInfo>())
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                detected.AddRange(settingStaticEntries.Select(x => PropSettingEntry.FromConfigWrapper(null, x, pluginInfo, plugin)).Where(x => x != null));

                // Normal properties ------

                bool IsPropSafeToShow(PropertyInfo p)
                {
                    return p.GetSetMethod()?.IsPublic == true && (p.PropertyType.IsValueType || p.PropertyType == typeof(string));
                }

                var normalPropsSafeToShow = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(IsPropSafeToShow)
                    .FilterBrowsable(true, true)
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                var normalPropsWithBrowsable = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FilterBrowsable(true, false)
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                var normalProps = normalPropsSafeToShow.Concat(normalPropsWithBrowsable).Distinct();

                detected.AddRange(normalProps.Select(x => PropSettingEntry.FromNormalProperty(plugin, x, pluginInfo, plugin)).Where(x => x != null));

                // Normal static properties ------

                var normalStaticPropsSafeToShow = type
                    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(IsPropSafeToShow)
                    .FilterBrowsable(true, true)
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                var normalStaticPropsWithBrowsable = type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .FilterBrowsable(true, false)
                    .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));

                var normalStaticProps = normalStaticPropsSafeToShow.Concat(normalStaticPropsWithBrowsable).Distinct();

                detected.AddRange(normalStaticProps.Select(x => PropSettingEntry.FromNormalProperty(null, x, pluginInfo, plugin)).Where(x => x != null));

                detected.RemoveAll(x => x.Browsable == false);

                if (!detected.Any())
                    modsWithoutSettings.Add(pluginInfo.Name);

                // Allow to enable/disable plugin if it uses any update methods ------
                if (showDebug && type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(x => _updateMethodNames.Contains(x.Name)))
                {
                    var enabledSetting =
                        PropSettingEntry.FromNormalProperty(plugin, type.GetProperty("enabled"), pluginInfo, plugin);
                    enabledSetting.DispName = "!Allow plugin to run on every frame";
                    enabledSetting.Description =
                        "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                    enabledSetting.IsAdvanced = true;
                    detected.Add(enabledSetting);
                }

                if (detected.Any())
                {
                    var isAdvancedPlugin = type.GetCustomAttributes(typeof(AdvancedAttribute), false).Cast<AdvancedAttribute>()
                        .Any(x => x.IsAdvanced);
                    if (isAdvancedPlugin)
                        detected.ForEach(entry => entry.IsAdvanced = true);

                    results = results.Concat(detected);
                }
            }
        }
    }
}