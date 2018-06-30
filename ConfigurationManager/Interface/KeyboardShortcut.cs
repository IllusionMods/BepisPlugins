// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace BepInEx
{
    /// <summary>
    ///     A keyboard shortcut that can be used in Update method to check if user presses a key combo.
    ///     Use SavedKeyboardShortcut to allow user to change this shortcut and have the changes saved.
    ///     How to use: Use IsDown instead of the Imput.GetKeyDown in the Update loop.
    /// </summary>
    public class KeyboardShortcut : INotifyPropertyChanged
    {
        public static readonly KeyboardShortcut Empty = new KeyboardShortcut(KeyCode.None);

        public static readonly IEnumerable<KeyCode> AllKeyCodes = (KeyCode[]) Enum.GetValues(typeof(KeyCode));

        private KeyCode[] allKeys;
        private HashSet<KeyCode> allKeysLookup;

        /// <summary>
        ///     Create a new keyboard shortcut.
        /// </summary>
        /// <param name="mainKey">Main key to press</param>
        /// <param name="modifiers">Keys that should be held down before main key is registered</param>
        public KeyboardShortcut(KeyCode mainKey, params KeyCode[] modifiers)
        {
            AllKeys = new[] {mainKey}.Concat(modifiers).ToArray();
        }

        private KeyboardShortcut(params KeyCode[] keys)
        {
            AllKeys = keys;
        }

        public KeyboardShortcut()
        {
            AllKeys = new KeyCode[] { };
        }

        public KeyCode[] AllKeys
        {
            get => allKeys;
            set
            {
                allKeys = value;
                allKeysLookup = new HashSet<KeyCode>(value);
            }
        }

        public KeyCode MainKey
        {
            get => AllKeys.Length > 0 ? AllKeys[0] : KeyCode.None;

            set
            {
                if (MainKey == value) return;

                if (AllKeys.Length > 0)
                    AllKeys[0] = value;
                else
                    AllKeys = new[] {value};

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainKey)));
            }
        }

        public IEnumerable<KeyCode> Modifiers
        {
            get => AllKeys.Skip(1);

            set
            {
                if (AllKeys.Length > 0)
                    AllKeys = new[] {AllKeys[0]}.Concat(value).ToArray();
                else
                    AllKeys = value.ToArray();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Modifiers)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static KeyboardShortcut Deserialize(string str)
        {
            try
            {
                var parts = str.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (KeyCode) Enum.Parse(typeof(KeyCode), x)).ToArray();
                return new KeyboardShortcut(parts);
            }
            catch (SystemException ex)
            {
                BepInLogger.Log("Failed to read keybind from settings: " + ex.Message);
                return null;
            }
        }

        public string Serialize()
        {
            return string.Join(" ", AllKeys.Select(x => x.ToString()).ToArray());
        }

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

        private bool ModifierKeyTest()
        {
            return AllKeyCodes.All(c =>
            {
                if (allKeysLookup.Contains(c))
                {
                    if (AllKeys[0] == c)
                        return true;
                    return Input.GetKey(c);
                }
                return !Input.GetKey(c);
            });
        }

        public override string ToString()
        {
            if (MainKey == KeyCode.None) return "Not set";

            return string.Join(" + ", AllKeys.Select(c => c.ToString()).ToArray());
        }
    }
}