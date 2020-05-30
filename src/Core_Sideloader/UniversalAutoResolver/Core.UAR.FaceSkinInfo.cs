#if AI || HS2
namespace Sideloader.AutoResolver
{
    internal class FaceSkinInfo
    {
        public int SkinSlot;
        public int SkinLocalSlot;
        public string SkinGUID;
        public int HeadSlot;
        public string HeadGUID;

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