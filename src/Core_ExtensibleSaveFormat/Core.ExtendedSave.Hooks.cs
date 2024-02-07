#if !RG
using HarmonyLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
#if AI || HS2
using AIChara;
#endif
#else
using BepisPlugins;
using Chara;
using CharaCustom;
using HarmonyLib;
using Illusion.IO;
using Manager;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using BinaryReader = Il2CppSystem.IO.BinaryReader;
using BinaryWriter = Il2CppSystem.IO.BinaryWriter;
using FileAccess   = Il2CppSystem.IO.FileAccess;
using FileMode     = Il2CppSystem.IO.FileMode;
using FileStream   = Il2CppSystem.IO.FileStream;
using MemoryStream = Il2CppSystem.IO.MemoryStream;
using SeekOrigin   = Il2CppSystem.IO.SeekOrigin;
using Stream       = Il2CppSystem.IO.Stream;
using Version      = Il2CppSystem.Version;
using Object       = UnityEngine.Object;
using Scene        = UnityEngine.SceneManagement.Scene;
#endif

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
#if RG
        /// <inheritdoc/>
        public static CustomControl CustomControl { get; private set; }
#endif

        internal static partial class Hooks
        {
#if !RG
            private static bool cardReadEventCalled;
#else
            private static readonly string[] Markers = new[] { Marker, "RGEx" };

            private static Harmony harmony;
#endif

            internal static void InstallHooks()
            {
#if !RG
                var hi = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
#else
                harmony = HarmonyExtentions.CreateAndPatchAll(typeof(Hooks), GUID);
                if (!Constants.InsideStudio)
                    SceneManager.activeSceneChanged += (UnityAction<Scene, Scene>)((prevScene, nextScene) => ClearSceneData());
#endif

#if KK
                var vrType = AccessTools.TypeByName("VR.VRClassRoomCharaFile");
                if (vrType != null)
                {
                    var vrTarget = AccessTools.DeclaredMethod(vrType, "Start");
                    hi.Patch(original: vrTarget,
                        prefix: new HarmonyMethod(typeof(Hooks), nameof(CustomScenePreHook)),
                        postfix: new HarmonyMethod(typeof(Hooks), nameof(CustomScenePostHook)));
                }

                // Fix ext data getting lost in KK Party live mode. Not needed in KK.
                if (UnityEngine.Application.productName == BepisPlugins.Constants.GameProcessNameSteam)
                {
                    var t = typeof(LiveCharaSelectSprite)
                            .GetNestedType("<Start>c__AnonStorey0", AccessTools.allDeclared)
                            .GetNestedType("<Start>c__AnonStorey1", AccessTools.allDeclared)
                            .GetMethod("<>m__3", AccessTools.allDeclared);
                    hi.Patch(t, transpiler: new HarmonyMethod(typeof(Hooks), nameof(Hooks.PartyLiveCharaFixTpl)));
                }
#endif
            }

            #region ChaFile

            #region Loading
#if !RG
#if KK || KKS
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            private static void ChaFileLoadFilePreHook() => cardReadEventCalled = false;

            private static void ChaFileLoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
            {
                var info = header.SearchInfo(Marker);

                if (LoadEventsEnabled && info != null && info.version == DataVersion.ToString())
                {
                    long originalPosition = reader.BaseStream.Position;
                    long basePosition = originalPosition - header.lstInfo.Sum(x => x.size);

                    reader.BaseStream.Position = basePosition + info.pos;

                    var data = reader.ReadBytes((int)info.size);

                    reader.BaseStream.Position = originalPosition;

                    cardReadEventCalled = true;

                    try
                    {
                        var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(data);
                        internalCharaDictionary.Set(file, dictionary);
                    }
                    catch (Exception e)
                    {
                        internalCharaDictionary.Set(file, new Dictionary<string, PluginData>());
                        Logger.LogWarning($"Invalid or corrupted extended data in card \"{file.charaFileName}\" - {e.Message}");
                    }

                    CardReadEvent(file);
                }
                else
                {
                    internalCharaDictionary.Set(file, new Dictionary<string, PluginData>());
                }
            }
#else
            [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), nameof(CustomControl.InitializeUI))]
            private static void CharaCustomInitializeUIPostHook(CustomControl __instance) => CustomControl = __instance;

            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CharaCustom), nameof(CharaCustom.CharaCustom.OnDestroy))]
            private static void CharaCustomOnDestroyPostHook() => CustomControl = null;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(string), typeof(int), typeof(bool), typeof(bool))]
            private static bool ChaFileLoadFileHook(ref bool __result, ChaFile __instance, string path, int lang, bool noLoadPNG, bool noLoadStatus)
            {
                if (!File.Exists(path))
                {
                    __instance.lastLoadErrorCode = ChaFileDefine.LoadError_FileNotExist;
                    __result = false;
                }
                else
                {
                    __instance.CharaFileName = Path.GetFileName(path);
                    var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    try
                    {
                        __result = __instance.LoadFile(fileStream, lang, noLoadPNG, noLoadStatus);
                    }
                    finally
                    {
                        fileStream.Dispose();
                    }
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(Stream), typeof(int), typeof(bool), typeof(bool))]
            private static bool ChaFileLoadFileHook(ref bool __result, ChaFile __instance, Stream st, int lang, bool noLoadPNG, bool noLoadStatus)
            {
                var binaryReader = new BinaryReader(st);
                try
                {
                    __result = __instance.LoadFile(binaryReader, lang, noLoadPNG, noLoadStatus);
                }
                finally
                {
                    binaryReader.Dispose(false);
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
            private static bool ChaFileLoadFileHook(ref bool __result, ChaFile __instance, BinaryReader br, int lang, bool noLoadPNG, bool noLoadStatus)
            {
                try
                {
                    if (noLoadPNG)
                        PngFile.SkipPng(br);
                    else
                        __instance.PngData = PngFile.LoadPngBytes(br);
                    if (br.BaseStream.Length == br.BaseStream.Position)
                    {
                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_OnlyPNG;
                        __result = false;
                    }
                    else
                    {
                        __instance.LoadProductNo = br.ReadInt32();
                        if (__instance.LoadProductNo > 100)
                        {
                            __instance.lastLoadErrorCode = ChaFileDefine.LoadError_ProductNo;
                            __result = false;
                        }
                        else if (br.ReadString() != "【RG_Chara】")
                        {
                            __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Tag;
                            __result = false;
                        }
                        else
                        {
                            var loadVersion = new Version(br.ReadString());
                            __instance.LoadVersion = loadVersion;
                            if (loadVersion > ChaFileDefine.ChaFileVersion)
                            {
                                __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                __result = false;
                            }
                            else
                            {
                                __instance.Language = br.ReadInt32();
                                __instance.UserID = br.ReadString();
                                __instance.DataID = br.ReadString();
                                if (loadVersion == ChaFileDefine.ChaFileVersion)
                                    __instance.FacePngData = br.ReadBytes(br.ReadInt32());
                                var count = br.ReadInt32();
                                var blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(br.ReadBytes(count));
                                var blockDataLength = br.ReadInt64();
                                var blockDataStart = br.BaseStream.Position;
                                var info = blockHeader.SearchInfo(ChaFileCustom.BlockName);
                                if (info != null)
                                {
                                    var version = new Version(info.version);
                                    if (version > ChaFileDefine.ChaFileCustomVersion)
                                    {
                                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                    }
                                    else
                                    {
                                        br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                        var customBytes = br.ReadBytes((int)info.size);
                                        __instance.SetCustomBytes(customBytes, version);
                                    }
                                }
                                if ((info = blockHeader.SearchInfo(ChaFileCoordinate.BlockName)) != null)
                                {
                                    var version = new Version(info.version);
                                    if (version > ChaFileDefine.ChaFileCoordinateVersion)
                                    {
                                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                    }
                                    else
                                    {
                                        br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                        var coordinateBytes = br.ReadBytes((int)info.size);
                                        __instance.SetCoordinateBytes(coordinateBytes, version);
                                    }
                                }
                                if ((info = blockHeader.SearchInfo(ChaFileParameter.BlockName)) != null)
                                {
                                    if (new Version(info.version) > ChaFileDefine.ChaFileParameterVersion)
                                    {
                                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                    }
                                    else
                                    {
                                        br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                        var parameterBytes = br.ReadBytes((int)info.size);
                                        __instance.SetParameterBytes(parameterBytes);
                                    }
                                }
                                if ((info = blockHeader.SearchInfo(ChaFileGameInfo.BlockName)) != null)
                                {
                                    if (new Version(info.version) > ChaFileDefine.ChaFileGameInfoVersion)
                                    {
                                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                    }
                                    else
                                    {
                                        br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                        var gameInfoBytes = br.ReadBytes((int)info.size);
                                        __instance.SetGameInfoBytes(gameInfoBytes);
                                    }
                                }
                                if (!noLoadStatus &&
                                    (info = blockHeader.SearchInfo(ChaFileStatus.BlockName)) != null)
                                {
                                    if (new Version(info.version) > ChaFileDefine.ChaFileStatusVersion)
                                    {
                                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                    }
                                    else
                                    {
                                        br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                        var statusBytes = br.ReadBytes((int)info.size);
                                        __instance.SetStatusBytes(statusBytes);
                                    }
                                }

                                #region ExtendedData
                                Dictionary<string, PluginData> extendedData = null;
                                if (LoadEventsEnabled &&
                                    (info = blockHeader.SearchInfo(Markers)) != null &&
                                    info.version == DataVersion.ToString())
                                {
                                    br.BaseStream.Seek(blockDataStart + info.pos, SeekOrigin.Begin);
                                    var extendedDataBytes = br.ReadBytes((int)info.size);
                                    try
                                    {
                                        extendedData = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(extendedDataBytes);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.LogWarning($"Invalid or corrupted extended data in card \"{__instance.CharaFileName}\" - {e.Message}");
                                    }
                                }
                                internalCharaDictionary[__instance.Pointer] = extendedData ??= new();
                                if (CustomControl != null)
                                    internalCharaDictionary[CustomControl.chaFile.Pointer] = extendedData;
                                CardReadEvent(__instance);
                                #endregion

                                br.BaseStream.Seek(blockDataStart + blockDataLength, SeekOrigin.Begin);
                                __instance.lastLoadErrorCode = 0;
                                __result = true;
                            }
                        }
                    }
                }
                catch (Il2CppException ex)
                {
                    if (!typeof(EndOfStreamException).IsAssignableFrom(ex.GetTypeFromMessage()))
                        throw;
                    __instance.lastLoadErrorCode = ChaFileDefine.LoadError_ETC;
                    __result = false;
                }
                return false;
            }
#endif

#if !RG
#if KK || KKS
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            private static IEnumerable<CodeInstruction> ChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

                //get the index of the first searchinfo call
                int searchInfoIndex = newInstructionSet.FindIndex(instruction => CheckCallVirtName(instruction, "SearchInfo"));

                //get the index of the last seek call
                int lastSeekIndex = newInstructionSet.FindLastIndex(instruction => CheckCallVirtName(instruction, "Seek"));

                var blockHeaderLocalBuilder = newInstructionSet[searchInfoIndex - 2]; //get the localbuilder for the blockheader

                //insert our own hook right after the last seek
                newInstructionSet.InsertRange(lastSeekIndex + 2, //we insert AFTER the NEXT instruction, which is right before the try block exit
                    new[] {
                    new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                    new CodeInstruction(blockHeaderLocalBuilder.opcode, blockHeaderLocalBuilder.operand), //push the BlockHeader instance
                    new CodeInstruction(OpCodes.Ldarg_1), //push the binaryreader instance
                    new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileLoadFileHook), AccessTools.all)), //call our hook
                    });

                return newInstructionSet;
            }

