// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;

namespace BepInEx
{
    /// <summary>
    ///     Specify the range of acceptable values for this variable. It will allow the configuration window to show a slider
    ///     and filter inputs.
    /// </summary>
	[Obsolete("Use the one in BepInEx.Configuration namespace")]
    public sealed class AcceptableValueRangeAttribute : AcceptableValueBaseAttribute
	{
        /// <param name="minValue">Lowest acceptable value</param>
        /// <param name="maxValue">Highest acceptable value</param>
        /// <param name="showAsPercentage">
        ///     Show the current value as % between min and max values if possible. Otherwise show the
        ///     value itself.
        /// </param>
        public AcceptableValueRangeAttribute(object minValue, object maxValue, bool showAsPercentage = true)
        {
            if (maxValue == null)
                throw new ArgumentNullException(nameof(maxValue));
            if (minValue == null)
                throw new ArgumentNullException(nameof(minValue));

            if (!(minValue is IComparable))
                throw new ArgumentException("Value has to implement IComparable", nameof(minValue));
            if (!(maxValue is IComparable))
                throw new ArgumentException("Value has to implement IComparable", nameof(maxValue));

            MinValue = minValue;
            MaxValue = maxValue;
            ShowAsPercentage = showAsPercentage;
        }

        public object MinValue { get; }
        public object MaxValue { get; }
        public bool ShowAsPercentage { get; }
    }
}