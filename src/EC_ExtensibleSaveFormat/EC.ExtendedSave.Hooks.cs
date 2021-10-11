using BepInEx.Logging;
using HarmonyLib;
using HEdit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using static Extensions;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region Import KK Chara

            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFile), nameof(ConvertChaFile.ConvertCharaFile))]
            private static void ConvertChaFilePostHook(ChaFileControl cfc, KoikatsuCharaFile.ChaFile kkfile)
            {
                // Move data from import dictionary to normal dictionary before the imported cards are saved so the imported data is written
                var result = _internalCharaImportDictionary.Get(kkfile);
                if (result != null)
                {
                    CardImportEvent(result);
                    internalCharaDictionary.Set(cfc, result);
                }
            }

            private static void KKChaFileLoadFileHook(KoikatsuCharaFile.ChaFile file, BlockHeader header, BinaryReader reader)
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
                        _internalCharaImportDictionary.Set(file, dictionary);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Warning, $"Invalid or corrupted extended data in card \"{file.charaFileName}\" - {e.Message}");
                        reader.BaseStream.Position = originalPosition;
                    }
                }
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(KoikatsuCharaFile.ChaFile), nameof(KoikatsuCharaFile.ChaFile.LoadFile), typeof(BinaryReader), typeof(bool), typeof(bool))]
            private static IEnumerable<CodeInstruction> KKChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var newInstructionSet = new List<CodeInstruction>(instructions);

                //get the index of the first searchinfo call
                var searchInfoIndex = newInstructionSet.FindIndex(instruction => CheckCallVirtName(instruction, "SearchInfo"));

                //get the index of the last seek call
                var lastSeekIndex = newInstructionSet.FindLastIndex(instruction => CheckCallVirtName(instruction, "Seek"));

                var blockHeaderLocalBuilder = (LocalBuilder)newInstructionSet[searchInfoIndex - 2].operand; //get the localbuilder for the blockheader

                //insert our own hook right after the last seek
                newInstructionSet.InsertRange(
                    lastSeekIndex + 2, //we insert AFTER the NEXT instruction, which is right before the try block exit
                    new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                        new CodeInstruction(OpCodes.Ldloc_S, blockHeaderLocalBuilder), //push the BlockHeader instance
                        new CodeInstruction(OpCodes.Ldarg_1, blockHeaderLocalBuilder), //push the binaryreader instance
                        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(KKChaFileLoadFileHook), AccessTools.all)) //call our hook
                    });

                return newInstructionSet;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(KoikatsuCharaFile.ChaFile), nameof(KoikatsuCharaFile.ChaFile.LoadFile), typeof(BinaryReader), typeof(bool), typeof(bool))]
            private static void KKChaFileLoadFilePostHook(KoikatsuCharaFile.ChaFile __instance, bool __result, BinaryReader br)
            {
                if (!__result)
                    return;

                //Compatibility for ver 1 and 2 ext save data
                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    var originalPosition = br.BaseStream.Position;

                    try
                    {
                        var marker = br.ReadString();
                        var version = br.ReadInt32();

                        if (marker == "KKEx" && version == 2)
                        {
                            var length = br.ReadInt32();

                            if (length > 0)
                            {
                                var bytes = br.ReadBytes(length);
                                var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                                _internalCharaImportDictionary.Set(__instance, dictionary);
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

            #region Import KK Coordinate

            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFile), nameof(ConvertChaFile.ConvertCoordinateFile))]
            private static void ConvertCoordinateFile(ChaFileCoordinate emcoorde, KoikatsuCharaFile.ChaFileCoordinate kkcoorde)
            {
                // Move data from import dictionary to normal dictionary before the imported cards are saved so the imported data is written
                var result = _internalCoordinateImportDictionary.Get(kkcoorde);
                if (result != null)
                {
                    CoordinateImportEvent(result);
                    internalCoordinateDictionary.Set(emcoorde, result);
                }
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(KoikatsuCharaFile.ChaFileCoordinate), nameof(KoikatsuCharaFile.ChaFileCoordinate.LoadFile), typeof(Stream), typeof(bool))]
            private static IEnumerable<CodeInstruction> KKChaFileCoordinateLoadTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var usingFinishIndex = instructionList.FindIndex(instruction => instruction.opcode == OpCodes.Leave);
                while (usingFinishIndex > 0)
                {
                    instructionList.InsertRange(usingFinishIndex, new[]
                    {
                        // Load instance of ChaFileCoordinate
                        new CodeInstruction(OpCodes.Ldarg_0),
                        // Load BinaryReader local var
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(KKChaFileCoordinateLoadHook), AccessTools.all))

                    });

                    usingFinishIndex = instructionList.FindIndex(usingFinishIndex + 4, instruction => instruction.opcode == OpCodes.Leave);
                }

                return instructionList;
            }

            private static void KKChaFileCoordinateLoadHook(KoikatsuCharaFile.ChaFileCoordinate coordinate, BinaryReader br)
            {
                var originalPosition = br.BaseStream.Position;
                try
                {
                    var marker = br.ReadString();
                    var version = br.ReadInt32();
                    var length = br.ReadInt32();
                    if (marker == Marker && version == DataVersion && length > 0)
                    {
                        var bytes = br.ReadBytes(length);
                        var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);
                        _internalCoordinateImportDictionary.Set(coordinate, dictionary);
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
                catch (InvalidOperationException)
                {
                    /* Invalid/unexpected deserialized data */
                    br.BaseStream.Position = originalPosition;
                }
            }

            #endregion

            #region HEditData

            [HarmonyPostfix, HarmonyPatch(typeof(HEditData), nameof(HEditData.Load), typeof(BinaryReader), typeof(int), typeof(YS_Node.NodeControl), typeof(HEditData.InfoData), typeof(bool))]
            private static bool HEditDataLoadHook(bool __result, HEditData __instance, ref BinaryReader _reader)
            {
                var originalPosition = _reader.BaseStream.Position;
                try
                {
                    var marker = _reader.ReadString();
                    var version = _reader.ReadInt32();
                    var length = _reader.ReadInt32();
                    if (marker == Marker && version == DataVersion && length > 0)
                    {
                        var bytes = _reader.ReadBytes(length);
                        var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);
                        _internalHEditDataDictionary.Set(__instance, dictionary);
                    }
                    else
                    {
                        _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                        _reader.BaseStream.Position = originalPosition;
                    }
                }
                catch (EndOfStreamException)
                {
                    /* Incomplete/non-existant data */
                    _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    _reader.BaseStream.Position = originalPosition;
                }
                catch (InvalidOperationException)
                {
                    /* Invalid/unexpected deserialized data */
                    _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    _reader.BaseStream.Position = originalPosition;
                }

                HEditDataReadEvent(__instance);

                return __result;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(HEditData), nameof(HEditData.Save), typeof(BinaryWriter), typeof(YS_Node.NodeControl), typeof(bool))]
            private static bool HEditDataSaveHook(bool __result, HEditData __instance, ref BinaryWriter _writer)
            {
                HEditDataWriteEvent(__instance);

                Logger.Log(LogLevel.Debug, "MapInfo hook!");

                var extendedData = GetAllExtendedData(__instance);
                if (extendedData == null || extendedData.Count == 0)
                    return __result;

                var originalLength = _writer.BaseStream.Length;
                var originalPosition = _writer.BaseStream.Position;
                try
                {
                    var bytes = MessagePackSerialize(extendedData);

                    _writer.Write(Marker);
                    _writer.Write(DataVersion);
                    _writer.Write(bytes.Length);
                    _writer.Write(bytes);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"Failed to save extended data in card. {e.Message}");
                    _writer.BaseStream.Position = originalPosition;
                    _writer.BaseStream.SetLength(originalLength);
                }

                return __result;
            }

            #endregion

            #region Import Chara ExtendedSaveData
            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFile), nameof(ConvertChaFile.ConvertCharaFile))]
            private static void ConvertCharaFile(ChaFileControl cfc, KoikatsuCharaFile.ChaFile kkfile)
            {
                #region Face
                var face = cfc.custom.face;
                var face2 = kkfile.custom.face;
                face.TransferSerializedExtendedData(face2);
                for (var i = 0; i < face.pupil.Length; i++)
                {
                    face.pupil[i].TransferSerializedExtendedData(face2.pupil[i]);
                }
                #endregion

                #region Body
                cfc.custom.body.TransferSerializedExtendedData(kkfile.custom.body);
                #endregion

                #region Hair
                var hair = cfc.custom.hair;
                var hair2 = kkfile.custom.hair;
                hair.TransferSerializedExtendedData(hair2);
                for (var i = 0; i < hair.parts.Length; i++)
                {
                    hair.parts[i].TransferSerializedExtendedData(hair2.parts[i]);
                }
                #endregion

                #region Parameters
                cfc.parameter.TransferSerializedExtendedData(kkfile.parameter);
                #endregion
            }
            #endregion

            #region Import Coordinate ExtendedSaveData
            [HarmonyPrefix, HarmonyPatch(typeof(ConvertChaFile), nameof(ConvertChaFile.ConvertCoordinate))]
            private static void ConvertCoordinatePrefix(ChaFileCoordinate emcoorde, KoikatsuCharaFile.ChaFileCoordinate kkcoorde)
            {
                emcoorde.accessory.parts = new ChaFileAccessory.PartsInfo[kkcoorde.accessory.parts.Length];
                for (var i = 0; i < kkcoorde.accessory.parts.Length; i++)
                {
                    emcoorde.accessory.parts[i] = new ChaFileAccessory.PartsInfo();
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFile), nameof(ConvertChaFile.ConvertCoordinate))]
            private static void ConvertCoordinatePostfix(ChaFileCoordinate emcoorde, KoikatsuCharaFile.ChaFileCoordinate kkcoorde)
            {
                var clothes = emcoorde.clothes;
                var clothes2 = kkcoorde.clothes;

                #region ChaFileClothes
                clothes.TransferSerializedExtendedData(clothes2); //ChaFileClothes
                for (var i = 0; i < clothes.parts.Length - 1; i++)
                {
                    clothes.parts[i].TransferSerializedExtendedData(clothes2.parts[i]);//ChaFileClothes/PartsInfo
                    for (var j = 0; j < clothes.parts[i].colorInfo.Length; j++)
                    {
                        clothes.parts[i].colorInfo[j].TransferSerializedExtendedData(clothes2.parts[i].colorInfo[j]);//ChaFileClothes/PartsInfo/ColorInfo
                    }
                }

                var destinationshoe = 7;
                var sourceshoe = 8;
                clothes.parts[destinationshoe].TransferSerializedExtendedData(clothes2.parts[sourceshoe]);//(shoes) ChaFileClothes/PartsInfo
                for (var i = 0; i < clothes.parts[destinationshoe].colorInfo.Length; i++)
                {
                    clothes.parts[destinationshoe].colorInfo[i].TransferSerializedExtendedData(clothes2.parts[sourceshoe].colorInfo[i]);//(shoes) ChaFileClothes/PartsInfo/ColorInfo
                }
                #endregion

                #region Accessories
                var accessory = emcoorde.accessory;
                var accessory2 = kkcoorde.accessory;
                accessory.TransferSerializedExtendedData(accessory2);//ChaFileAccessory
                for (var i = 0; i < accessory.parts.Length; i++)
                {
                    accessory.parts[i].TransferSerializedExtendedData(accessory2.parts[i]);//ChaFileAccessory/PartsInfo
                }
                #endregion
            }
            #endregion
        }
    }
}