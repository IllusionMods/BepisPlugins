using System;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Elements.Xml;
using Studio;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Config;

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

            #region rampID GUID

            private const string ConfigFilePath = "UserData/config/system.xml";

            [HarmonyPrefix, HarmonyPatch(typeof(Control), nameof(Control.Write))]
            private static void XMLWritePrefix(Control __instance, ref int __state)
            {
                try
                {
                    __state = -1;
                    foreach (Data data in __instance.Datas)
                    {
                        if (data is GraphicSystem graphicSystem)
                        {
                            if (graphicSystem.rampId >= BaseSlotID)
                            {
                                ResolveInfo RampResolveInfo = TryGetResolutionInfo("Ramp", graphicSystem.rampId);
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
                            if (data is GraphicSystem graphicSystem)
                            {
                                ResolveInfo RampResolveInfo = TryGetResolutionInfo("Ramp", rampId);
                                if (RampResolveInfo != null)
                                {
                                    //Restore the resolved ID
                                    graphicSystem.rampId = RampResolveInfo.LocalSlot;
                                    
                                    var xmlDoc = XDocument.Load(ConfigFilePath);
                                    xmlDoc.Element("System").Element("Graphic").Element("rampId").AddAfterSelf(new XElement("rampGUID", RampResolveInfo.GUID));
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
                        if (data is GraphicSystem graphicSystem)
                        {
                            if (graphicSystem.rampId >= BaseSlotID) //Saved with a resolved ID, reset it to default
                                graphicSystem.rampId = 1;
                            else if (File.Exists(ConfigFilePath))
                            {
                                var xmlDoc = XDocument.Load(ConfigFilePath);
                                string rampGUID = xmlDoc.Element("System").Element("Graphic").Element("rampGUID")?.Value;
                                if (!rampGUID.IsNullOrWhiteSpace())
                                {
                                    ResolveInfo RampResolveInfo = TryGetResolutionInfo(graphicSystem.rampId, "Ramp", rampGUID);
                                    if (RampResolveInfo == null) //Missing mod, reset ID to default
                                        graphicSystem.rampId = 1;
                                    else //Restore the resolved ID
                                        graphicSystem.rampId = RampResolveInfo.LocalSlot;
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
                    string rampGUID = xmlDoc.Element("System").Element("Graphic").Element("rampGUID")?.Value;
                    string rampIDXML = xmlDoc.Element("System").Element("Graphic").Element("rampId")?.Value;
                    if (!rampGUID.IsNullOrWhiteSpace() && !rampIDXML.IsNullOrWhiteSpace() && int.TryParse(rampIDXML, out int rampID))
                    {
                        ResolveInfo RampResolveInfo = TryGetResolutionInfo(rampID, "Ramp", rampGUID);
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

            #endregion
        }
    }
}