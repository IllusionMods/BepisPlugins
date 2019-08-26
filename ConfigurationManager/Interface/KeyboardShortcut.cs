// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BepInEx
{
    /// <summary>
    ///     A keyboard shortcut that can be used in Update method to check if user presses a key combo.
    ///     Use SavedKeyboardShortcut to allow user to change this shortcut and have the changes saved.
    ///     How to use: Use IsDown instead of the Imput.GetKeyDown in the Update loop.
    /// </summary>
	[Obsolete("Use the one in BepInEx.Configuration namespace")]
	public class KeyboardShortcut
	{
        public static readonly KeyboardShortcut Empty = new KeyboardShortcut(KeyCode.None);

        public static readonly IEnumerable<KeyCode> AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        private readonly KeyCode[] _allKeys;
        private readonly HashSet<KeyCode> _allKeysLookup;

        /// <summary>
        ///     Create a new keyboard shortcut.
        /// </summary>
        /// <param name="mainKey">Main key to press</param>
        /// <param name="modifiers">Keys that should be held down before main key is registered</param>
        public KeyboardShortcut(KeyCode mainKey, params KeyCode[] modifiers) : this(new[] { mainKey }.Concat(modifiers).ToArray())
        {
        }

        private KeyboardShortcut(KeyCode[] keys)
        {
            _allKeys = SanitizeKeys(keys);
            _allKeysLookup = new HashSet<KeyCode>(_allKeys);
        }

        public KeyboardShortcut()
        {
            _allKeys = SanitizeKeys();
            _allKeysLookup = new HashSet<KeyCode>(_allKeys);
        }

        private static KeyCode[] SanitizeKeys(params KeyCode[] keys)
        {
            if (keys == null || keys.Length == 0)
                return new[] { KeyCode.None };

            return new[] { keys[0] }.Concat(keys.Skip(1).OrderBy(x => x.ToString())).ToArray();
        }

        public IEnumerable<KeyCode> AllKeys => _allKeys;

        public KeyCode MainKey => _allKeys.Length > 0 ? _allKeys[0] : KeyCode.None;

        public IEnumerable<KeyCode> Modifiers => _allKeys.Skip(1);

        public static KeyboardShortcut Deserialize(string str)
        {
            try
            {
                var parts = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (KeyCode)Enum.Parse(typeof(KeyCode), x)).ToArray();
                return new KeyboardShortcut(parts);
            }
            catch (SystemException ex)
            {
                ConfigurationManager.ConfigurationManager.Logger.Log(LogLevel.Error, "Failed to read keybind from settings: " + ex.Message);
                return new KeyboardShortcut();
            }
        }

        public string Serialize() => string.Join(" ", AllKeys.Select(x => x.ToString()).ToArray());

        /// <summary>
        ///     Check if the main key was just pressed (Input.GetKeyDown), and specified modifier keys are all pressed
        /// </summary>
        public bool IsDown()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKeyDown(MainKey) && ModifierKeyTest();
        }

        /// <summary>
        ///     Check if the main key is currently held down (Input.GetKey), and specified modifier keys are all pressed
        /// </summary>
        public bool IsPressed()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKey(MainKey) && ModifierKeyTest();
        }

        /// <summary>
        ///     Check if the main key was just lifted (Input.GetKeyUp), and specified modifier keys are all pressed.
        /// </summary>
        public bool IsUp()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKeyUp(MainKey) && ModifierKeyTest();
        }

        private bool ModifierKeyTest() => AllKeyCodes.All(c =>
                                                    {
                                                        if (_allKeysLookup.Contains(c))
                                                        {
                                                            if (_allKeys[0] == c)
                                                                return true;
                                                            return Input.GetKey(c);
                                                        }
                                                        return !Input.GetKey(c);
                                                    });

        public override string ToString()
        {
            if (MainKey == KeyCode.None) return "Not set";

            return string.Join(" + ", AllKeys.Select(c => c.ToString()).ToArray());
        }

        public override bool Equals(object obj) => obj is KeyboardShortcut shortcut && _allKeys.SequenceEqual(shortcut._allKeys);

        public override int GetHashCode()
        {
            var hc = _allKeys.Length;
            for (var i = 0; i < _allKeys.Length; i++)
                hc = unchecked(hc * 31 + (int)_allKeys[i]);
            return hc;
        }
    }
}