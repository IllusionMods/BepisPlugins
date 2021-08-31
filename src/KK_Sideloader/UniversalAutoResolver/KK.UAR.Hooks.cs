using HarmonyLib;
using Illusion.Elements.Xml;
using Studio;
using System.Linq;
using System.Xml.Linq;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePrefix(Control __instance, ref int __state)
            {
                __state = -1;
                foreach (Data data in __instance.Datas)
                    if (data is Config.EtceteraSystem etceteraSystem)
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

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePostfix(Control __instance, ref int __state)
            {
                int rampId = __state;
                if (rampId >= BaseSlotID)
                    foreach (Data data in __instance.Datas)
                        if (data is Config.EtceteraSystem etceteraSystem)
                        {
                            ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == rampId);
                            if (RampResolveInfo != null)
                            {
                                //Restore the resolved ID
                                etceteraSystem.rampId = RampResolveInfo.LocalSlot;

                                var xmlDoc = XDocument.Load("UserData/config/system.xml");
                                xmlDoc.Element("System").Element("Etc").Element("rampId").AddAfterSelf(new XElement("rampGUID", RampResolveInfo.GUID));
                                xmlDoc.Save("UserData/config/system.xml");
                            }
                        }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Read))]
            private static void XMLReadPostfix(Control __instance)
            {
                foreach (Data data in __instance.Datas)
                    if (data is Config.EtceteraSystem etceteraSystem)
                        if (etceteraSystem.rampId >= BaseSlotID) //Saved with a resolved ID, reset it to default
                            etceteraSystem.rampId = 1;
                        else
                        {
                            var xmlDoc = XDocument.Load("UserData/config/system.xml");
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
            //Studio
            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Init))]
            private static void SceneInfoInit(SceneInfo __instance)
            {
                var xmlDoc = XDocument.Load("UserData/config/system.xml");
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
        }
    }
}