#if KK || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(bool), typeof(bool))]
            private static void ChaFileLoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br)
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
            private static void ChaFileLoadFilePostHook(ChaFile __instance, bool __result)
#endif
            {
                if (!__result) return;

#if KK // Doesn't work in KKS because KK cards go through a different load path
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
                                if (!LoadEventsEnabled)
                                {
                                    br.BaseStream.Seek(length, SeekOrigin.Current);
                                }
                                else
                                {
                                    byte[] bytes = br.ReadBytes(length);
                                    var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                                    cardReadEventCalled = true;
                                    internalCharaDictionary.Set(__instance, dictionary);

                                    CardReadEvent(__instance);
                                }
                            }
                        }
                        else
                        {
                            br.BaseStream.Position = originalPosition;
                        }
                    }
                    catch (EndOfStreamException) { } //Incomplete/non-existant data
                    catch (SystemException) { } //Invalid/unexpected deserialized data
                }
#endif

                //If the event wasn't called at this point, it means the card doesn't contain any data, but we still need to call the even for consistency.
                if (cardReadEventCalled == false)
                {
                    internalCharaDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    CardReadEvent(__instance);
                }
            }
#endif
            #endregion

            #region Saving

#if !RG
            private static byte[] currentlySavingData = null;

#if KK || KKS
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            private static void ChaFileSaveFilePreHook(ChaFile __instance) => CardWriteEvent(__instance);

            private static void ChaFileSaveFileHook(ChaFile file, BlockHeader header, ref long[] array3)
            {
                Dictionary<string, PluginData> extendedData = GetAllExtendedData(file);
                if (extendedData == null)
                {
                    currentlySavingData = null;
                    return;
                }

                // Remove null entries
                foreach (var key in extendedData.Where(entry => entry.Value == null).Select(entry => entry.Key).ToArray())
                    extendedData.Remove(key);

                currentlySavingData = MessagePackSerializer.Serialize(extendedData);

                //get offset
                long offset = array3.Sum();
                long length = currentlySavingData.LongLength;

                //insert our custom data length at the end
                Array.Resize(ref array3, array3.Length + 1);
                array3[array3.Length - 1] = length;

                //add info about our data to the block header
                BlockHeader.Info info = new BlockHeader.Info
                {
                    name = Marker,
                    version = DataVersion.ToString(),
                    pos = offset,
                    size = length
                };

                header.lstInfo.Add(info);
            }
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(string), typeof(int))]
            private static bool ChaFileSaveFileHook(ref bool __result, ChaFile __instance, string path, int lang)
            {
                var directoryName = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                __instance.CharaFileName = Path.GetFileName(path);
                var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                try
                {
                    __result = __instance.SaveFile(fileStream, true, lang);
                }
                finally
                {
                    fileStream.Dispose();
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(Stream), typeof(bool), typeof(int))]
            private static bool ChaFileSaveFileHook(ref bool __result, ChaFile __instance, Stream st, bool savePng, int lang)
            {
                var binaryWriter = new BinaryWriter(st);
                try
                {
                    __result = __instance.SaveFile(binaryWriter, savePng, lang);
                }
                finally
                {
                    binaryWriter.Dispose(false);
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool), typeof(int))]
            private static bool ChaFileSaveFileHook(ref bool __result, ChaFile __instance, BinaryWriter bw, bool savePng, int lang)
            {
                CardWriteEvent(__instance);

                if (savePng && __instance.PngData != null)
                    bw.Write(__instance.PngData);

                bw.Write(100);
                bw.Write("【RG_Chara】");
                bw.Write(ChaFileDefine.ChaFileVersion.ToString());
                bw.Write(lang);
                bw.Write(__instance.UserID);
                bw.Write(__instance.DataID);

                if (savePng && __instance.FacePngData != null)
                {
                    bw.Write(__instance.FacePngData.Length);
                    bw.Write(__instance.FacePngData);
                }
                else
                {
                    bw.Write(0);
                }

                byte[] customBytes = __instance.GetCustomBytes();
                byte[] coordinateBytes = __instance.GetCoordinateBytes();
                byte[] parameterBytes = __instance.GetParameterBytes();
                byte[] gameInfoBytes = __instance.GetGameInfoBytes();
                byte[] statusBytes = __instance.GetStatusBytes();

                const int lstInfoCount = 5;
                long pos = 0;

                var names = new string[lstInfoCount]
                {
                    ChaFileCustom.BlockName,
                    ChaFileCoordinate.BlockName,
                    ChaFileParameter.BlockName,
                    ChaFileGameInfo.BlockName,
                    ChaFileStatus.BlockName,
                };

                var versions = new string[lstInfoCount]
                {
                    ChaFileDefine.ChaFileCustomVersion.ToString(),
                    ChaFileDefine.ChaFileCoordinateVersion.ToString(),
                    ChaFileDefine.ChaFileParameterVersion.ToString(),
                    ChaFileDefine.ChaFileGameInfoVersion.ToString(),
                    ChaFileDefine.ChaFileStatusVersion.ToString(),
                };

                var sizes = new long[lstInfoCount]
                {
                    customBytes?.LongLength ?? 0,
                    coordinateBytes?.LongLength ?? 0,
                    parameterBytes?.LongLength ?? 0,
                    gameInfoBytes?.LongLength ?? 0,
                    statusBytes?.LongLength ?? 0,
                };

                var blockHeader = new BlockHeader();
                for (int i = 0; i < lstInfoCount; i++)
                {
                    var size = sizes[i];
                    blockHeader.lstInfo.Add(new BlockHeader.Info
                    {
                        name = names[i],
                        version = versions[i],
                        pos = pos,
                        size = size
                    });
                    pos += size;
                }

                #region ExtendedData
                byte[] extendedDataBytes = null;
                var extendedData = GetAllExtendedData(__instance);
                if (extendedData != null)
                {
                    // Remove null entries
                    foreach (var key in extendedData.Where(entry => entry.Value == null).Select(entry => entry.Key).ToArray())
                        extendedData.Remove(key);

                    extendedDataBytes = MessagePackSerializer.Serialize(extendedData);

                    // add info about our data to the block header
                    var size = extendedDataBytes.LongLength;
                    blockHeader.lstInfo.Add(new BlockHeader.Info
                    {
                        name = Marker,
                        version = DataVersion.ToString(),
                        pos = pos,
                        size = size
                    });
                    pos += size;
                }
                #endregion

                var blockHeaderBytes = MessagePackSerializer.Serialize(blockHeader);
                bw.Write(blockHeaderBytes.Length);
                bw.Write(blockHeaderBytes);

                bw.Write(pos);
                bw.Write(customBytes);
                bw.Write(coordinateBytes);
                bw.Write(parameterBytes);
                bw.Write(gameInfoBytes);
                bw.Write(statusBytes);

                #region ExtendedData
                if (extendedDataBytes != null)
                {
                    bw.Write(extendedDataBytes);
                }
                #endregion

                __result = true;
                return false;
            }
