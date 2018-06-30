// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System.ComponentModel;

namespace BepInEx
{
    /// <summary>
    ///     A keyboard shortcut that is saved in the config file and can be changed by the user if ConfigurationManager plugin
    ///     is present.
    ///     How to use: Run IsPressed in Update to check if user presses the button combo.
    /// </summary>
    public class SavedKeyboardShortcut : ConfigWrapper<KeyboardShortcut>
    {
        private KeyboardShortcut _last;

        public SavedKeyboardShortcut(string name, BaseUnityPlugin plugin, KeyboardShortcut defaultShortcut)
            : base(name, plugin, KeyboardShortcut.Deserialize, k => k.Serialize(), defaultShortcut)
        {
        }

        public SavedKeyboardShortcut(string name, string section, KeyboardShortcut defaultShortcut)
            : base(name, section, KeyboardShortcut.Deserialize, k => k.Serialize(), defaultShortcut)
        {
        }

        private void SetNewLast(KeyboardShortcut value)
        {
            if (_last != null)
                _last.PropertyChanged -= ShortcutChanged;

            _last = value;
            _last.PropertyChanged += ShortcutChanged;
        }

        protected override void SetValue(KeyboardShortcut value)
        {
            SetNewLast(value);
            base.SetValue(value);
        }

        protected override KeyboardShortcut GetValue()
        {
            var value = base.GetValue();
            SetNewLast(value);
            return value;
        }

        private void ShortcutChanged(object sender, PropertyChangedEventArgs e)
        {
            base.SetValue((KeyboardShortcut) sender);
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