using System;
using System.IO;
using HarmonyLib;
using Illusion.Elements.Xml;
using Studio;
using System.Linq;
using System.Xml.Linq;
using Config;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            private const string ConfigFilePath = "UserData/config/system.xml";

            [HarmonyPrefix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePrefix(Control __instance, ref int __state)
            {
                try
                {
                    __state = -1;
                    foreach (Data data in __instance.Datas)
                    {
                        if (data is EtceteraSystem etceteraSystem)
                        {
                            if (etceteraSystem.rampId >= BaseSlotID)
                            {
                                ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == etceteraSystem.rampId);
                                if (RampResolveInfo == null)
                                {
                                    //ID is a sideloader ID but no resolve info found, set it to the default
                                    __state = 1;
                                    etceteraSystem.rampId = 1;
                                }
                                else
                                {
                                    //Switch out the resolved ID for the original
                                    __state = etceteraSystem.rampId;
                                    etceteraSystem.rampId = RampResolveInfo.Slot;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    __state = -1;
                    UnityEngine.Debug.LogException(e);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePostfix(Control __instance, ref int __state)
            {
                try
                {
                    int rampId = __state;
                    if (rampId >= BaseSlotID)
                    {
                        foreach (Data data in __instance.Datas)
                        {
                            if (data is EtceteraSystem etceteraSystem)
                            {
                                ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == rampId);
                                if (RampResolveInfo != null)
                                {
                                    //Restore the resolved ID
                                    etceteraSystem.rampId = RampResolveInfo.LocalSlot;

                                    var xmlDoc = XDocument.Load(ConfigFilePath);
                                    xmlDoc.Element("System").Element("Etc").Element("rampId").AddAfterSelf(new XElement("rampGUID", RampResolveInfo.GUID));
                                    xmlDoc.Save(ConfigFilePath);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Read))]
            private static void XMLReadPostfix(Control __instance)
            {
                try
                {
                    foreach (Data data in __instance.Datas)
                    {
                        if (data is EtceteraSystem etceteraSystem)
                        {
                            if (etceteraSystem.rampId >= BaseSlotID) //Saved with a resolved ID, reset it to default
                                etceteraSystem.rampId = 1;
                            else if (File.Exists(ConfigFilePath))
                            {
                                var xmlDoc = XDocument.Load(ConfigFilePath);
                                string rampGUID = xmlDoc.Element("System").Element("Etc").Element("rampGUID")?.Value;
                                if (!rampGUID.IsNullOrWhiteSpace())
                                {
                                    ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == etceteraSystem.rampId);
                                    if (RampResolveInfo == null) //Missing mod, reset ID to default
                                        etceteraSystem.rampId = 1;
                                    else //Restore the resolved ID
                                        etceteraSystem.rampId = RampResolveInfo.LocalSlot;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            //Studio
            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Init))]
            private static void SceneInfoInit(SceneInfo __instance)
            {
                try
                {
                    if (!File.Exists(ConfigFilePath)) return;
                    var xmlDoc = XDocument.Load(ConfigFilePath);
                    string rampGUID = xmlDoc.Element("System").Element("Etc").Element("rampGUID")?.Value;
                    string rampIDXML = xmlDoc.Element("System").Element("Etc").Element("rampId")?.Value;
                    if (!rampGUID.IsNullOrWhiteSpace() && !rampIDXML.IsNullOrWhiteSpace() && int.TryParse(rampIDXML, out int rampID))
                    {
                        ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == rampID);
                        if (RampResolveInfo == null) //Missing mod, reset ID to default
                            __instance.rampG = 1;
                        else //Restore the resolved ID
                            __instance.rampG = RampResolveInfo.LocalSlot;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}