#endif

#if !RG
#if KK || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            private static void ChaFileSaveFilePostHook(bool __result, BinaryWriter bw)
            {
                if (!__result || currentlySavingData == null)
                    return;

                bw.Write(currentlySavingData);
            }
#endif

#if !RG
#if KK || KKS
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SaveFile), typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            private static IEnumerable<CodeInstruction> ChaFileSaveFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

#if AI || HS2
                string blockHeader = "AIChara.BlockHeader";
#else
                string blockHeader = "BlockHeader";
#endif

                //get the index of the last blockheader creation
                int blockHeaderIndex = newInstructionSet.FindLastIndex(instruction => CheckNewObjTypeName(instruction, blockHeader));

                //get the index of array3 (which contains data length info)
                int array3Index = newInstructionSet.FindIndex(instruction =>
                {
                    //find first int64 array
                    return instruction.opcode == OpCodes.Newarr &&
                               instruction.operand.ToString() == "System.Int64";
                });

                LocalBuilder blockHeaderLocalBuilder = (LocalBuilder)newInstructionSet[blockHeaderIndex + 1].operand; //get the local index for the block header
                LocalBuilder array3LocalBuilder = (LocalBuilder)newInstructionSet[array3Index + 1].operand; //get the local index for array3

                //insert our own hook right after the blockheader creation
                newInstructionSet.InsertRange(blockHeaderIndex + 2, //we insert AFTER the NEXT instruction, which is the store local for the blockheader
                    new[] {
                        new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                        new CodeInstruction(OpCodes.Ldloc_S, blockHeaderLocalBuilder), //push the BlockHeader instance
                        new CodeInstruction(OpCodes.Ldloca_S, array3LocalBuilder), //push the array3 instance as ref
                        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileSaveFileHook), AccessTools.all)), //call our hook
                    });

                return newInstructionSet;
            }
