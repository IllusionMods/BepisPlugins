using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Re-enable sideloader card and coordinate saving once import is finished
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFileScene), "OnDestroy")]
            private static void ConvertChaFileSceneEnd() => DoingImport = false;

            internal static void ExtendedCardImport(Dictionary<string, PluginData> importedExtendedData)
            {
                if (importedExtendedData.TryGetValue(UARExtID, out var pluginData))
                {
                    if (pluginData != null && pluginData.data.ContainsKey("info"))
                    {
                        var tmpExtInfo = (object[])pluginData.data["info"];
                        var extInfo = tmpExtInfo.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();

                        for (int i = 0; i < extInfo.Count;)
                        {
                            if (extInfo[i].Property.StartsWith("outfit0") && extInfo[i].Property.EndsWith("ClothesShoesInner"))
                            {
                                //KK had inner shoes, EC does not
                                extInfo.RemoveAt(i);
                            }
                            else if (extInfo[i].Property.StartsWith("outfit0"))

                            {
                                extInfo[i].Property = extInfo[i].Property.Replace("outfit0", "outfit");

                                //KK originally had only one emblem
                                if (extInfo[i].Property.EndsWith("Emblem"))
                                    extInfo[i].Property += "0";

                                //KK has multiple shoes slots, convert to one shoes slot
                                extInfo[i].Property = extInfo[i].Property.Replace("ClothesShoesOuter", "ClothesShoes");

                                i++;
                            }
                            else if (extInfo[i].Property.StartsWith("outfit"))
                            {
                                //Remove all the excess outfits
                                extInfo.RemoveAt(i);
                            }
                            else
                                i++;
                        }

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

            internal static void ExtendedCoordinateImport(Dictionary<string, PluginData> importedExtendedData)
            {
                if (importedExtendedData.TryGetValue(UARExtID, out var pluginData))
                {
                    if (pluginData != null && pluginData.data.ContainsKey("info"))
                    {
                        var tmpExtInfo = (object[])pluginData.data["info"];
                        var extInfo = tmpExtInfo.Select(x => ResolveInfo.Deserialize((byte[])x)).ToList();

                        for (int i = 0; i < extInfo.Count;)
                        {
                            Sideloader.Logger.Log(LogLevel.Debug, $"External info: {extInfo[i].GUID} : {extInfo[i].Property} : {extInfo[i].Slot} : {extInfo[i].CategoryNo}");
                            if (extInfo[i].Property.EndsWith("ClothesShoesInner"))
                            {
                                //KK had inner shoes, EC does not
                                extInfo.RemoveAt(i);
                            }
                            else
                            {
                                extInfo[i].Property = extInfo[i].Property.Replace("outfit0", "outfit");

                                //KK originally had only one emblem
                                if (extInfo[i].Property.EndsWith("Emblem"))
                                    extInfo[i].Property += "0";

                                //KK has multiple shoes slots, convert to one shoes slot
                                extInfo[i].Property = extInfo[i].Property.Replace("ClothesShoesOuter", "ClothesShoes");

                                i++;
                            }
                        }

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
                        Sideloader.Logger.Log(LogLevel.Debug, "Imported coordinate data: No sideloader marker found");
                    }
                    else
                    {
                        var tmpExtInfo = (List<byte[]>)extData.data["info"];
                        var extInfo = tmpExtInfo.Select(ResolveInfo.Deserialize).ToList();

                        Sideloader.Logger.Log(LogLevel.Debug, $"Imported coordinate data: Sideloader marker found, external info count: {extInfo.Count}");

                        foreach (ResolveInfo info in extInfo)
                            Sideloader.Logger.Log(LogLevel.Debug, $"External info: {info.GUID} : {info.Property} : {info.Slot} : {info.CategoryNo}");
                    }
                }
            }
        }
    }
}