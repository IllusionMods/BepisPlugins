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
    /// A keyboard shortcut that can be used in Update method to check if user presses a key combo.
    /// Use SavedKeyboardShortcut to allow user to change this shortcut and have the changes saved.
    ///
    /// How to use: Use IsDown instead of the Imput.GetKeyDown in the Update loop.
    /// </summary>
    public class KeyboardShortcut : INotifyPropertyChanged
    {
        public static readonly KeyboardShortcut Empty = new KeyboardShortcut(KeyCode.None);

        public static readonly IEnumerable<KeyCode> AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        private KeyCode[] allKeys;

        /// <summary>
        /// Create a new keyboard shortcut.
        /// </summary>
        /// <param name="mainKey">Main key to press</param>
        /// <param name="modifiers">Keys that should be held down before main key is registered</param>
        public KeyboardShortcut(KeyCode mainKey, params KeyCode[] modifiers)
        {
            allKeys = new[] { mainKey }.Concat(modifiers).ToArray();
        }

        private KeyboardShortcut(params KeyCode[] keys)
        {
            allKeys = keys;
        }

        public KeyboardShortcut()
        {
            allKeys = new KeyCode[] { };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public KeyCode MainKey
        {
            get
            {
                return allKeys.Length > 0 ? allKeys[0] : KeyCode.None;
            }

            set
            {
                if (MainKey == value) return;

                if (allKeys.Length > 0)
                    allKeys[0] = value;
                else
                    allKeys = new[] { value };

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainKey)));
            }
        }

        public IEnumerable<KeyCode> Modifiers
        {
            get
            {
                return allKeys.Skip(1);
            }

            set
            {
                if (allKeys.Length > 0)
                    allKeys = new[] { allKeys[0] }.Concat(value).ToArray();
                else
                    allKeys = value.ToArray();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Modifiers)));
            }
        }

        public static KeyboardShortcut Deserialize(string str)
        {
            try
            {
                var parts = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => (KeyCode)Enum.Parse(typeof(KeyCode), x)).ToArray();
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
            return string.Join(" ", allKeys.Select(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Check if the main key was just pressed (Input.GetKeyDown), and specified modifier keys are all pressed
        /// </summary>
        public bool IsDown()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKeyDown(MainKey) && ModifierKeyTest();
        }

        /// <summary>
        /// Check if the main key is currently held down (Input.GetKey), and specified modifier keys are all pressed
        /// </summary>
        public bool IsPressed()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKey(MainKey) && ModifierKeyTest();
        }

        /// <summary>
        /// Check if the main key was just lifted (Input.GetKeyUp), and specified modifier keys are all pressed.
        /// </summary>
        public bool IsUp()
        {
            if (MainKey == KeyCode.None) return false;

            return Input.GetKeyUp(MainKey) && ModifierKeyTest();
        }

        private bool ModifierKeyTest()
        {
            return AllKeys.All(c =>
            {
                if (allKeys.Contains(c))
                {
                    if (allKeys[0] == c)
                        return true;
                    return Input.GetKey(c);
                }
                else
                {
                    return !Input.GetKey(c);
                }
            });
        }

        public override string ToString()
        {
            if (MainKey == KeyCode.None) return "Not set";

            return string.Join(" + ", allKeys.Select(c => c.ToString()).ToArray());
        }
    }
}