#endif

            #endregion

            #endregion

            #region ChaFileCoordinate

            #region Loading

#if !RG
#if KK || KKS
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(Stream))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(Stream), typeof(int))]
#endif
            private static IEnumerable<CodeInstruction> ChaFileCoordinateLoadTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
#if HS2 || KKS
                    if (set == false && inst.opcode == OpCodes.Ldc_I4_1 && instructionsList[i + 1].opcode == OpCodes.Stloc_3 && (instructionsList[i + 2].opcode == OpCodes.Leave || instructionsList[i + 2].opcode == OpCodes.Leave_S))
#else
                    if (set == false && inst.opcode == OpCodes.Ldc_I4_1 && instructionsList[i + 1].opcode == OpCodes.Stloc_1 && (instructionsList[i + 2].opcode == OpCodes.Leave || instructionsList[i + 2].opcode == OpCodes.Leave_S))
#endif
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateLoadHook), AccessTools.all));
                        set = true;
                    }

                    yield return inst;
                }
                if (!set) throw new InvalidOperationException("Didn't find any matches");
            }

            private static void ChaFileCoordinateLoadHook(ChaFileCoordinate coordinate, BinaryReader br)
            {
                try
                {
                    string marker = br.ReadString();
                    int version = br.ReadInt32();

                    int length = br.ReadInt32();

                    if (marker == Marker && version == DataVersion && length > 0)
                    {
                        if (!LoadEventsEnabled)
                        {
                            br.BaseStream.Seek(length, SeekOrigin.Current);
                            internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>());
                        }
                        else
                        {
                            var bytes = br.ReadBytes(length);
                            var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                            internalCoordinateDictionary.Set(coordinate, dictionary);
                        }
                    }
                    else
                        internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>()); //Overriding with empty data just in case there is some remnant from former loads.

                }
                catch (EndOfStreamException)
                {
                    // Incomplete/non-existant data
                    internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>());
                }
                catch (InvalidOperationException)
                {
                    // Invalid/unexpected deserialized data
                    internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>());
                }

                // Firing the event in any case
                CoordinateReadEvent(coordinate);
            }
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(TextAsset))]
            private static bool ChaFileCoordinateLoadFileHook(ref bool __result, ChaFileCoordinate __instance, TextAsset ta)
            {
                var memoryStream = new MemoryStream(ta.bytes);
                try
                {
                    __result = __instance.LoadFile(memoryStream, (int)GameSystem.Language);
                }
                finally
                {
                    memoryStream.Dispose();
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
            private static bool ChaFileCoordinateLoadFileHook(ref bool __result, ChaFileCoordinate __instance, string path, bool clothes, bool accessory, bool hair, bool skipPng)
            {
                if (!File.Exists(path))
                {
                    __instance.lastLoadErrorCode = ChaFileDefine.LoadError_FileNotExist;
                    __result = false;
                }
                else
                {
                    __instance.coordinateFileName = Path.GetFileName(path);
                    var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    try
                    {
                        __result = __instance.LoadFile(fileStream, (int)GameSystem.Language, clothes, accessory, hair, skipPng);
                    }
                    finally
                    {
                        fileStream.Dispose();
                    }
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(Stream), typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
            private static bool ChaFileCoordinateLoadFileHook(ref bool __result, ChaFileCoordinate __instance, Stream st, int lang, bool clothes, bool accessory, bool hair, bool skipPng)
            {
                var br = new BinaryReader(st);
                try
                {
                    if (skipPng)
                        PngFile.SkipPng(br);
                    else
                        __instance.pngData = PngFile.LoadPngBytes(br);
                    if (br.BaseStream.Length == br.BaseStream.Position)
                    {
                        __instance.lastLoadErrorCode = ChaFileDefine.LoadError_OnlyPNG;
                        __result = false;
                    }
                    else
                    {
                        __instance.loadProductNo = br.ReadInt32();
                        if (__instance.loadProductNo > 100)
                        {
                            __instance.lastLoadErrorCode = ChaFileDefine.LoadError_ProductNo;
                            __result = false;
                        }
                        else if (br.ReadString() != "【RG_Clothes】")
                        {
                            __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Tag;
                            __result = false;
                        }
                        else
                        {
                            var loadVersion = new Version(br.ReadString());
                            __instance.loadVersion = loadVersion;
                            if (loadVersion > ChaFileDefine.ChaFileCoordinateVersion)
                            {
                                __instance.lastLoadErrorCode = ChaFileDefine.LoadError_Version;
                                __result = false;
                            }
                            else
                            {
                                __instance.language = br.ReadInt32();
                                __instance.coordinateName = br.ReadString();
                                var count = br.ReadInt32();
                                var data = br.ReadBytes(count);
                                if (__instance.LoadBytes(data, loadVersion, clothes, accessory, hair))
                                {
                                    #region ExtendedData
                                    Dictionary<string, PluginData> extendedData = null;
                                    try
                                    {
                                        var marker = br.ReadString();
                                        var version = br.ReadInt32();
                                        var length = br.ReadInt32();
                                        if (Markers.Contains(marker) && version == DataVersion && length > 0)
                                        {
                                            if (!LoadEventsEnabled)
                                            {
                                                br.BaseStream.Seek(length, SeekOrigin.Current);
                                            }
                                            else
                                            {
                                                var bytes = br.ReadBytes(length);
                                                extendedData = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
                                            }
                                        }
                                    }
                                    catch (Il2CppException ex)
                                    {
                                        if (!typeof(EndOfStreamException).IsAssignableFrom(ex.GetTypeFromMessage()))
                                            throw;
                                        // Incomplete/non-existant data
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Invalid/unexpected deserialized data
                                    }

                                    // Overriding with empty data just in case there is some remnant from former loads.
                                    internalCoordinateDictionary[__instance.Pointer] = extendedData ?? new();

                                    // Firing the event in any case
                                    CoordinateReadEvent(__instance);
                                    #endregion

                                    __instance.lastLoadErrorCode = 0;
                                    __result = true;
                                }
                                else
                                {
                                    __instance.lastLoadErrorCode = ChaFileDefine.LoadError_ETC;
                                    __result = false;
                                }
                            }
                        }
                    }
                }
                catch (Il2CppException ex)
                {
                    if (!typeof(EndOfStreamException).IsAssignableFrom(ex.GetTypeFromMessage()))
                        throw;
                    __instance.lastLoadErrorCode = ChaFileDefine.LoadError_ETC;
                    __result = false;
                }
                finally
                {
                    br.Dispose(false);
                }
                return false;
            }
#endif

            #endregion

            #region Saving

#if !RG
#if KK || KKS
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string), typeof(int))]
#endif
            private static IEnumerable<CodeInstruction> ChaFileCoordinateSaveTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool hooked = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;

                    //find the end of the using(BinaryWriter) block
                    if (!hooked && inst.opcode == OpCodes.Callvirt && (instructionsList[i + 1].opcode == OpCodes.Leave || instructionsList[i + 1].opcode == OpCodes.Leave_S))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); //push the ChaFileInstance
                        yield return new CodeInstruction(instructionsList[i - 2]); //push the BinaryWriter (copying the instruction to do so)
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateSaveHook), AccessTools.all)); //call our hook
                        hooked = true;
                    }
                }
                if (!hooked) throw new InvalidOperationException("Didn't find any matches");
            }
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string), typeof(int))]
            private static bool ChaFileCoordinateSaveFileHook(ChaFileCoordinate __instance, string path, int lang)
            {
                var directoryName = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                __instance.coordinateFileName = Path.GetFileName(path);
                var st = new FileStream(path, FileMode.Create, FileAccess.Write);
                try
                {
                    var bw = new BinaryWriter(st);
                    try
                    {
                        if (__instance.pngData != null)
                        {
                            bw.Write(__instance.pngData);
                        }
                        bw.Write(100);
                        bw.Write("【RG_Clothes】");
                        bw.Write(ChaFileDefine.ChaFileCoordinateVersion.ToString());
                        bw.Write(lang);
                        bw.Write(__instance.coordinateName);
                        var array = __instance.SaveBytes();
                        bw.Write(array.Length);
                        bw.Write(array);

                        #region ExtendedData
                        ChaFileCoordinateSaveHook(__instance, bw);
                        #endregion
                    }
                    finally
                    {
                        bw.Dispose();
                    }
                }
                finally
                {
                    st.Dispose();
                }
                return false;
            }
