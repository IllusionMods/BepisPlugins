using MessagePack;
using System;

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Contains information saved to the card for resolving ID conflicts
    /// </summary>
    [Serializable]
    [MessagePackObject]
    public class StudioResolveInfo
    {
        /// <summary>
        /// GUID of the mod as defined in the manifest.xml
        /// </summary>
        [Key("ModID")]
        public string GUID { get; set; }
        /// <summary>
        /// ID of the item as defined in the mod's list files
        /// </summary>
        [Key("Slot")]
        public int Slot { get; set; }
        /// <summary>
        /// Resolved item ID. IDs greater than 100000000 are resolved IDs belonging to Sideloader. Use the resolved ID (local slot) to look up the original ID (slot)
        /// </summary>
        [Key("LocalSlot")]
        public int LocalSlot { get; set; }
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
        /// Used to determine if the item should be searched for ID lookups
        /// </summary>
        [Key("ResolveItem")]
        public bool ResolveItem { get; set; }
        /// <summary>
        /// Group of the item, used by animations
        /// </summary>
        [Key("Group")]
        public int Group { get; set; }
        /// <summary>
        /// Category of the item, used by animations
        /// </summary>
        [Key("Category")]
        public int Category { get; set; }

        internal static StudioResolveInfo Deserialize(byte[] data) => MessagePackSerializer.Deserialize<StudioResolveInfo>(data);

        internal byte[] Serialize() => MessagePackSerializer.Serialize(this);
    }
}
