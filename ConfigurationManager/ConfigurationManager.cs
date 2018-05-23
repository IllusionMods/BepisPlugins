using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConfigurationManager
{
    [BepInPlugin(GUID: "com.bepis.bepinex.configurationmanager", Name: "Configuration Manager", Version: "1.0")]
    public class ConfigurationManager : BaseUnityPlugin
    {
        private readonly Type baseSettingType = typeof(ConfigWrapper<>);

        private List<SettingEntry> BuildSettingList()
        {
            var list = new List<SettingEntry>();

            foreach (var plugin in FindPlugins())
            {
                var type = plugin.GetType();

                var pluginInfo = type.GetCustomAttributes(typeof(BepInPlugin), false).Cast<BepInPlugin>().FirstOrDefault();
                if (pluginInfo == null)
                {
                    BepInLogger.Log($"Error: Plugin {type.FullName} is missing the BepInPlugin attribute!");
                    continue;
                }

                var settingProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                list.AddRange(settingProps.Select((x) => SettingEntry.FromConfigWrapper(plugin, x, pluginInfo)).Where(x => x.Browsable));

                var settingPropsStatic = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.PropertyType.IsSubclassOfRawGeneric(baseSettingType));
                list.AddRange(settingPropsStatic.Select((x) => SettingEntry.FromConfigWrapper(null, x, pluginInfo)).Where(x => x.Browsable));

                //TODO scan normal properties too
            }

            return list;
        }

        private BaseUnityPlugin[] FindPlugins()
        {
            return FindObjectsOfType<BaseUnityPlugin>();
        }

        private void Start()
        {
        }

        private void Update()
        {
        }
    }
}