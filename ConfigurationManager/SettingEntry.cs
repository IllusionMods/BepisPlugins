// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;

namespace ConfigurationManager
{
    internal class PropSettingEntry : SettingEntryBase
    {
        private Type settingType;

        private PropSettingEntry()
        {
        }

        public override string DispName
        {
            get => string.IsNullOrEmpty(base.DispName) ? Property.Name : base.DispName;
            internal set => base.DispName = value;
        }

        public object Instance { get; internal set; }
        public PropertyInfo Property { get; internal set; }

        public override Type SettingType => settingType ?? (settingType = Property.PropertyType);

        public override object Get()
        {
            return Property.GetValue(Instance, null);
        }

        public override void Set(object newVal)
        {
            Property.SetValue(Instance, newVal, null);
        }

        public static PropSettingEntry FromConfigWrapper(object instance, PropertyInfo settingProp,
            BepInPlugin pluginInfo)
        {
            try
            {
                var wrapper = settingProp.GetValue(instance, null);

                var innerProp = wrapper.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

                var entry = new PropSettingEntry();
                entry.SetFromAttributes(settingProp, pluginInfo);

                if (innerProp == null)
                {
                    Logger.Log(LogLevel.Error, "Failed to find property Value of ConfigWrapper");
                    return null;
                }

                entry.Browsable = innerProp.CanRead && innerProp.CanWrite && entry.Browsable != false;

                entry.Property = innerProp;
                entry.Instance = wrapper;

                entry.Wrapper = wrapper;

                if (entry.DispName == "Value")
                    entry.DispName = wrapper.GetType().GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(wrapper, null) as string;

                if (string.IsNullOrEmpty(entry.Category))
                {
                    var section = wrapper.GetType().GetProperty("Section", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(wrapper, null) as string;
                    if (section != pluginInfo?.GUID)
                        entry.Category = section;
                }

                return entry;
            }
            catch (SystemException ex)
            {
                Logger.Log(LogLevel.Error,
                    "Failed to create ConfigWrapper entry : " + instance?.ToString() + " | " + settingProp?.Name +
                    " | " + pluginInfo?.Name);
                return null;
            }
        }

        public static PropSettingEntry FromNormalProperty(object instance, PropertyInfo settingProp,
            BepInPlugin pluginInfo)
        {
            var entry = new PropSettingEntry();
            entry.SetFromAttributes(settingProp, pluginInfo);

            entry.Browsable = settingProp.CanRead && settingProp.CanWrite && entry.Browsable != false;

            entry.Property = settingProp;
            entry.Instance = instance;

            return entry;
        }
    }
}