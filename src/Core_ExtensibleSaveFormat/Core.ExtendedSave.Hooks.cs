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

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            private static bool cardReadEventCalled;

            internal static void InstallHooks()
            {
                var hi = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

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
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            private static void ChaFileLoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br)
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
            #endregion

            #region Saving

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

                //Remove null entries
                List<string> keysToRemove = new List<string>();
                foreach (var entry in extendedData)
                    if (entry.Value == null)
                        keysToRemove.Add(entry.Key);
                foreach (var key in keysToRemove)
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

            #endregion

            #endregion

            #region ChaFileCoordinate

            #region Loading

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
                            byte[] bytes = br.ReadBytes(length);
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

                //Firing the event in any case
                CoordinateReadEvent(coordinate);
            }

            #endregion

            #region Saving

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

            private static void ChaFileCoordinateSaveHook(ChaFileCoordinate file, BinaryWriter bw)
            {
                CoordinateWriteEvent(file);

                Logger.Log(BepInEx.Logging.LogLevel.Debug, "Coordinate hook!");

                Dictionary<string, PluginData> extendedData = GetAllExtendedData(file);
                if (extendedData == null)
                    return;

                //Remove null entries
                List<string> keysToRemove = new List<string>();
                foreach (var entry in extendedData)
                    if (entry.Value == null)
                        keysToRemove.Add(entry.Key);
                foreach (var key in keysToRemove)
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

            private static bool CheckCallVirtName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Callvirt &&
                       //need to do reflection fuckery here because we can't access MonoMethod which is the operand type, not MehtodInfo like normal reflection
                       instruction.operand.GetType().GetProperty("Name", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString().ToString() == name;

            private static bool CheckNewObjTypeName(CodeInstruction instruction, string name) => instruction.opcode == OpCodes.Newobj &&
                       //need to do reflection fuckery here because we can't access MonoCMethod which is the operand type, not ConstructorInfo like normal reflection
                       instruction.operand.GetType().GetProperty("DeclaringType", AccessTools.all).GetGetMethod().Invoke(instruction.operand, null).ToString() == name;

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
#else
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
        }
    }
}