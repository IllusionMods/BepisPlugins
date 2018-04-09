using BepInEx;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sideloader.AutoResolver
{
    public static class UniversalAutoResolver
    {
        public const string UARExtID = "com.bepis.sideloader.universalautoresolver";

        public static List<ResolveInfo> LoadedResolutionInfo = new List<ResolveInfo>();

        public static void ResolveStructure(Dictionary<string, PropertyInfo> propertyDict, object structure, ChaFile save)
        {
            if (LoadedResolutionInfo.Count == 0)
                GenerateResolutionInfo();

            //BepInLogger.Log($"Tried to resolve structure: {structure.GetType().Name}");

            var extData = ExtendedSave.GetExtendedDataById(save, UARExtID);

            if (extData == null || !extData.data.ContainsKey("info"))
            {
                //BepInLogger.Log($"No info to load!");
                return;
            }

            var obj = extData.data["info"];


            //BepInLogger.Log(obj.GetType().ToString());



            var tmpExtInfo = (object[])extData.data["info"];
            var extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x));

            //BepInLogger.Log($"Internal info count: {LoadedResolutionInfo.Count}");
            //foreach (ResolveInfo info in LoadedResolutionInfo)
            //    BepInLogger.Log($"Internal info: {info.ModID} : {info.Property} : {info.Slot}");
            
            //BepInLogger.Log($"External info count: {extInfo.Count()}");
            //foreach (ResolveInfo info in extInfo)
            //    BepInLogger.Log($"External info: {info.ModID} : {info.Property} : {info.Slot}");


            foreach (var kv in propertyDict)
            {
                var extResolve = extInfo.FirstOrDefault(x => x.Property == kv.Key);

                if (extResolve != null)
                {
                    var intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.CanResolve(extResolve));

                    if (intResolve != null)
                    {
                        BepInLogger.Log($"[UAR] Resolving {extResolve.ModID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.Slot}");

                        kv.Value.SetValue(structure, intResolve.Slot, null);
                    }
                }
            }

            specialCasingResolve(structure, extInfo);
        }

        private static bool specialCasingResolve(object structure, IEnumerable<ResolveInfo> extInfo)
        {
            if (structure is ChaFileFace)
            {
                ChaFileFace face = (ChaFileFace)structure;

                ResolveInfo extResolve;
                ResolveInfo intResolve;

                extResolve = extInfo.FirstOrDefault(x => x.Property == "Pupil1");
                if (extResolve != null)
                {
                    intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.CanResolve(extResolve));

                    if (intResolve != null)
                    {
                        BepInLogger.Log($"[UAR] Resolving {extResolve.ModID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.Slot}");

                        face.pupil[0].id = intResolve.Slot;
                    }
                }
                extResolve = null;
                
                
                extResolve = extInfo.FirstOrDefault(x => x.Property == "Pupil2");
                if (extResolve != null)
                {
                    intResolve = LoadedResolutionInfo.FirstOrDefault(x => x.CanResolve(extResolve));

                    if (intResolve != null)
                    {
                        BepInLogger.Log($"[UAR] Resolving {extResolve.ModID}:{extResolve.Property} from slot {extResolve.Slot} to slot {intResolve.Slot}");

                        face.pupil[1].id = intResolve.Slot;
                    }
                }
                extResolve = null;

                return true;
            }

            return false;
        }

        public static void GenerateResolutionInfo()
        {
            LoadedResolutionInfo.Clear();

            foreach (var manifestData in Sideloader.LoadedData)
            {
                foreach (var data in manifestData.Value)
                {
                    foreach (var kv in data.dictList)
                    {
                        if (!StructReference.ChaFileFaceCategories.ContainsKey((ChaListDefine.CategoryNo)data.categoryNo))
                        {
                            if ((ChaListDefine.CategoryNo)data.categoryNo == ChaListDefine.CategoryNo.mt_eye)
                            {
                                var pupilInfo = new ResolveInfo
                                {
                                    ModID = manifestData.Key.GUID,
                                    Slot = int.Parse(kv.Value[0]),
                                    Property = "Pupil1"
                                };

                                LoadedResolutionInfo.Add(pupilInfo);

                                pupilInfo = new ResolveInfo
                                {
                                    ModID = manifestData.Key.GUID,
                                    Slot = int.Parse(kv.Value[0]),
                                    Property = "Pupil2"
                                };
                                LoadedResolutionInfo.Add(pupilInfo);
                            }

                            continue;
                        }

                        var info = new ResolveInfo
                        {
                            ModID = manifestData.Key.GUID,
                            Slot = int.Parse(kv.Value[0]),
                            Property = StructReference.ChaFileFaceCategories[(ChaListDefine.CategoryNo)data.categoryNo]
                        };

                        LoadedResolutionInfo.Add(info);
                    }
                }
            }
        }
    }
}
