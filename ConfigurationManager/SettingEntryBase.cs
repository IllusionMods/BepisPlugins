// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx.Configuration;

namespace ConfigurationManager
{
    internal abstract class SettingEntryBase
    {
        public object[] AcceptableValues { get; protected set; }
        public KeyValuePair<object, object> AcceptableValueRange { get; protected set; }
        public bool? ShowRangeAsPercent { get; protected set; }
	    public CustomSettingDrawAttribute CustomDrawer { get; private set; }

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

        public BaseUnityPlugin PluginInstance { get; private set; }

        /// <summary>
        ///     Instance of the <see cref="ConfigWrapper{T}"/> that holds this setting. 
        ///     Null if setting is not in a ConfigWrapper.
        /// </summary>
        public object Wrapper { get; internal set; }

        public bool? IsAdvanced { get; internal set; }

        public abstract object Get();
        public abstract void Set(object newVal);

        public Func<object, string> ObjToStr { get; internal set; }

        public Func<string, object> StrToObj { get; internal set; }

        /// <summary>
        ///     todo from property that checks canread canwrite
        ///     from method that shows a button?
        ///     change to inheritance? or isbutton and ignore set argument
        /// </summary>
        protected void SetFromAttributes(object[] attribs, BaseUnityPlugin pluginInstance)
        {
			PluginInstance = pluginInstance;
            PluginInfo = pluginInstance?.Info.Metadata;
            
            if (attribs == null || attribs.Length == 0) return;

            DispName = attribs.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName;
            Category = attribs.OfType<CategoryAttribute>().FirstOrDefault()?.Category;
            Description = attribs.OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
            DefaultValue = attribs.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;

            var acc = attribs.OfType<AcceptableValueBaseAttribute>().FirstOrDefault();
            if (acc is AcceptableValueListAttribute accList)
                AcceptableValues = accList.GetAcceptableValues(pluginInstance);
            else if (acc is AcceptableValueRangeAttribute accRange)
            {
                AcceptableValueRange = new KeyValuePair<object, object>(accRange.MinValue, accRange.MaxValue);
                ShowRangeAsPercent = accRange.ShowAsPercentage;
            }

			CustomDrawer = attribs.OfType<CustomSettingDrawAttribute>().FirstOrDefault();

			ReadOnly = attribs.OfType<ReadOnlyAttribute>().FirstOrDefault()?.IsReadOnly;
            Browsable = attribs.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable;
            IsAdvanced = attribs.OfType<AdvancedAttribute>().FirstOrDefault()?.IsAdvanced;
        }
    }
}