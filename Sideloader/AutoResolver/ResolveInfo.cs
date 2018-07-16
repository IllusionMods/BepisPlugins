using System;
using Illusion.Extensions;
using MessagePack;

namespace Sideloader.AutoResolver
{
    [Serializable]
    [MessagePackObject(true)]
    public class ResolveInfo
    {
        [Key("ModID")]
        public string m_GUID { get; set; }

        [Key("GUID")]
        public string m_GUID2 { get; set; }

        public string GUID
        {
            get => $"{m_GUID}{m_GUID2}";
            set
            {
                m_GUID2 = "";
                m_GUID = value;
            }
        }

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
