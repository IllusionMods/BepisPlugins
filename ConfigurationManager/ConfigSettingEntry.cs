using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace ConfigurationManager
{
	internal sealed class ConfigSettingEntry : SettingEntryBase
	{
		private readonly ConfigEntryBase _entry;

		public ConfigSettingEntry(ConfigEntryBase entry, BaseUnityPlugin owner)
		{
			_entry = entry;

			SetFromAttributes(entry.Description?.Tags, owner);

		    DispName = entry.Definition.Key;
		    Category = entry.Definition.Section;
		    Description = entry.Description?.Description;

            var converter = TomlTypeConverter.GetConverter(entry.SettingType);
			if (converter != null)
			{
				ObjToStr = o => converter.ConvertToString(o, entry.SettingType);
				StrToObj = s => converter.ConvertToObject(s, entry.SettingType);
			}

			var values = entry.Description?.AcceptableValues;
			if (values != null)
				GetAcceptableValues(values);

		    DefaultValue = entry.DefaultValue;
		}

		private void GetAcceptableValues(AcceptableValueBase values)
		{
			var t = values.GetType();
			var listProp = t.GetProperty(nameof(AcceptableValueList<bool>.AcceptableValues), BindingFlags.Instance | BindingFlags.Public);
			if (listProp != null)
			{
				AcceptableValues = ((IEnumerable)listProp.GetValue(values, null)).Cast<object>().ToArray();
			}
			else
			{
				var minProp = t.GetProperty(nameof(AcceptableValueRange<bool>.MinValue), BindingFlags.Instance | BindingFlags.Public);
				if (minProp != null)
				{
					var maxProp = t.GetProperty(nameof(AcceptableValueRange<bool>.MaxValue), BindingFlags.Instance | BindingFlags.Public);
					if (maxProp == null) throw new ArgumentNullException(nameof(maxProp));
					AcceptableValueRange = new KeyValuePair<object, object>(minProp.GetValue(values, null), maxProp.GetValue(values, null));
					ShowRangeAsPercent = AcceptableValueRange.Key.Equals(0) && AcceptableValueRange.Value.Equals(100) ||
										 AcceptableValueRange.Key.Equals(0f) && AcceptableValueRange.Value.Equals(1f);
				}
			}
		}

		public override Type SettingType => _entry.SettingType;

		public override object Get()
		{
			return _entry.BoxedValue;
		}

		public override void Set(object newVal)
		{
			_entry.BoxedValue = newVal;
		}
	}
}