#endif

            private static void ChaFileCoordinateSaveHook(ChaFileCoordinate file, BinaryWriter bw)
            {
                CoordinateWriteEvent(file);

                Logger.Log(BepInEx.Logging.LogLevel.Debug, "Coordinate hook!");

                Dictionary<string, PluginData> extendedData = GetAllExtendedData(file);
                if (extendedData == null)
                    return;

                // Remove null entries
                foreach (var key in extendedData.Where(entry => entry.Value == null).Select(entry => entry.Key).ToArray())
                    extendedData.Remove(key);

                byte[] data = MessagePackSerializer.Serialize(extendedData);

                bw.Write(Marker);
                bw.Write(DataVersion);
                bw.Write(data.Length);
                bw.Write(data);
            }

            #endregion

            #endregion

            #region Helper

#if !RG
            private static bool CheckCallVirtName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Callvirt &&
                       //need to do reflection fuckery here because we can't access MonoMethod which is the operand type, not MehtodInfo like normal reflection
                       instruction.operand.GetType().GetProperty("Name", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString().ToString() == name;

            private static bool CheckNewObjTypeName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Newobj &&
                       //need to do reflection fuckery here because we can't access MonoCMethod which is the operand type, not ConstructorInfo like normal reflection
                       instruction.operand.GetType().GetProperty("DeclaringType", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString() == name;
#endif

            #endregion

            #region Extended Data Override Hooks
#if EC || KK || KKS
            //Prevent loading extended data when loading the list of characters in Chara Maker since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), nameof(ChaCustom.CustomCharaFile.Initialize))]
            private static void CustomScenePreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), nameof(ChaCustom.CustomCharaFile.Initialize))]
            private static void CustomScenePostHook() => LoadEventsEnabled = true;
            //Prevent loading extended data when loading the list of coordinates in Chara Maker since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCoordinateFile), nameof(ChaCustom.CustomCoordinateFile.Initialize))]
            private static void CustomCoordinatePreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCoordinateFile), nameof(ChaCustom.CustomCoordinateFile.Initialize))]
            private static void CustomCoordinatePostHook() => LoadEventsEnabled = true;
