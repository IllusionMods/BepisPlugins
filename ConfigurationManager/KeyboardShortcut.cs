using System;
using System.Linq;
using UnityEngine;

namespace BepInEx
{
    /// <summary>
    /// A keyboard shortcut that can be used in Update method to check if user presses a key combo.
    /// Use SavedKeyboardShortcut to allow user to change this shortcut and have the changes saved.
    /// 
    /// How to use: Run IsPressed in Update to check if user presses the button combo.
    /// </summary>
    public class KeyboardShortcut
    {
        /// <summary>
        /// Create a new keyboard shortcut.
        /// </summary>
        /// <param name="key">Main key to press</param>
        /// <param name="control">Should Control be held down?</param>
        /// <param name="alt">Should Alt be held down?</param>
        /// <param name="shift">Should Shift be held down?</param>
        public KeyboardShortcut(KeyCode key, bool control = false, bool alt = false, bool shift = false)
        {
            Key = key;
            Control = control;
            Alt = alt;
            Shift = shift;
        }

        public KeyboardShortcut()
        {

        }

        public KeyCode Key { get; set; }
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        public string Serialize()
        {
            return $"{(int)Key} {(Control ? 1 : 0)} {(Alt ? 1 : 0)} {(Shift ? 1 : 0)}";
        }

        public static KeyboardShortcut Deserialize(string str)
        {
            try
            {
                var parts = str.Split(' ').Select(x => int.Parse(x)).ToArray();
                return new KeyboardShortcut((KeyCode)parts[0], parts[1] == 1, parts[2] == 1, parts[3] == 1);
            }
            catch (SystemException ex)
            {
                BepInLogger.Log("Failed to read keybind from settings: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Run in Update to check if user presses the button combo instead of manually using Input.GetKey.
        /// </summary>
        public bool IsPressed()
        {
            if (!Input.GetKey(Key))
                return false;

            if (Control && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                return false;

            if (Alt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
                return false;

            if (Shift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                return false;

            if (Key == KeyCode.None && !Control && !Alt && !Shift)
                return false;

            return true;
        }
    }
}