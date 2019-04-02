using ChaCustom;
using Harmony;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using Studio;

namespace ExtensibleSaveFormat
{
    public static class Hooks
    {
        public static string Marker = "KKEx";
        public static int Version = 3;

        private static bool cardReadEventCalled;

        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.extensiblesaveformat");
            harmony.PatchAll(typeof(Hooks));
            harmony.Patch(typeof(Studio.MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("InitFileList", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPreHook), BindingFlags.Static | BindingFlags.Public)),
                new HarmonyMethod(typeof(Hooks).GetMethod(nameof(StudioCoordinateListPostHook), BindingFlags.Static | BindingFlags.Public)));
        }


        #region ChaFile

        #region Loading
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
        public static void ChaFileLoadFilePreHook(ChaFile __instance, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
        {
            cardReadEventCalled = false;
        }

        public static void ChaFileLoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
        {
            var info = header.SearchInfo(Marker);

            if (info != null && info.version == Version.ToString())
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
                    ExtendedSave.internalCharaDictionary.Set(file, dictionary);
                }
                catch (Exception e)
                {
                    ExtendedSave.internalCharaDictionary.Set(file, new Dictionary<string, PluginData>());
                    BepInEx.Logger.Log(LogLevel.Warning, $"Invalid or corrupted extended data in card \"{file.charaFileName}\" - {e.Message}");
                }

                ExtendedSave.cardReadEvent(file);
            }
            else
            {
                ExtendedSave.internalCharaDictionary.Set(file, new Dictionary<string, PluginData>());
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
        public static IEnumerable<CodeInstruction> ChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
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
                    new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileLoadFileHook))), //call our hook
                });

            return newInstructionSet;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
        public static void ChaFileLoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
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
                            var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                            cardReadEventCalled = true;
                            ExtendedSave.internalCharaDictionary.Set(__instance, dictionary);

                            ExtendedSave.cardReadEvent(__instance);
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
                }
                catch (SystemException)
                {
                    /* Invalid/unexpected deserialized data */
                }
            }

            //If the event wasn't called at this point, it means the card doesn't contain any data, but we still need to call the even for consistency.
            if (cardReadEventCalled == false)
            {
                ExtendedSave.internalCharaDictionary.Set(__instance, new Dictionary<string, PluginData>());
                ExtendedSave.cardReadEvent(__instance);
            }
        }

        #endregion

        #region Saving

        private static byte[] currentlySavingData = null;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static void ChaFileSaveFilePreHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
        {
            ExtendedSave.cardWriteEvent(__instance);
        }

        public static void ChaFileSaveFileHook(ChaFile file, BlockHeader header, ref long[] array3)
        {
            Dictionary<string, PluginData> extendedData = ExtendedSave.GetAllExtendedData(file);
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
                version = Version.ToString(),
                pos = offset,
                size = length
            };

            header.lstInfo.Add(info);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static void ChaFileSaveFilePostHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
        {
            if (!__result || currentlySavingData == null)
                return;

            bw.Write(currentlySavingData);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static IEnumerable<CodeInstruction> ChaFileSaveFileTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

            //get the index of the last blockheader creation
            int blockHeaderIndex = newInstructionSet.FindLastIndex(instruction => CheckNewObjTypeName(instruction, "BlockHeader"));

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
                    new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileSaveFileHook))), //call our hook
	            });

            return newInstructionSet;
        }

        #endregion

        #endregion

        #region SceneInfo

        #region Loading

        [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), "Load", new[] { typeof(string), typeof(Version) }, new[] { 1 })]
        public static IEnumerable<CodeInstruction> SceneInfoLoadTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Stind_Ref && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoLoadHook)));
                    set = true;
                }
            }
        }

        public static void SceneInfoLoadHook(string path, BinaryReader br)
        {
            ExtendedSave.internalSceneDictionary.Clear();

            try
            {
                br.ReadString(); //Reading that useless string at the end "【KStudio】"

                string marker = br.ReadString();
                int version = br.ReadInt32();

                int length = br.ReadInt32();

                if (marker.Equals(Marker) && length > 0)
                {
                    byte[] bytes = br.ReadBytes(length);
                    ExtendedSave.internalSceneDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
                }
            }
            catch (EndOfStreamException)
            {
                /* Incomplete/non-existant data */
            }
            catch (InvalidOperationException)
            {
                /* Invalid/unexpected deserialized data */
            }

            ExtendedSave.sceneReadEvent(path);
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), "Import", new[] { typeof(string) })]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Call && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoImportHook)));
                    set = true;
                }
            }
        }

        public static void SceneInfoImportHook(string path, BinaryReader br, Version version)
        {
            //Reading useless data
            br.ReadInt32();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadInt32();
            br.ReadBoolean();
            br.ReadInt32();
            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
                br.ReadSingle();
            if (version.CompareTo(new Version(0, 0, 1)) <= 0)
            {
                br.ReadBoolean();
                br.ReadSingle();
                br.ReadString();
            }

            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
            {
                br.ReadBoolean();
                br.ReadString();
                br.ReadSingle();
            }

            br.ReadBoolean();
            br.ReadSingle();
            br.ReadSingle();
            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
                br.ReadSingle();
            if (version.CompareTo(new Version(0, 0, 1)) <= 0)
                br.ReadBoolean();
            br.ReadBoolean();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadBoolean();
            if (version.CompareTo(new Version(0, 0, 1)) <= 0)
                br.ReadSingle();
            br.ReadBoolean();
            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
            {
                br.ReadString();
                br.ReadSingle();
                br.ReadSingle();
            }

            br.ReadBoolean();
            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
            {
                br.ReadString();
                br.ReadString();
            }

            if (version.CompareTo(new Version(0, 0, 4)) >= 0)
                br.ReadInt32();
            if (version.CompareTo(new Version(0, 0, 2)) >= 0)
                br.ReadBoolean();
            if (version.CompareTo(new Version(0, 0, 4)) >= 0)
            {
                br.ReadBoolean();
                br.ReadBoolean();
                br.ReadSingle();
                br.ReadString();
            }

            if (version.CompareTo(new Version(0, 0, 5)) >= 0)
            {
                br.ReadSingle();
                br.ReadInt32();
                br.ReadSingle();
            }

            int num = br.ReadInt32();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            if (num == 1)
                br.ReadSingle();
            else
            {
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
            }

            br.ReadSingle();
            for (int j = 0; j < 10; j++)
            {
                num = br.ReadInt32();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                if (num == 1)
                    br.ReadSingle();
                else
                {
                    br.ReadSingle();
                    br.ReadSingle();
                    br.ReadSingle();
                }

                br.ReadSingle();
            }

            br.ReadString();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadBoolean();

            br.ReadString();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
            br.ReadBoolean();

            br.ReadInt32();
            br.ReadInt32();
            br.ReadBoolean();

            br.ReadInt32();
            br.ReadInt32();
            br.ReadBoolean();

            br.ReadInt32();
            br.ReadString();
            br.ReadBoolean();
            br.ReadString();
            br.ReadString();
            br.ReadString();
            br.ReadBytes(16);

            ExtendedSave.internalSceneDictionary.Clear();

            try
            {
                string marker = br.ReadString();
                int ver = br.ReadInt32();

                int length = br.ReadInt32();

                if (marker.Equals(Marker) && length > 0)
                {
                    byte[] bytes = br.ReadBytes(length);
                    ExtendedSave.internalSceneDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
                }
            }
            catch (EndOfStreamException)
            {
                /* Incomplete/non-existant data */
            }
            catch (InvalidOperationException)
            {
                /* Invalid/unexpected deserialized data */
            }

            ExtendedSave.sceneImportEvent(path);
        }

        #endregion

        #region Saving

        [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), "Save", new[] { typeof(string) })]
        public static IEnumerable<CodeInstruction> SceneInfoSaveTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Callvirt && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoSaveHook)));
                    set = true;
                }
            }
        }

        public static void SceneInfoSaveHook(string path, BinaryWriter bw)
        {
            ExtendedSave.sceneWriteEvent(path);

            Dictionary<string, PluginData> extendedData = ExtendedSave.internalSceneDictionary;
            if (extendedData == null)
                return;
            byte[] data = MessagePackSerializer.Serialize(extendedData);

            bw.Write(Marker);    //Not super useful
            bw.Write(Version);   //but kept for consistency
            bw.Write(data.Length);
            bw.Write(data);
        }

        #endregion

        #endregion

        #region ChaFileCoordinate

        #region Loading

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile), new[] { typeof(Stream) })]
        public static IEnumerable<CodeInstruction> ChaFileCoordinateLoadTranspiler(IEnumerable<CodeInstruction> instructions)
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
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateLoadHook)));
                    set = true;
                }

                yield return inst;
            }
        }

        public static void ChaFileCoordinateLoadHook(ChaFileCoordinate coordinate, BinaryReader br)
        {
            try
            {
                string marker = br.ReadString();
                int version = br.ReadInt32();

                int length = br.ReadInt32();

                if (marker == Marker && version == Version && length > 0)
                {
                    byte[] bytes = br.ReadBytes(length);
                    var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

                    ExtendedSave.internalCoordinateDictionary.Set(coordinate, dictionary);
                }
                else
                    ExtendedSave.internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>()); //Overriding with empty data just in case there is some remnant from former loads.

            }
            catch (EndOfStreamException)
            {
                /* Incomplete/non-existant data */
                ExtendedSave.internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>());
            }
            catch (InvalidOperationException)
            {
                /* Invalid/unexpected deserialized data */
                ExtendedSave.internalCoordinateDictionary.Set(coordinate, new Dictionary<string, PluginData>());
            }
            ExtendedSave.coordinateReadEvent(coordinate); //Firing the event in any case
        }

        #endregion

        #region Saving

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), new[] { typeof(string) })]
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
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(ChaFileCoordinateSaveHook))); //call our hook
                    hooked = true;
                }
            }
        }

        public static void ChaFileCoordinateSaveHook(ChaFileCoordinate file, BinaryWriter bw)
        {
            ExtendedSave.coordinateWriteEvent(file);

            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Coordinate hook!");

            Dictionary<string, PluginData> extendedData = ExtendedSave.GetAllExtendedData(file);
            if (extendedData == null)
                return;

            byte[] data = MessagePackSerializer.Serialize(extendedData);

            bw.Write(Marker);
            bw.Write(Version);
            bw.Write(data.Length);
            bw.Write(data);
        }

        #endregion

        #endregion

        #region Helper

        public static bool CheckCallVirtName(CodeInstruction instruction, string name)
        {
            return instruction.opcode == OpCodes.Callvirt &&
                   //need to do reflection fuckery here because we can't access MonoMethod which is the operand type, not MehtodInfo like normal reflection
                   instruction.operand.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod().Invoke(instruction.operand, null).ToString().ToString() == name;
        }

        public static bool CheckNewObjTypeName(CodeInstruction instruction, string name)
        {
            return instruction.opcode == OpCodes.Newobj &&
                   //need to do reflection fuckery here because we can't access MonoCMethod which is the operand type, not ConstructorInfo like normal reflection
                   instruction.operand.GetType().GetProperty("DeclaringType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod().Invoke(instruction.operand, null).ToString() == name;
        }

        #endregion

        #region Extended Data Override Hooks
        //Prevent loading extended data when loading the list of characters in Chara Maker since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePreHook() => ExtendedSave.LoadEventsEnabled = false;
        [HarmonyPostfix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePostHook() => ExtendedSave.LoadEventsEnabled = true;
        //Prevent loading extended data when loading the list of coordinates in Chara Maker since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
        public static void CustomCoordinatePreHook() => ExtendedSave.LoadEventsEnabled = false;

        [HarmonyPostfix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
        public static void CustomCoordinatePostHook() => ExtendedSave.LoadEventsEnabled = true;
        //Prevent loading extended data when loading the list of characters in Studio since it is irrelevant here
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void StudioFemaleListPreHook() => ExtendedSave.LoadEventsEnabled = false;

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void StudioFemaleListPostHook() => ExtendedSave.LoadEventsEnabled = true;
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitMaleList")]
        public static void StudioMaleListPreHook() => ExtendedSave.LoadEventsEnabled = false;
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitMaleList")]
        public static void StudioMaleListPostHook() => ExtendedSave.LoadEventsEnabled = true;

        //Prevent loading extended data when loading the list of coordinates in Studio since it is irrelevant here
        public static void StudioCoordinateListPreHook() => ExtendedSave.LoadEventsEnabled = false;
        public static void StudioCoordinateListPostHook() => ExtendedSave.LoadEventsEnabled = true;
        #endregion
    }
}