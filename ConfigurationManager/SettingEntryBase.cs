// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;

namespace ConfigurationManager
{
    internal abstract class SettingEntryBase
    {
        public AcceptableValueBaseAttribute AcceptableValues { get; internal set; }

        /// <summary>
        ///     Show this setting in the settings screen at all? If false, don't show.
        /// </summary>
        public bool? Browsable { get; internal set; }

        /// <summary>
        ///     Category the setting is under. Null to be directly under the plugin.
        /// </summary>
        public string Category { get; internal set; }

        /// <summary>
        ///     If set, a "Default" button will be shown next to the setting to allow resetting to default.
        /// </summary>
        public object DefaultValue { get; internal set; }

        /// <summary>
        ///     Optional description shown when hovering over the setting
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        ///     Name of the setting
        /// </summary>
        public virtual string DispName { get; internal set; }

        /// <summary>
        ///     Plugin this setting belongs to
        /// </summary>
        public BepInPlugin PluginInfo { get; internal set; }

        /// <summary>
        ///     Only allow showing of the value. False whenever possible by default.
        /// </summary>
        public bool? ReadOnly { get; internal set; }

        /// <summary>
        ///     Type of the variable
        /// </summary>
        public abstract Type SettingType { get; }

        public object Wrapper { get; internal set; }

        public bool? IsAdvanced { get; internal set; }

        public abstract object Get();
        public abstract void Set(object newVal);

        /// <summary>
        ///     todo from property that checks canread canwrite
        ///     from method that shows a button?
        ///     change to inheritance? or isbutton and ignore set argument
        /// </summary>
        public void SetFromAttributes(MemberInfo settingProp, BepInPlugin pluginInfo)
        {
            PluginInfo = pluginInfo;

            var attribs = settingProp.GetCustomAttributes(false);

            DispName = attribs.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName;
            Category = attribs.OfType<CategoryAttribute>().FirstOrDefault()?.Category;
            Description = attribs.OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
            DefaultValue = attribs.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;
            AcceptableValues = attribs.OfType<AcceptableValueBaseAttribute>().FirstOrDefault();

            ReadOnly = attribs.OfType<ReadOnlyAttribute>().FirstOrDefault()?.IsReadOnly;
            Browsable = attribs.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable;
            IsAdvanced = attribs.OfType<AdvancedAttribute>().FirstOrDefault()?.IsAdvanced;
        }
    }
}