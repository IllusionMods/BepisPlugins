using Illusion.Extensions;
using MessagePack;
using System;

namespace Sideloader.AutoResolver
{
    [Serializable]
    [MessagePackObject]
    public class ResolveInfo
    {
        [Key("ModID")]
        public string GUID { get; set; }
        [Key("Slot")]
        public int Slot { get; set; }
        [Key("LocalSlot")]
        public int LocalSlot { get; set; }
        [Key("Property")]
        public string Property { get; set; }
        [Key("CategoryNo")]
        public ChaListDefine.CategoryNo CategoryNo { get; set; }

        public bool CanResolve(ResolveInfo other) => GUID == other.GUID && Property == other.Property && Slot == other.Slot;

        public static ResolveInfo Unserialize(byte[] data) => MessagePackSerializer.Deserialize<ResolveInfo>(data);

        public byte[] Serialize() => MessagePackSerializer.Serialize(this);

        public ResolveInfo AppendPropertyPrefix(string prefix)
        {
            var newResolveInfo = this.DeepCopy();

            newResolveInfo.Property = $"{prefix}{newResolveInfo.Property}";

            return newResolveInfo;
        }
    }

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

        public bool CanResolve(ResolveInfo other) => GUID == other.GUID && Slot == other.Slot;

        public static StudioResolveInfo Unserialize(byte[] data) => MessagePackSerializer.Deserialize<StudioResolveInfo>(data);

        public byte[] Serialize() => MessagePackSerializer.Serialize(this);
    }
}
