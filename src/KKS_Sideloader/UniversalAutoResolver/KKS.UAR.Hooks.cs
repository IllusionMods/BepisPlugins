using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Elements.Xml;
using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Re-enable sideloader card and coordinate saving once import is finished
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.OnDestroy))]
            private static void ConvertChaFileSceneEnd() => DoingImport = false;

            internal static void ExtendedCardImport(Dictionary<string, PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
            {
                if (importedExtendedData.TryGetValue(UARExtID, out var pluginData))
                {
                    if (pluginData != null && pluginData.data.ContainsKey("info"))
                    {
                        var tmpExtInfo = (object[])pluginData.data["info"];
                        var extInfo = tmpExtInfo.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();
                        List<ResolveInfo> extInfoToRemove = new List<ResolveInfo>();

                        foreach (var info in extInfo)
                        {
                            if (info.Property.StartsWith("outfit"))
                            {
                                var propertySplit = info.Property.Split('.');
                                int index = int.Parse(propertySplit[0].Replace("outfit", ""));
                                if (coordinateMapping.TryGetValue(index, out int? newIndex) && newIndex != null)
                                {
                                    propertySplit[0] = $"outfit{newIndex}";
                                    info.Property = string.Join(".", propertySplit);
                                }
                                else
                                {
                                    //Remove all the excess outfits
                                    extInfoToRemove.Add(info);
                                }
                            }
                        }
                        foreach (var infoToRemove in extInfoToRemove)
                            extInfo.Remove(infoToRemove);

                        importedExtendedData[UARExtID] = new PluginData
                        {
                            data = new Dictionary<string, object>
                            {
                                ["info"] = extInfo.Select(x => x.Serialize()).ToList()
                            }
                        };
                    }
                }

                if (Sideloader.DebugLogging.Value && importedExtendedData.TryGetValue(UARExtID, out var extData))
                {
                    if (extData == null || !extData.data.ContainsKey("info"))
                    {
                        Sideloader.Logger.Log(LogLevel.Debug, "Imported card data: No sideloader marker found");
                    }
                    else
                    {
                        var tmpExtInfo = (List<byte[]>)extData.data["info"];
                        var extInfo = tmpExtInfo.Select(ResolveInfo.Deserialize).ToList();

                        Sideloader.Logger.Log(LogLevel.Debug, $"Imported card data: Sideloader marker found, external info count: {extInfo.Count}");

                        foreach (ResolveInfo info in extInfo)
                            Sideloader.Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot}");
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePrefix(Control __instance, ref int __state)
            {
                __state = -1;
                foreach (Data data in __instance.Datas)
                    if (data is Config.GraphicSystem graphicSystem)
                        if (graphicSystem.rampId >= BaseSlotID)
                        {
                            ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == graphicSystem.rampId);
                            if (RampResolveInfo == null)
                            {
                                //ID is a sideloader ID but no resolve info found, set it to the default
                                __state = 1;
                                graphicSystem.rampId = 1;
                            }
                            else
                            {
                                //Switch out the resolved ID for the original
                                __state = graphicSystem.rampId;
                                graphicSystem.rampId = RampResolveInfo.Slot;
                            }
                        }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePostfix(Control __instance, ref int __state)
            {
                int rampId = __state;
                if (rampId >= BaseSlotID)
                    foreach (Data data in __instance.Datas)
                        if (data is Config.GraphicSystem graphicSystem)
                        {
                            ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.LocalSlot == rampId);
                            if (RampResolveInfo != null)
                            {
                                //Restore the resolved ID
                                graphicSystem.rampId = RampResolveInfo.LocalSlot;

                                var xmlDoc = XDocument.Load("UserData/config/system.xml");
                                xmlDoc.Element("System").Element("Graphic").Element("rampId").AddAfterSelf(new XElement("rampGUID", RampResolveInfo.GUID));
                                xmlDoc.Save("UserData/config/system.xml");
                            }
                        }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Control), nameof(Control.Read))]
            private static void XMLReadPostfix(Control __instance)
            {
                foreach (Data data in __instance.Datas)
                    if (data is Config.GraphicSystem graphicSystem)
                        if (graphicSystem.rampId >= BaseSlotID) //Saved with a resolved ID, reset it to default
                            graphicSystem.rampId = 1;
                        else
                        {
                            var xmlDoc = XDocument.Load("UserData/config/system.xml");
                            string rampGUID = xmlDoc.Element("System").Element("Graphic").Element("rampGUID")?.Value;
                            if (!rampGUID.IsNullOrWhiteSpace())
                            {
                                ResolveInfo RampResolveInfo = LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Ramp" && x.GUID == rampGUID && x.Slot == graphicSystem.rampId);
                                if (RampResolveInfo == null) //Missing mod, reset ID to default
                                    graphicSystem.rampId = 1;
                                else //Restore the resolved ID
                                    graphicSystem.rampId = RampResolveInfo.LocalSlot;
                            }
                        }
            }
            //Studio
            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Init))]
            private static void SceneInfoInit(SceneInfo __instance)
            {
                var xmlDoc = XDocument.Load("UserData/config/system.xml");
                string rampGUID = xmlDoc.Element("System").Element("Graphic").Element("rampGUID")?.Value;
                string rampIDXML = xmlDoc.Element("System").Element("Graphic").Element("rampId")?.Value;
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