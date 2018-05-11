using System;
using Illusion.Extensions;
using MessagePack;

namespace Sideloader.AutoResolver
{
    [Serializable]
    [MessagePackObject(true)]
    public class ResolveInfo
    {
        public string ModID { get; set; }
        public int Slot { get; set; }
        public int LocalSlot { get; set; }
        public string Property { get; set; }

        public bool CanResolve(ResolveInfo other)
        {
            return ModID == other.ModID
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
