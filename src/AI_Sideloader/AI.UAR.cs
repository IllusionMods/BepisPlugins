using Sideloader.ListLoader;
using System.Collections.Generic;
using System.Linq;
using AIChara;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        private static ILookup<int, HeadPresetInfo> _headPresetInfoLookupSlot;
        private static ILookup<int, FaceSkinInfo> _faceSkinInfoLookupSlot;
        private static ILookup<int, FaceSkinInfo> _faceSkinInfoLookupLocalSlot;

        internal static HeadPresetInfo TryGetHeadPresetInfo(int slot, string guid, string preset) =>
            _headPresetInfoLookupSlot?[slot].FirstOrDefault(x => x.HeadGUID == guid && x.Preset == preset);
        internal static void SetHeadPresetInfos(ICollection<HeadPresetInfo> results) => _headPresetInfoLookupSlot = results.ToLookup(info => info.HeadID);

        internal static FaceSkinInfo TryGetFaceSkinInfo(int slot, string guid) =>
            _faceSkinInfoLookupSlot?[slot].FirstOrDefault(x => x.SkinSlot == slot && x.SkinGUID == guid);
        internal static FaceSkinInfo TryGetFaceSkinInfo(int localSlot) =>
            _faceSkinInfoLookupLocalSlot?[localSlot].FirstOrDefault();
        internal static void SetFaceSkinInfos(ICollection<FaceSkinInfo> results)
        {
            foreach (var info in results)
            {
                var resolveInfo = TryGetResolutionInfo(info.SkinSlot, ChaListDefine.CategoryNo.ft_skin_f, info.SkinGUID);
                if (resolveInfo != null)
                    info.SkinLocalSlot = resolveInfo.LocalSlot;
            }

            _faceSkinInfoLookupSlot = results.ToLookup(info => info.SkinSlot);
            _faceSkinInfoLookupLocalSlot = results.ToLookup(info => info.SkinLocalSlot);
        }

        internal static void GenerateHeadPresetInfo(Manifest manifest, List<HeadPresetInfo> results)
        {
            manifest.LoadHeadPresetInfo();
            results.AddRange(manifest.HeadPresetList);
        }

        internal static void GenerateFaceSkinInfo(Manifest manifest, List<FaceSkinInfo> results)
        {
            manifest.LoadFaceSkinInfo();
            results.AddRange(manifest.FaceSkinList);
        }

        internal static void ResolveFaceSkins()
        {
            foreach (var data in Lists.ExternalDataList.Where(x => x.categoryNo == (int)ChaListDefine.CategoryNo.ft_skin_f))
            {
                int IDIndex = data.lstKey.IndexOf("ID");
                int headIDIndex = data.lstKey.IndexOf("HeadID");

                foreach (var x in data.dictList)
                {
                    int id = int.Parse(x.Value[IDIndex]);
                    int headID = int.Parse(x.Value[headIDIndex]);

                    var faceSkinInfo = TryGetFaceSkinInfo(id);
                    if (faceSkinInfo == null) continue;

                    var resolveInfo = TryGetResolutionInfo(faceSkinInfo.HeadSlot, ChaListDefine.CategoryNo.fo_head, faceSkinInfo.HeadGUID);
                    if (resolveInfo == null)
                        ShowGUIDError(faceSkinInfo.HeadGUID);
                    else
                    {
                        if (headID != faceSkinInfo.HeadSlot)
                        {
                            Sideloader.Logger.LogError($"Error resolving face skins, head ID in manifest does do not match ID in list for GUID:{faceSkinInfo.SkinGUID}, skin ID:{faceSkinInfo.SkinSlot}");
                            continue;
                        }

                        if (Sideloader.DebugResolveInfoLogging.Value)
                            Sideloader.Logger.LogDebug($"Resolving face skin ({faceSkinInfo.SkinGUID}) head ID ({faceSkinInfo.HeadGUID}) from slot {faceSkinInfo.HeadSlot} to slot {resolveInfo.LocalSlot}");

                        x.Value[headIDIndex] = resolveInfo.LocalSlot.ToString();
                    }
                }
            }
        }
    }
}
