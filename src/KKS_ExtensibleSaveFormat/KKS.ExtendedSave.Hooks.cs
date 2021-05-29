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
            #region Import KK Chara

            private static void KKChaFileLoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
            {
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
                            CardImportEvent(dictionary);
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
                                    CardImportEvent(dictionary);
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
        }
    }
}