#elif !RG
            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CustomCharaFileInfoAssist), nameof(CharaCustom.CustomCharaFileInfoAssist.AddList))]
            private static void LoadCharacterListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CustomCharaFileInfoAssist), nameof(CharaCustom.CustomCharaFileInfoAssist.AddList))]
            private static void LoadCharacterListPostfix() => LoadEventsEnabled = true;

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaLoad), nameof(CharaCustom.CvsO_CharaLoad.UpdateCharasList))]
            private static void CvsO_CharaLoadUpdateCharasListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaLoad), nameof(CharaCustom.CvsO_CharaLoad.UpdateCharasList))]
            private static void CvsO_CharaLoadUpdateCharasListPostfix() => LoadEventsEnabled = true;

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaSave), nameof(CharaCustom.CvsO_CharaSave.UpdateCharasList))]
            private static void CvsO_CharaSaveUpdateCharasListPrefix() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CvsO_CharaSave), nameof(CharaCustom.CvsO_CharaSave.UpdateCharasList))]
            private static void CvsO_CharaSaveUpdateCharasListPostfix() => LoadEventsEnabled = true;

#else
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.AddList))]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.AddList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(CvsO_CharaLoad), nameof(CvsO_CharaLoad.UpdateCharasList))]
            [HarmonyPatch(typeof(CvsO_CharaSave), nameof(CvsO_CharaSave.UpdateCharasList))]
            [HarmonyPatch(typeof(CvsC_ClothesLoad), nameof(CvsC_ClothesLoad.UpdateClothesList))]
            [HarmonyPatch(typeof(CvsC_ClothesSave), nameof(CvsC_ClothesSave.UpdateClothesList))]
            private static void AddListPrefix() => LoadEventsEnabled = false;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.AddList))]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.AddList))]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            [HarmonyPatch(typeof(CvsO_CharaLoad), nameof(CvsO_CharaLoad.UpdateCharasList))]
            [HarmonyPatch(typeof(CvsO_CharaSave), nameof(CvsO_CharaSave.UpdateCharasList))]
            [HarmonyPatch(typeof(CvsC_ClothesLoad), nameof(CvsC_ClothesLoad.UpdateClothesList))]
            [HarmonyPatch(typeof(CvsC_ClothesSave), nameof(CvsC_ClothesSave.UpdateClothesList))]
            private static void AddListPostfix() => LoadEventsEnabled = true;
