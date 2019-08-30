using MessagePack;
using System;
#if AI
using AIChara;
#endif

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

        public static ResolveInfo Deserialize(byte[] data) => MessagePackSerializer.Deserialize<ResolveInfo>(data);

        public byte[] Serialize() => MessagePackSerializer.Serialize(this);
    }
}
