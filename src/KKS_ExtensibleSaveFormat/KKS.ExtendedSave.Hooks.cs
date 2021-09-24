using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            private static readonly bool MoreOutfitsInstalled;
            private static Dictionary<int, int?> CoordinateMapping = new Dictionary<int, int?>();

            static Hooks()
            {
                MoreOutfitsInstalled = Type.GetType($"KK_Plugins.MoreOutfits.Plugin, KKS_MoreOutfits") != null;
                if (!MoreOutfitsInstalled)
                    HandleNoMoreOutfitsHooks.Apply();
            }

            #region Import KK Chara

            private static void KKChaFileLoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
            {
                CoordinateMapping = new Dictionary<int, int?>();

                // Remap coordinates from the KK index to the matching KKS index
                ChaFileCoordinate[] newCoordinates = new ChaFileCoordinate[file.coordinate.Length];
                for (int i = 0; i < file.coordinate.Length; i++)
                {
                    int index = i;
                    int? newIndex = null;

                    switch (i)
                    {
                        case 0:
                            //Get rid of these outfits without MoreOutfits installed since they wouldn't be usable
                            if (MoreOutfitsInstalled)
                                newIndex = 4;
                            break;
                        case 1:
                            newIndex = 3;
                            break;
                        case 2:
                            if (MoreOutfitsInstalled)
                                newIndex = 5;
                            break;
                        case 3:
                            newIndex = 1;
                            break;
                        case 4:
                            if (MoreOutfitsInstalled)
                                newIndex = 6;
                            break;
                        case 5:
                            newIndex = 0;
                            break;
                        case 6:
                            newIndex = 2;
                            break;
                        default:
                            //Carry over extra outfits if MoreOutfits is installed, otherwise drop them 
                            if (MoreOutfitsInstalled)
                                newIndex = index;
                            break;
                    }

                    CoordinateMapping[index] = newIndex;
                    if (newIndex != null)
                        newCoordinates[(int)newIndex] = file.coordinate[index];
                }

                if (!MoreOutfitsInstalled)
                    newCoordinates = newCoordinates.Take(4).ToArray();

                file.coordinate = newCoordinates;

                var info = header.SearchInfo(Marker);
                if (info != null && info.version == DataVersion.ToString())
                {
                    var originalPosition = reader.BaseStream.Position;
                    var basePosition = originalPosition - header.lstInfo.Sum(x => x.size);

                    reader.BaseStream.Position = basePosition + info.pos;

                    var data = reader.ReadBytes((int)info.size);

                    reader.BaseStream.Position = originalPosition;

                    try
                    {
                        var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(data);
                        if (dictionary != null)
                        {
                            CardImportEvent(dictionary, CoordinateMapping);
                            internalCharaDictionary.Set(file, dictionary);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Warning, $"Invalid or corrupted extended data in card \"{file.charaFileName}\" - {e.Message}");
                        reader.BaseStream.Position = originalPosition;
                    }
                }
            }

            //protected bool LoadFileKoikatsu(global::System.IO.BinaryReader br, bool noLoadPNG = false, bool noLoadStatus = true)
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFileKoikatsu), typeof(BinaryReader), typeof(bool), typeof(bool))]
            private static IEnumerable<CodeInstruction> KKChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var newInstructionSet = new List<CodeInstruction>(instructions);

                //get the index of the first searchinfo call
                var searchInfoIndex = newInstructionSet.FindIndex(instruction => CheckCallVirtName(instruction, "SearchInfo"));

                //get the index of the last seek call
                var lastSeekIndex = newInstructionSet.FindLastIndex(instruction => CheckCallVirtName(instruction, "Seek"));

                var blockHeaderLocalBuilder = newInstructionSet[searchInfoIndex - 2]; //get the localbuilder for the blockheader

                //insert our own hook right after the last seek
                newInstructionSet.InsertRange(
                    lastSeekIndex + 2, //we insert AFTER the NEXT instruction, which is right before the try block exit
                    new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                        new CodeInstruction(blockHeaderLocalBuilder.opcode, blockHeaderLocalBuilder.operand), //push the BlockHeader instance
                        new CodeInstruction(OpCodes.Ldarg_1), //push the binaryreader instance
                        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(KKChaFileLoadFileHook), AccessTools.all)) //call our hook
                    });

                return newInstructionSet;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFileKoikatsu), typeof(BinaryReader), typeof(bool), typeof(bool))]
            private static void KKChaFileLoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br)
            {
                if (!__result)
                    return;

                //Compatibility for ver 1 and 2 ext save data
                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    long originalPosition = br.BaseStream.Position;

                    try
                    {
                        string marker = br.ReadString();
                        int version = br.ReadInt32();

                        if (marker == "KKEx" && version == 2)
                        {
                            int length = br.ReadInt32();

                            if (length > 0)
                            {
                                byte[] bytes = br.ReadBytes(length);
                                var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                                if (dictionary != null)
                                {
                                    CardImportEvent(dictionary, CoordinateMapping);
                                    internalCharaDictionary.Set(__instance, dictionary);
                                }
                            }
                        }
                        else
                        {
                            br.BaseStream.Position = originalPosition;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        /* Incomplete/non-existant data */
                        br.BaseStream.Position = originalPosition;
                    }
                    catch (SystemException)
                    {
                        /* Invalid/unexpected deserialized data */
                        br.BaseStream.Position = originalPosition;
                    }
                }
            }

            #endregion

            /// <summary>
            /// Prevent coords from being trimmed to 4 and then overwritten with defaults if MoreOutfits is missing
            /// (basically a copy of some of the MoreOutfits hooks, should not be applied if MoreOutfits exists)
            /// </summary>
            private static class HandleNoMoreOutfitsHooks
            {
                private static Harmony _nmoHooks;

                public static void Apply()
                {
                    _nmoHooks = Harmony.CreateAndPatchAll(typeof(HandleNoMoreOutfitsHooks), nameof(HandleNoMoreOutfitsHooks));
                }

                /// <summary>
                /// Ensure extra coordinates are loaded
                /// </summary>
                [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SetCoordinateBytes))]
                private static bool SetCoordinateBytes(ChaFile __instance, byte[] data, Version ver)
                {
                    List<byte[]> list = MessagePack.MessagePackSerializer.Deserialize<List<byte[]>>(data);

                    //Reinitialize the array with the new length
                    __instance.coordinate = new ChaFileCoordinate[list.Count];
                    for (int i = 0; i < list.Count; i++)
                        __instance.coordinate[i] = new ChaFileCoordinate();

                    //Load all the coordinates
                    for (int i = 0; i < __instance.coordinate.Length; i++)
                        __instance.coordinate[i].LoadBytes(list[i], ver);

                    return false;
                }

                private static bool DoingImport = true;

                [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.OnDestroy))]
                private static void ConvertChaFileSceneEnd()
                {
                    DoingImport = false;
                    _nmoHooks.UnpatchSelf();
                }

                /// <summary>
                /// Don't allow outfits to be replaced by defaults
                /// </summary>
                [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.AssignCoordinate))]
                private static bool ChaFile_AssignCoordinate()
                {
                    if (DoingImport)
                        return false;
                    return true;
                }
            }
        }
    }
}