#endif
            #endregion

#if KK
            private static IEnumerable<CodeInstruction> PartyLiveCharaFixTpl(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions).MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ChaFile), nameof(ChaFile.CopyCustom))))
                                                    .ThrowIfInvalid("CopyCustom not found")
                                                    .Advance(1)
                                                    .ThrowIfNotMatch("Ldloc_0 not found", new CodeMatch(OpCodes.Ldloc_0))
                                                    .Advance(1)
                                                    .Insert(new CodeInstruction(OpCodes.Dup),
                                                            new CodeInstruction(OpCodes.Ldarg_1),
                                                            CodeInstruction.Call(typeof(Hooks), nameof(Hooks.PartyLiveCharaFix)))
                                                    .Instructions();
            }
            private static void PartyLiveCharaFix(ChaFile target, SaveData.Heroine source)
            {
                // Copy ext data over to the new chafile
                var data = internalCharaDictionary.Get(source.charFile);
                if (data != null) internalCharaDictionary.Set(target, data);
            }
#endif

#if KK || EC || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileAccessory), nameof(ChaFileAccessory.MemberInit))]
            private static void MemberInit(ChaFileAccessory __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileAccessory.PartsInfo), nameof(ChaFileAccessory.PartsInfo.MemberInit))]
            private static void MemberInit(ChaFileAccessory.PartsInfo __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileClothes), nameof(ChaFileClothes.MemberInit))]
            private static void MemberInit(ChaFileClothes __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileClothes.PartsInfo), nameof(ChaFileClothes.PartsInfo.MemberInit))]
            private static void MemberInit(ChaFileClothes.PartsInfo __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileClothes.PartsInfo.ColorInfo), nameof(ChaFileClothes.PartsInfo.ColorInfo.MemberInit))]
            private static void MemberInit(ChaFileClothes.PartsInfo.ColorInfo __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.MemberInit))]
            private static void MemberInit(ChaFileStatus __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.Copy))]
            private static void Copy(ChaFileStatus __instance, ChaFileStatus src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter), nameof(ChaFileParameter.MemberInit))]
            private static void MemberInit(ChaFileParameter __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter), nameof(ChaFileParameter.Copy))]
            private static void Copy(ChaFileParameter __instance, ChaFileParameter src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileFace), nameof(ChaFileFace.MemberInit))]
            private static void MemberInit(ChaFileFace __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileFace.PupilInfo), nameof(ChaFileFace.PupilInfo.MemberInit))]
            private static void MemberInit(ChaFileFace.PupilInfo __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileFace.PupilInfo), nameof(ChaFileFace.PupilInfo.Copy))]
            private static void Copy(ChaFileFace.PupilInfo __instance, ChaFileFace.PupilInfo src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());
