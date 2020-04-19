using MessagePack;
using System;
using System.Collections.Generic;

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Contains information saved to the card for resolving ID conflicts
    /// </summary>
    [Serializable]
    [MessagePackObject]
    public class StudioPatternResolveInfo
    {
        /// <summary>
        /// Dictionary key of the item, used on scene load
        /// </summary>
        [Key("SceneDicKey")]
        public int DicKey { get; set; }
        /// <summary>
        /// Order of the item saved to the scene, used on scene import
        /// </summary>
        [Key("SceneObjectOrder")]
        public int ObjectOrder { get; set; }
        /// <summary>
        /// Information about the patterns saved to the item
        /// </summary>
        [Key("ObjectPatternInfo")]
        public Dictionary<int, PatternInfo> ObjectPatternInfo { get; set; }

        internal static StudioPatternResolveInfo Deserialize(byte[] data) => MessagePackSerializer.Deserialize<StudioPatternResolveInfo>(data);

        internal byte[] Serialize() => MessagePackSerializer.Serialize(this);

        /// <summary>
        /// Information about the patterns
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class PatternInfo
        {
            /// <summary>
            /// GUID of the mod as defined in the manifest.xml
            /// </summary>
            [Key("GUID")]
            public string GUID;
            /// <summary>
            /// ID of the item as defined in the mod's list files
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Resolved item ID. IDs greater than 100000000 are resolved IDs belonging to Sideloader. Use the resolved ID (local slot) to look up the original ID (slot)
            /// </summary>
            [Key("LocalSlot")]
            public int LocalSlot;
        }
    }
}
