using MessagePack;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    /// <summary>
    /// An object containing data saved to and loaded from cards.
    /// </summary>
    [MessagePackObject]
    public class PluginData
    {
        /// <summary>
        /// Version of the plugin data saved to the card. Get or set this if ever your plugin data format changes and use it to differentiate.
        /// </summary>
        [Key(0)]
        public int version;
        /// <summary>
        /// Dictionary of objects saved to or loaded loaded from the card.
        /// </summary>
        [Key(1)]
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }
}