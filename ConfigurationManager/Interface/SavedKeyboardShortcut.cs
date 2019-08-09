// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx.Configuration;
using BepInEx.Logging;
using System;

namespace BepInEx
{
    /// <summary>
    ///     A keyboard shortcut that is saved in the config file and can be changed by the user if ConfigurationManager plugin
    ///     is present.
    ///     How to use: Run IsPressed in Update to check if user presses the button combo.
    /// </summary>
    public class SavedKeyboardShortcut
    {
        public ConfigWrapper<string> Wrapper { get; }

        private KeyboardShortcut _last;

        private static readonly string KeybindCategoryName = "Keyboard shortcuts";

        public SavedKeyboardShortcut(ConfigFile file, string key, string description, KeyboardShortcut defaultShortcut)
        {
            Wrapper = file.Wrap(new ConfigDefinition(KeybindCategoryName, key, description), defaultShortcut.Serialize());
            Wrapper.SettingChanged += ShortcutChanged;
            ShortcutChanged(null, null);
        }

        private void ShortcutChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                _last = KeyboardShortcut.Deserialize(Wrapper.Value);
            }
            catch (SystemException ex)
            {
                ConfigurationManager.ConfigurationManager.Logger.Log(LogLevel.Error, "Failed to read keybind from settings: " + ex.Message);
                _last = KeyboardShortcut.Empty;
            }
        }

        /// <summary>
        ///     Check if the main key is currently held down (Input.GetKey), and specified modifier keys are all pressed
        /// </summary>
        public bool IsPressed() => _last.IsPressed();

        /// <summary>
        ///     Check if the main key was just pressed (Input.GetKeyDown), and specified modifier keys are all pressed
        /// </summary>
        public bool IsDown() => _last.IsDown();

        /// <summary>
        ///     Check if the main key was just lifted (Input.GetKeyUp), and specified modifier keys are all pressed.
        /// </summary>
        public bool IsUp() => _last.IsUp();
    }
}