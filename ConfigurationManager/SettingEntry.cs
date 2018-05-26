using BepInEx;
using System;
using System.Reflection;

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
            get
            {
                return string.IsNullOrEmpty(base.DispName) ? Property.Name : base.DispName;
            }
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

        public static PropSettingEntry FromConfigWrapper(object instance, PropertyInfo settingProp, BepInPlugin pluginInfo)
        {
            var wrapper = settingProp.GetValue(instance, null);

            var innerProp = wrapper.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            
            PropSettingEntry entry = new PropSettingEntry();
            entry.SetFromAttributes(settingProp, pluginInfo);

            entry.Browsable = innerProp.CanRead && innerProp.CanWrite && entry.Browsable != false;

            entry.Property = innerProp;
            entry.Instance = wrapper;
            
            if (entry.DispName == "Value")
                entry.DispName = wrapper.GetType().GetProperty("Key", BindingFlags.Instance | BindingFlags.Public).GetValue(wrapper, null) as string;

            if (string.IsNullOrEmpty(entry.Category))
            {
                var section = wrapper.GetType().GetProperty("Section", BindingFlags.Instance | BindingFlags.Public).GetValue(wrapper, null) as string;
                if (section != pluginInfo.GUID)
                    entry.Category = section;
            }

            return entry;
        }

        public static PropSettingEntry FromNormalProperty(object instance, PropertyInfo settingProp, BepInPlugin pluginInfo)
        {
            PropSettingEntry entry = new PropSettingEntry();
            entry.SetFromAttributes(settingProp, pluginInfo);

            entry.Browsable = settingProp.CanRead && settingProp.CanWrite && entry.Browsable != false;

            entry.Property = settingProp;
            entry.Instance = instance;

            return entry;
        }
    }
}