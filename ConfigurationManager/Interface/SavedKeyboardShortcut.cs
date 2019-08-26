// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.ComponentModel;
using BepInEx4;

namespace BepInEx
{
    /// <summary>
    ///     A keyboard shortcut that is saved in the config file and can be changed by the user if ConfigurationManager plugin
    ///     is present.
    ///     How to use: Run IsPressed in Update to check if user presses the button combo.
    /// </summary>
	[Obsolete("Use the one in BepInEx.Configuration namespace")]
    public class SavedKeyboardShortcut : ConfigWrapper<KeyboardShortcut>
    {
        public SavedKeyboardShortcut(string name, BaseUnityPlugin plugin, KeyboardShortcut defaultShortcut)
            : base(name, plugin, KeyboardShortcut.Deserialize, k => k.Serialize(), defaultShortcut ?? new KeyboardShortcut())
        {
        }

        public SavedKeyboardShortcut(string name, string section, KeyboardShortcut defaultShortcut)
            : base(name, section, KeyboardShortcut.Deserialize, k => k.Serialize(), defaultShortcut ?? new KeyboardShortcut())
        {
        }

        private void ShortcutChanged(object sender, PropertyChangedEventArgs e)
        {
            base.SetValue((KeyboardShortcut)sender);
        }

        /// <summary>
        ///     Check if the main key is currently held down (Input.GetKey), and specified modifier keys are all pressed
        /// </summary>
        public bool IsPressed()
        {
            return Value.IsPressed();
        }

        /// <summary>
        ///     Check if the main key was just pressed (Input.GetKeyDown), and specified modifier keys are all pressed
        /// </summary>
        public bool IsDown()
        {
            return Value.IsDown();
        }

        /// <summary>
        ///     Check if the main key was just lifted (Input.GetKeyUp), and specified modifier keys are all pressed.
        /// </summary>
        public bool IsUp()
        {
            return Value.IsUp();
        }
    }
}