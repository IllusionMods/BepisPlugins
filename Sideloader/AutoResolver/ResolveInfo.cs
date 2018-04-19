using MessagePack;

namespace Sideloader.AutoResolver
{
    [MessagePackObject(true)]
    public class ResolveInfo
    {
        public string ModID { get; set; }
        public int Slot { get; set; }
        public int LocalSlot { get; set; }
        public string Property { get; set; }

        public bool CanResolve(ResolveInfo other)
        {
            return ModID == other.ModID &&
                    Property == other.Property;
        }

        public static ResolveInfo Unserialize(byte[] data)
        {
            return MessagePackSerializer.Deserialize<ResolveInfo>(data);
        }
        
        public byte[] Serialize()
        {
            return MessagePackSerializer.Serialize(this);
        }
    }
}
