using MessagePack;
using System;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Contains information saved to the card for resolving ID conflicts
    /// </summary>
    [Serializable]
    [MessagePackObject]
    public class ResolveInfo
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
        /// Property of the object as defined in Sideloader's StructReference.
        /// If ever you need to know what to use for this, enable debug resolve info logging and see what Sideloader generates at the start of the game.
        /// </summary>
        [Key("Property")]
        public string Property { get; set; }
        /// <summary>
        /// ChaListDefine.CategoryNo. Typically only used for hard mod resolving in cases where the GUID is not known.
        /// </summary>
        [Key("CategoryNo")]
        public ChaListDefine.CategoryNo CategoryNo { get; set; }

        internal static ResolveInfo Deserialize(byte[] data) => MessagePackSerializer.Deserialize<ResolveInfo>(data);

        internal byte[] Serialize() => MessagePackSerializer.Serialize(this);
    }
}
