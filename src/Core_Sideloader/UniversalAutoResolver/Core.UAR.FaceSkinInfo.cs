using MessagePack;

#if AI || HS2
namespace Sideloader.AutoResolver
{
    [MessagePackObject]
    public class FaceSkinInfo
    {
        [Key(1)]
        public int SkinSlot;
        [Key(2)]
        public int SkinLocalSlot;
        [Key(3)]
        public string SkinGUID;
        [Key(4)]
        public int HeadSlot;
        [Key(5)]
        public string HeadGUID;

        public FaceSkinInfo(int skinID, string skinGUID, int headID, string headGUID)
        {
            SkinSlot = skinID;
            SkinGUID = skinGUID;
            HeadSlot = headID;
            HeadGUID = headGUID;
        }

        public FaceSkinInfo() { }
    }
}
#endif