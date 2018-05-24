﻿using System;
namespace BepInEx
{
    /// <summary>
    /// Specify the range of acceptable values for this variable. It will allow the configuration window to show a slider and filter inputs.
    /// </summary>
    public sealed class AcceptableValueRangeAttribute : AcceptableValueBaseAttribute
    {
        public AcceptableValueRangeAttribute(object minValue, object maxValue)
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
        }

        public object MinValue { get; }
        public object MaxValue { get; }
    }
}