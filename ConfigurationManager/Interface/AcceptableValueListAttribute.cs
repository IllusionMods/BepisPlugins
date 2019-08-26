// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Reflection;
using BepInEx.Logging;

namespace BepInEx
{
    /// <summary>
    ///     Specify the list of acceptable values for this variable. It will allow the configuration window to show a list of available values.
    /// </summary>
	[Obsolete("Use the one in BepInEx.Configuration namespace")]
    public sealed class AcceptableValueListAttribute : AcceptableValueBaseAttribute
	{
        private readonly string _acceptableValueGetterName;
        private readonly object[] _acceptableValues;

        /// <summary>
        /// Specify the list of acceptable values for this variable. It will allow the configuration window to show a list of available values.
        /// </summary>
        /// <param name="acceptableValues">List of acceptable values for this setting</param>
        public AcceptableValueListAttribute(object[] acceptableValues) => _acceptableValues = acceptableValues ?? throw new ArgumentNullException(nameof(acceptableValues));

        /// <summary>
        /// Specify a method that returns the list of acceptable values for this variable. It will allow the configuration window to show a list of available values.
        /// </summary>
        /// <param name="acceptableValueGetterName">Name of an instance method that takes no arguments and returns array object[] that contains the acceptable values</param>
        public AcceptableValueListAttribute(string acceptableValueGetterName) => _acceptableValueGetterName = acceptableValueGetterName ?? throw new ArgumentNullException(nameof(acceptableValueGetterName));

        internal object[] GetAcceptableValues(object instance)
        {
            if (_acceptableValues != null) return _acceptableValues;

            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var getter = type.GetMethod(_acceptableValueGetterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getter == null)
            {
                ConfigurationManager.ConfigurationManager.Logger.Log(LogLevel.Error, $"Failed to find instance method {_acceptableValueGetterName} in type {type.FullName}. Check your AcceptableValueList arguments!");
                return null;
            }

            try
            {
                var result = (object[])getter.Invoke(instance, null);
                return result;
            }
            catch (Exception ex)
            {
                ConfigurationManager.ConfigurationManager.Logger.Log(LogLevel.Error, $"Failed to get calues from method {_acceptableValueGetterName} in type {type.FullName}. The method has to take no arguments and return object[]!");
                ConfigurationManager.ConfigurationManager.Logger.Log(LogLevel.Error, ex);
                return null;
            }
        }
    }
}