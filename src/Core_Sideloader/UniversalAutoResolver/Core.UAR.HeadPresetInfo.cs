#if AI || HS2
using System.Collections.Generic;

namespace Sideloader.AutoResolver
{
    internal class HeadPresetInfo
    {
        public string Preset;
        public int HeadID;
        public string HeadGUID;
        public string SkinGUID;
        public string DetailGUID;
        public string EyebrowGUID;
        public string Pupil1GUID;
        public string Pupil2GUID;
        public string Black1GUID;
        public string Black2GUID;
        public string HlGUID;
        public string EyelashesGUID;
        public string MoleGUID;
        public string EyeshadowGUID;
        public string CheekGUID;
        public string LipGUID;
        public string Paint1GUID;
        public string Paint2GUID;
        public string Layout1GUID;
        public string Layout2GUID;

        public Dictionary<string, string> FaceData;
        public Dictionary<string, string> MakeupData;

        public void Init()
        {
            FaceData = new Dictionary<string, string>();
            MakeupData = new Dictionary<string, string>();

            FaceData["detailId"] = DetailGUID;
            FaceData["eyebrowId"] = EyebrowGUID;
            FaceData["eyelashesId"] = EyelashesGUID;
            FaceData["headId"] = HeadGUID;
            FaceData["hlId"] = HlGUID;
            FaceData["moleId"] = MoleGUID;
            FaceData["skinId"] = SkinGUID;
            FaceData["Eye1"] = Pupil1GUID;
            FaceData["Eye2"] = Pupil2GUID;
            FaceData["EyeBlack1"] = Black1GUID;
            FaceData["EyeBlack2"] = Black2GUID;

            MakeupData["cheekId"] = CheekGUID;
            MakeupData["eyeshadowId"] = EyeshadowGUID;
            MakeupData["lipId"] = LipGUID;
            MakeupData["PaintID1"] = Paint1GUID;
            MakeupData["PaintID2"] = Paint2GUID;
            MakeupData["PaintLayoutID1"] = Layout1GUID;
            MakeupData["PaintLayoutID2"] = Layout2GUID;
        }
    }
}
#endif