#endif

#if KK || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileMakeup), nameof(ChaFileMakeup.MemberInit))]
            private static void MemberInit(ChaFileMakeup __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Attribute), nameof(ChaFileStatus.MemberInit))]
            private static void MemberInit(ChaFileParameter.Attribute __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Awnser), nameof(ChaFileStatus.MemberInit))]
            private static void MemberInit(ChaFileParameter.Awnser __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Denial), nameof(ChaFileStatus.MemberInit))]
            private static void MemberInit(ChaFileParameter.Denial __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Attribute), nameof(ChaFileParameter.Attribute.Copy))]
            private static void Copy(ChaFileParameter.Attribute __instance, ChaFileParameter.Attribute src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Awnser), nameof(ChaFileParameter.Awnser.Copy))]
            private static void Copy(ChaFileParameter.Awnser __instance, ChaFileParameter.Awnser src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileParameter.Denial), nameof(ChaFileParameter.Denial.Copy))]
            private static void Copy(ChaFileParameter.Denial __instance, ChaFileParameter.Denial src) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(Traverse.Create(src).Property(ExtendedSaveDataPropertyName).GetValue());
#endif

#if EC
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileFace.ChaFileMakeup), nameof(ChaFileFace.ChaFileMakeup.MemberInit))]
            private static void MemberInit(ChaFileFace.ChaFileMakeup __instance) => Traverse.Create(__instance).Property(ExtendedSaveDataPropertyName).SetValue(null);
#endif

#if RG
            [HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.InitScene), new[] { typeof(bool) })]
            private static void ClearSceneData()
            {
                var chaFiles = ((ChaInfo[])Object.FindObjectsOfType<ChaInfo>()).Where(Object.IsNativeObjectAlive).Select(x => x.ChaFile).ToArray();
                var charaPointers = chaFiles.Select(x => x.Pointer).ToArray();
                var coordinatePointers = chaFiles.SelectMany(x => x.Coordinate).Select(x => x.Pointer).ToArray();

                internalCharaDictionary = internalCharaDictionary.Where(x => charaPointers.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                internalCoordinateDictionary = internalCoordinateDictionary.Where(x => coordinatePointers.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                internalSceneDictionary.Clear();
                internalPoseDictionary.Clear();
            }
#endif
        }
    }
}
