namespace BepInEx
{
    /// <summary>
    /// A keyboard shortcut that is saved in the config file and can be changed by the user.
    /// How to use: Run IsPressed in Update to check if user presses the button combo.
    /// </summary>
    public class SavedKeyboardShortcut : ConfigWrapper<KeyboardShortcut>
    {
        public SavedKeyboardShortcut(string name, string section, KeyboardShortcut defaultShortcut)
            : base(name, section, KeyboardShortcut.Deserialize, k => k.Serialize(), defaultShortcut)
        {

        }

        public bool IsPressed()
        {
            return Value.IsPressed();
        }
    }
}