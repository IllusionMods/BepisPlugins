#if AI || HS2
using System.Collections.Generic;
using MessagePack;

namespace Sideloader.AutoResolver
{
    [MessagePackObject]
    public class HeadPresetInfo
    {
        [Key(1)] public string Preset;
        [Key(2)] public int HeadID;
        [Key(3)] public string HeadGUID;
        [Key(4)] public string SkinGUID;
        [Key(5)] public string DetailGUID;
        [Key(6)] public string EyebrowGUID;
        [Key(7)] public string Pupil1GUID;
        [Key(8)] public string Pupil2GUID;
        [Key(9)] public string Black1GUID;
        [Key(10)] public string Black2GUID;
        [Key(11)] public string HlGUID;
        [Key(12)] public string EyelashesGUID;
        [Key(13)] public string MoleGUID;
        [Key(14)] public string EyeshadowGUID;
        [Key(15)] public string CheekGUID;
        [Key(16)] public string LipGUID;
        [Key(17)] public string Paint1GUID;
        [Key(18)] public string Paint2GUID;
        [Key(19)] public string Layout1GUID;
        [Key(20)] public string Layout2GUID;

        [Key(21)] public Dictionary<string, string> FaceData;
        [Key(22)] public Dictionary<string, string> MakeupData;

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

        public HeadPresetInfo() { }
    }
}
#endif