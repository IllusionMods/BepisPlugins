using MessagePack;
using System;

namespace Sideloader.AutoResolver
{
    [Serializable]
    [MessagePackObject]
    public class StudioResolveInfo
    {
        [Key("ModID")]
        public string GUID { get; set; }
        [Key("Slot")]
        public int Slot { get; set; }
        [Key("LocalSlot")]
        public int LocalSlot { get; set; }
        [Key("SceneDicKey")] //Used on scene load
        public int DicKey { get; set; }
        [Key("SceneObjectOrder")] //Used on scene import
        public int ObjectOrder { get; set; }
        [Key("ResolveItem")] //Used to determine if the item should be searched for ID lookups
        public bool ResolveItem { get; set; }

        public static StudioResolveInfo Deserialize(byte[] data) => MessagePackSerializer.Deserialize<StudioResolveInfo>(data);

        public byte[] Serialize() => MessagePackSerializer.Serialize(this);
    }
}
