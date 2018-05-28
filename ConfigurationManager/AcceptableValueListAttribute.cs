// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
namespace BepInEx
{
    /// <summary>
    /// Specify the list of acceptable values for this variable. It will allow the configuration window to show a list of available values.
    /// </summary>
    public sealed class AcceptableValueListAttribute : AcceptableValueBaseAttribute
    {
        public AcceptableValueListAttribute(object[] acceptableValues)
        {
            if (acceptableValues == null)
                throw new ArgumentNullException(nameof(acceptableValues));

            AcceptableValues = acceptableValues;
        }

        public object[] AcceptableValues { get; }
    }
}
