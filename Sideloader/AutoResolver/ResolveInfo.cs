using System;
using Illusion.Extensions;
using MessagePack;

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

        public bool CanResolve(ResolveInfo other)
        {
            return GUID == other.GUID
                    && Property == other.Property
                    && Slot == other.Slot;
        }

        public static ResolveInfo Unserialize(byte[] data)
        {
            return MessagePackSerializer.Deserialize<ResolveInfo>(data);
        }
        
        public byte[] Serialize()
        {
            return MessagePackSerializer.Serialize(this);
        }

        public ResolveInfo AppendPropertyPrefix(string prefix)
        {
            var newResolveInfo = this.DeepCopy();

            newResolveInfo.Property = $"{prefix}{newResolveInfo.Property}";

            return newResolveInfo;
        }
    }
}
