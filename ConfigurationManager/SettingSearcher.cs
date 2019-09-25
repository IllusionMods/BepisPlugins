using System;
using BepInEx;
using BepInEx.Configuration;
using ConfigurationManager.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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

        private static readonly Type _bepin4BaseSettingType = Type.GetType("BepInEx4.ConfigWrapper`1, BepInEx.BepIn4Patcher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", false);

        public static void CollectSettings(out IEnumerable<SettingEntryBase> results, out List<string> modsWithoutSettings, bool showDebug)
        {
            modsWithoutSettings = new List<string>();

            try
            {
                results = GetBepInExCoreConfig();
            }
            catch (Exception ex)
            {
                results = Enumerable.Empty<SettingEntryBase>();
                ConfigurationManager.Logger.LogError(ex);
            }

            foreach (var plugin in Utils.FindPlugins())
            {
                var type = plugin.GetType();

                var pluginInfo = plugin.Info.Metadata;

                if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .Any(x => !x.Browsable))
                {
                    modsWithoutSettings.Add(pluginInfo.Name);
                    continue;
                }

                var detected = new List<SettingEntryBase>();

                detected.AddRange(GetPluginConfig(plugin).Cast<SettingEntryBase>());

                detected.AddRange(GetLegacyPluginConfig(plugin).Cast<SettingEntryBase>());

                detected.RemoveAll(x => x.Browsable == false);

                if (!detected.Any())
                    modsWithoutSettings.Add(pluginInfo.Name);

                // Allow to enable/disable plugin if it uses any update methods ------
                if (showDebug && type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(x => _updateMethodNames.Contains(x.Name)))
                {
                    // todo make a different class for it and fix access modifiers?
                    var enabledSetting = LegacySettingEntry.FromNormalProperty(plugin, type.GetProperty("enabled"), pluginInfo, plugin);
                    enabledSetting.DispName = "!Allow plugin to run on every frame";
                    enabledSetting.Description = "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                    enabledSetting.IsAdvanced = true;
                    detected.Add(enabledSetting);
                }

                if (detected.Any())
                {
                    var isAdvancedPlugin = type.GetCustomAttributes(typeof(AdvancedAttribute), false).Cast<AdvancedAttribute>().Any(x => x.IsAdvanced);
                    if (isAdvancedPlugin)
                        detected.ForEach(entry => entry.IsAdvanced = true);

                    results = results.Concat(detected);
                }
            }
        }

        /// <summary>
        /// Bepinex 5 config
        /// </summary>
        private static IEnumerable<SettingEntryBase> GetBepInExCoreConfig()
        {
            var coreConfigProp = typeof(ConfigFile).GetProperty("CoreConfig", BindingFlags.Static | BindingFlags.NonPublic);
            if (coreConfigProp == null) throw new ArgumentNullException(nameof(coreConfigProp));

            var coreConfig = (ConfigFile)coreConfigProp.GetValue(null, null);
            var bepinMeta = new BepInPlugin("BepInEx", "BepInEx", typeof(BepInEx.Bootstrap.Chainloader).Assembly.GetName().Version.ToString());

            return coreConfig.GetConfigEntries()
                .Select(x => new ConfigSettingEntry(x, null) { IsAdvanced = true, PluginInfo = bepinMeta })
                .Cast<SettingEntryBase>();
        }

        /// <summary>
        /// Used by bepinex 5 plugins
        /// </summary>
        private static IEnumerable<ConfigSettingEntry> GetPluginConfig(BaseUnityPlugin plugin)
        {
            return plugin.Config.GetConfigEntries().Select(x => new ConfigSettingEntry(x, plugin));
        }

        /// <summary>
        /// Used by bepinex 4 plugins
        /// </summary>
        private static IEnumerable<LegacySettingEntry> GetLegacyPluginConfig(BaseUnityPlugin plugin)
        {
            if (_bepin4BaseSettingType == null)
                return Enumerable.Empty<LegacySettingEntry>();

            var type = plugin.GetType();
            var pluginInfo = plugin.Info.Metadata;

            // Config wrappers ------

            var settingProps = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FilterBrowsable(true, true);

            var settingFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => !f.IsSpecialName)
                .FilterBrowsable(true, true)
                .Select(f => new FieldToPropertyInfoWrapper(f));

            var settingEntries = settingProps.Concat(settingFields.Cast<PropertyInfo>())
                .Where(x => x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            var results = settingEntries.Select(x => LegacySettingEntry.FromConfigWrapper(plugin, x, pluginInfo, plugin)).Where(x => x != null);

            // Config wrappers static ------

            var settingStaticProps = type
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FilterBrowsable(true, true);

            var settingStaticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => !f.IsSpecialName)
                .FilterBrowsable(true, true)
                .Select(f => new FieldToPropertyInfoWrapper(f));

            var settingStaticEntries = settingStaticProps.Concat(settingStaticFields.Cast<PropertyInfo>())
                .Where(x => x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            results = results.Concat(settingStaticEntries.Select(x => LegacySettingEntry.FromConfigWrapper(null, x, pluginInfo, plugin)).Where(x => x != null));

            // Normal properties ------

            bool IsPropSafeToShow(PropertyInfo p) => p.GetSetMethod()?.IsPublic == true && (p.PropertyType.IsValueType || p.PropertyType == typeof(string));

            var normalPropsSafeToShow = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsPropSafeToShow)
                .FilterBrowsable(true, true)
                .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            var normalPropsWithBrowsable = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FilterBrowsable(true, false)
                .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            var normalProps = normalPropsSafeToShow.Concat(normalPropsWithBrowsable).Distinct();

            results = results.Concat(normalProps.Select(x => LegacySettingEntry.FromNormalProperty(plugin, x, pluginInfo, plugin)).Where(x => x != null));

            // Normal static properties ------

            var normalStaticPropsSafeToShow = type
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsPropSafeToShow)
                .FilterBrowsable(true, true)
                .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            var normalStaticPropsWithBrowsable = type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .FilterBrowsable(true, false)
                .Where(x => !x.PropertyType.IsSubclassOfRawGeneric(_bepin4BaseSettingType));

            var normalStaticProps = normalStaticPropsSafeToShow.Concat(normalStaticPropsWithBrowsable).Distinct();

            results = results.Concat(normalStaticProps.Select(x => LegacySettingEntry.FromNormalProperty(null, x, pluginInfo, plugin)).Where(x => x != null));

            return results;
        }
    }
}