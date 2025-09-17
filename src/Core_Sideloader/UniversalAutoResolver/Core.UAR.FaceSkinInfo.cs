using MessagePack;

#pragma warning disable CS1591 // TODO Missing XML comment for publicly visible type or member

#if AI || HS2
namespace Sideloader.AutoResolver
{
    [MessagePackObject]
    public class FaceSkinInfo
    {
        [Key(0)] public int SkinSlot;
        [Key(1)] public string SkinGUID;
        [Key(2)] public int HeadSlot;
        [Key(3)] public string HeadGUID;
        [IgnoreMember] public int SkinLocalSlot;

        [SerializationConstructor]
        public FaceSkinInfo(int skinID, string skinGUID, int headID, string headGUID)
        {
            SkinSlot = skinID;
            SkinGUID = skinGUID;
            HeadSlot = headID;
            HeadGUID = headGUID;
        }
    }
}
#endif