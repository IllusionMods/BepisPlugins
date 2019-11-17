using AIChara;
using HarmonyLib;
using Manager;
using System.Collections.Generic;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Set the head ID for face skin types to the resolved head ID
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.Set))]
            internal static void ListInfoBaseSet(int _cateNo, List<string> lstKey, List<string> lstData)
            {
                if (_cateNo == 211)
                {
                    int headIDIndex = lstKey.IndexOf("HeadID");

                    if (headIDIndex == -1) return;
                    if (!int.TryParse(lstData[headIDIndex], out int headID)) return;

                    var resolveInfo = TryGetResolutionInfo(headID, ChaListDefine.CategoryNo.fo_head);
                    if (resolveInfo != null)
                        lstData[headIDIndex] = resolveInfo.LocalSlot.ToString();
                }
            }
            /// <summary>
            /// Find the head preset data
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            internal static void LoadFacePresetPrefix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                __state = null;
                int headID = __instance.custom.face.headId;
                if (headID >= BaseSlotID)
                {
                    ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                    ListInfoBase listInfo = chaListCtrl.GetListInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    string preset = listInfo.GetInfo(ChaListDefine.KeyType.Preset);

                    var resolveinfo = TryGetResolutionInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    if (resolveinfo == null) return;

                    var headPresetInfo = TryGetHeadPresetInfo(resolveinfo.Slot, resolveinfo.GUID, preset);
                    __state = headPresetInfo;
                }
                else
                {
                    ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                    ListInfoBase listInfo = chaListCtrl.GetListInfo(__instance.parameter.sex == 0 ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, __instance.custom.face.headId);
                    string preset = listInfo.GetInfo(ChaListDefine.KeyType.Preset);

                    var headPresetInfo = TryGetHeadPresetInfo(headID, null, preset);
                    __state = headPresetInfo;
                }
            }
            /// <summary>
            /// Use the head preset data to resolve the IDs
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFacePreset))]
            internal static void LoadFacePresetPostfix(ChaFileControl __instance, ref HeadPresetInfo __state)
            {
                if (__state == null) return;

                List<ResolveInfo> faceResolveInfos = new List<ResolveInfo>();
                List<ResolveInfo> makeupResolveInfos = new List<ResolveInfo>();

                foreach (var property in StructReference.ChaFileFaceProperties)
                {
                    if (__state.FaceData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.custom.face), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.FaceData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.FaceData[property.Key.Property]);
                    else
                        faceResolveInfos.Add(resolveinfo);
                }

                foreach (var property in StructReference.ChaFileMakeupProperties)
                {
                    if (__state.MakeupData[property.Key.Property] == null) continue;
                    var resolveinfo = TryGetResolutionInfo(property.Value.GetMethod(__instance.custom.face.makeup), $"{property.Key.Prefix}.{property.Key.Property}", property.Key.Category, __state.MakeupData[property.Key.Property]);
                    if (resolveinfo == null)
                        ShowGUIDError(__state.MakeupData[property.Key.Property]);
                    else
                        makeupResolveInfos.Add(resolveinfo);
                }

                ResolveStructure(StructReference.ChaFileFaceProperties, __instance.custom.face, faceResolveInfos);
                ResolveStructure(StructReference.ChaFileMakeupProperties, __instance.custom.face.makeup, makeupResolveInfos);
            }
        }
    }
}