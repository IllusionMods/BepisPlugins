using BepInEx.Harmony;
using HarmonyLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if AI
using AIChara;
#endif

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            private static bool cardReadEventCalled;

            internal static void InstallHooks()
            {
                var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
#if KK
                harmony.Patch(typeof(Studio.MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("InitFileList", AccessTools.all),
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPreHook), BindingFlags.Static | BindingFlags.Public)),
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPostHook), BindingFlags.Static | BindingFlags.Public)));
#endif
            }

            #region ChaFile

            #region Loading
#if KK
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif

            internal static void ChaFileLoadFilePreHook() => cardReadEventCalled = false;

            private static void ChaFileLoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
            {
                var info = header.SearchInfo(Marker);

                if (info != null && info.version == DataVersion.ToString())
                {
                    long originalPosition = reader.BaseStream.Position;
                    long basePosition = originalPosition - header.lstInfo.Sum(x => x.size);

                    reader.BaseStream.Position = basePosition + info.pos;

                    byte[] data = reader.ReadBytes((int)info.size);

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

#if KK
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            internal static IEnumerable<CodeInstruction> ChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

                //get the index of the first searchinfo call
                int searchInfoIndex = newInstructionSet.FindIndex(instruction => CheckCallVirtName(instruction, "SearchInfo"));

                //get the index of the last seek call
                int lastSeekIndex = newInstructionSet.FindLastIndex(instruction => CheckCallVirtName(instruction, "Seek"));

                LocalBuilder blockHeaderLocalBuilder = (LocalBuilder)newInstructionSet[searchInfoIndex - 2].operand; //get the localbuilder for the blockheader

                //insert our own hook right after the last seek
                newInstructionSet.InsertRange(lastSeekIndex + 2, //we insert AFTER the NEXT instruction, which is right before the try block exit
                    new[] {
                    new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                    new CodeInstruction(OpCodes.Ldloc_S, blockHeaderLocalBuilder), //push the BlockHeader instance
                    new CodeInstruction(OpCodes.Ldarg_1, blockHeaderLocalBuilder), //push the binaryreader instance
                    new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileLoadFileHook), AccessTools.all)), //call our hook
                    });

                return newInstructionSet;
            }

#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            internal static void ChaFileLoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br)
            {
                if (!__result) return;

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
                                var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                                cardReadEventCalled = true;
                                internalCharaDictionary.Set(__instance, dictionary);

                                CardReadEvent(__instance);
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

                //If the event wasn't called at this point, it means the card doesn't contain any data, but we still need to call the even for consistency.
                if (cardReadEventCalled == false)
                {
                    internalCharaDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    CardReadEvent(__instance);
                }
            }
            #endregion

            #region Saving

            private static byte[] currentlySavingData = null;

#if KK
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            internal static void ChaFileSaveFilePreHook(ChaFile __instance) => CardWriteEvent(__instance);

            private static void ChaFileSaveFileHook(ChaFile file, BlockHeader header, ref long[] array3)
            {
                Dictionary<string, PluginData> extendedData = GetAllExtendedData(file);
                if (extendedData == null)
                {
                    currentlySavingData = null;
                    return;
                }

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

#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            internal static void ChaFileSaveFilePostHook(bool __result, BinaryWriter bw)
            {
                if (!__result || currentlySavingData == null)
                    return;

                bw.Write(currentlySavingData);
            }

#if KK
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
            internal static IEnumerable<CodeInstruction> ChaFileSaveFileTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

#if AI
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

            #endregion

            #endregion

            #region ChaFileCoordinate

            #region Loading

#if KK
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(Stream))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), typeof(Stream), typeof(int))]
#endif
            internal static IEnumerable<CodeInstruction> ChaFileCoordinateLoadTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldc_I4_1 && instructionsList[i + 1].opcode == OpCodes.Stloc_1 && instructionsList[i + 2].opcode == OpCodes.Leave)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateLoadHook), AccessTools.all));
                        set = true;
                    }

                    yield return inst;
                }
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
                        byte[] bytes = br.ReadBytes(length);
                        var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                        internalCoordinateDictionary.Set(coordinate, dictionary);
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

                //Firing the event in any case
                CoordinateReadEvent(coordinate);
            }

            #endregion

            #region Saving

#if KK
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string))]
#else
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string), typeof(int))]
#endif
            public static IEnumerable<CodeInstruction> ChaFileCoordinateSaveTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool hooked = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (!hooked && inst.opcode == OpCodes.Callvirt && instructionsList[i + 1].opcode == OpCodes.Leave) //find the end of the using(BinaryWriter) block
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); //push the ChaFileInstance
                        yield return new CodeInstruction(instructionsList[i - 2]); //push the BinaryWriter (copying the instruction to do so)
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateSaveHook), AccessTools.all)); //call our hook
                        hooked = true;
                    }
                }
            }

            private static void ChaFileCoordinateSaveHook(ChaFileCoordinate file, BinaryWriter bw)
            {
                CoordinateWriteEvent(file);

                Logger.Log(BepInEx.Logging.LogLevel.Debug, "Coordinate hook!");

                Dictionary<string, PluginData> extendedData = GetAllExtendedData(file);
                if (extendedData == null)
                    return;

                byte[] data = MessagePackSerializer.Serialize(extendedData);

                bw.Write(Marker);
                bw.Write(DataVersion);
                bw.Write(data.Length);
                bw.Write(data);
            }

            #endregion

            #endregion

            #region Helper

            private static bool CheckCallVirtName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Callvirt &&
                       //need to do reflection fuckery here because we can't access MonoMethod which is the operand type, not MehtodInfo like normal reflection
                       instruction.operand.GetType().GetProperty("Name", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString().ToString() == name;

            private static bool CheckNewObjTypeName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Newobj &&
                       //need to do reflection fuckery here because we can't access MonoCMethod which is the operand type, not ConstructorInfo like normal reflection
                       instruction.operand.GetType().GetProperty("DeclaringType", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString() == name;

            #endregion

            #region Extended Data Override Hooks
#if EC || KK
            //Prevent loading extended data when loading the list of characters in Chara Maker since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), "Initialize")]
            internal static void CustomScenePreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), "Initialize")]
            internal static void CustomScenePostHook() => LoadEventsEnabled = true;
            //Prevent loading extended data when loading the list of coordinates in Chara Maker since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCoordinateFile), "Initialize")]
            internal static void CustomCoordinatePreHook() => LoadEventsEnabled = false;
#endif
            #endregion
        }
    }
}