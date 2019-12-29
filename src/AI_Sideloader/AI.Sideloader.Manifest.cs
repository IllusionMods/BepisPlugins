using Sideloader.AutoResolver;
using System;
using System.Collections.Generic;

namespace Sideloader
{
    public partial class Manifest
    {
        internal List<HeadPresetInfo> HeadPresetList = new List<HeadPresetInfo>();
        internal List<FaceSkinInfo> FaceSkinList = new List<FaceSkinInfo>();

        internal void LoadHeadPresetInfo()
        {
            foreach (var info in manifestDocument.Root.Elements("headPresetInfo"))
            {
                try
                {
                    string preset = info.Attribute("preset")?.Value;
                    string headID = info.Element("headID")?.Value;
                    string headGUID = info.Element("headGUID")?.Value;
                    string skinGUID = info.Element("skinGUID")?.Value;
                    string detailGUID = info.Element("detailGUID")?.Value;
                    string eyebrowGUID = info.Element("eyebrowGUID")?.Value;
                    string pupil1GUID = info.Element("pupil1GUID")?.Value;
                    string pupil2GUID = info.Element("pupil2GUID")?.Value;
                    string black1GUID = info.Element("black1GUID")?.Value;
                    string black2GUID = info.Element("black2GUID")?.Value;
                    string hlGUID = info.Element("hlGUID")?.Value;
                    string eyelashesGUID = info.Element("eyelashesGUID")?.Value;
                    string moleGUID = info.Element("moleGUID")?.Value;
                    string eyeshadowGUID = info.Element("eyeshadowGUID")?.Value;
                    string cheekGUID = info.Element("cheekGUID")?.Value;
                    string lipGUID = info.Element("lipGUID")?.Value;
                    string paint1GUID = info.Element("paint1GUID")?.Value;
                    string paint2GUID = info.Element("paint2GUID")?.Value;
                    string layout1GUID = info.Element("layout1GUID")?.Value;
                    string layout2GUID = info.Element("layout2GUID")?.Value;

                    HeadPresetInfo headPresetInfo = new HeadPresetInfo();

                    if (preset.IsNullOrWhiteSpace())
                        throw new Exception("Preset must be specified.");
                    if (!int.TryParse(headID, out int headIDInt))
                        throw new Exception("HeadID must be specified.");
                    headPresetInfo.Preset = preset;
                    headPresetInfo.HeadID = headIDInt;
                    headPresetInfo.HeadGUID = headGUID.IsNullOrWhiteSpace() ? null : headGUID;
                    headPresetInfo.SkinGUID = skinGUID.IsNullOrWhiteSpace() ? null : skinGUID;
                    headPresetInfo.DetailGUID = detailGUID.IsNullOrWhiteSpace() ? null : detailGUID;
                    headPresetInfo.EyebrowGUID = eyebrowGUID.IsNullOrWhiteSpace() ? null : eyebrowGUID;
                    headPresetInfo.Pupil1GUID = pupil1GUID.IsNullOrWhiteSpace() ? null : pupil1GUID;
                    headPresetInfo.Pupil2GUID = pupil2GUID.IsNullOrWhiteSpace() ? null : pupil2GUID;
                    headPresetInfo.Black1GUID = black1GUID.IsNullOrWhiteSpace() ? null : black1GUID;
                    headPresetInfo.Black2GUID = black2GUID.IsNullOrWhiteSpace() ? null : black2GUID;
                    headPresetInfo.HlGUID = hlGUID.IsNullOrWhiteSpace() ? null : hlGUID;
                    headPresetInfo.EyelashesGUID = eyelashesGUID.IsNullOrWhiteSpace() ? null : eyelashesGUID;
                    headPresetInfo.MoleGUID = moleGUID.IsNullOrWhiteSpace() ? null : moleGUID;
                    headPresetInfo.EyeshadowGUID = eyeshadowGUID.IsNullOrWhiteSpace() ? null : eyeshadowGUID;
                    headPresetInfo.CheekGUID = cheekGUID.IsNullOrWhiteSpace() ? null : cheekGUID;
                    headPresetInfo.LipGUID = lipGUID.IsNullOrWhiteSpace() ? null : lipGUID;
                    headPresetInfo.Paint1GUID = paint1GUID.IsNullOrWhiteSpace() ? null : paint1GUID;
                    headPresetInfo.Paint2GUID = paint2GUID.IsNullOrWhiteSpace() ? null : paint2GUID;
                    headPresetInfo.Layout1GUID = layout1GUID.IsNullOrWhiteSpace() ? null : layout1GUID;
                    headPresetInfo.Layout2GUID = layout2GUID.IsNullOrWhiteSpace() ? null : layout2GUID;
                    headPresetInfo.Init();
                    HeadPresetList.Add(headPresetInfo);
                }
                catch (Exception ex)
                {
                    Sideloader.Logger.LogError($"Could not load head preset data for {GUID}, skipping line.");
                    Sideloader.Logger.LogError(ex);
                }
            }
        }

        internal void LoadFaceSkinInfo()
        {
            foreach (var info in manifestDocument.Root.Elements("faceSkinInfo"))
            {
                try
                {
                    string skinID = info.Attribute("skinID")?.Value;
                    string headID = info.Attribute("headID")?.Value;
                    string headGUID = info.Attribute("headGUID")?.Value;

                    if (!int.TryParse(skinID, out int skinIDInt))
                        throw new Exception("SkinID must be specified.");
                    if (!int.TryParse(headID, out int headIDInt))
                        throw new Exception("HeadID must be specified.");
                    if (headGUID.IsNullOrWhiteSpace())
                        throw new Exception("HeadGUID must be specified.");

                    FaceSkinInfo faceSkinInfo = new FaceSkinInfo(skinIDInt, GUID, headIDInt, headGUID);
                    FaceSkinList.Add(faceSkinInfo);
                }
                catch (Exception ex)
                {
                    Sideloader.Logger.LogError($"Could not load face skin data for {GUID}, skipping line.");
                    Sideloader.Logger.LogError(ex);
                }
            }
        }
    }
}
