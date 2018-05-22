using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ConfigurationManager
{
    [BepInPlugin(GUID: "com.bepis.bepinex.configurationmanager", Name: "Configuration Manager", Version: "1.0")]
    public partial class ConfigurationManager : BaseUnityPlugin
    {
        void Start()
        {

        }

        void Update()
        {

        }

        BaseUnityPlugin[] FindPlugins()
        {
            return FindObjectsOfType<BaseUnityPlugin>();
        }

        void BuildSettingList()
        {
            var settingType = typeof(ConfigWrapper<>);
            foreach (var plugin in FindPlugins())
            {
                var type = plugin.GetType();
                var settingProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => IsSubclassOfRawGeneric(settingType, x.PropertyType));

                var settingEntries = settingProps.Select((x) => SettingEntry.FromConfigWrapper(plugin, x)).Where(x => x.Browsable).ToList();
                
            }
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
