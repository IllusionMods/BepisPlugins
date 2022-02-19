using HarmonyLib;
using MessagePack;
using Studio;
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
            #region SceneInfo

            #region Loading

            [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Load),
                new Type[] { typeof(string), typeof(Version) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
            private static IEnumerable<CodeInstruction> SceneInfoLoadTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;

#if PH
                    if (set == false && inst.opcode == OpCodes.Stfld && instructionsList[i + 1].opcode == OpCodes.Leave)
#else
                    if (set == false && inst.opcode == OpCodes.Stind_Ref && (instructionsList[i + 1].opcode == OpCodes.Leave || instructionsList[i + 1].opcode == OpCodes.Leave_S))
#endif
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoLoadHook)));
                        set = true;
                    }
                }

                if (!set)
                    throw new Exception("Failed to patch SceneInfo.Load");
            }

            public static void SceneInfoLoadHook(string path, BinaryReader br)
            {
                internalSceneDictionary.Clear();

                try
                {
                    br.ReadString(); //Reading that useless string at the end "【KStudio】"

                    string marker = br.ReadString();
                    int version = br.ReadInt32();

                    int length = br.ReadInt32();

                    if (marker.Equals(Marker) && length > 0)
                    {
                        byte[] bytes = br.ReadBytes(length);
                        internalSceneDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
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

                SceneReadEvent(path);
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Import), typeof(string))]
            private static IEnumerable<CodeInstruction> SceneInfoImportTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;

                    if (set == false && inst.opcode == OpCodes.Call && (instructionsList[i + 1].opcode == OpCodes.Leave || instructionsList[i + 1].opcode == OpCodes.Leave_S))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoImportHook)));
                        set = true;
                    }
                }

                if (!set)
                    throw new Exception("Failed to patch SceneInfo.Import");
            }

            public static void SceneInfoImportHook(string path, BinaryReader br, Version _)
            {
                internalSceneDictionary.Clear();

                while (true)
                {
                    try
                    {
                        // -1 to go back to the length specifier of the string
                        var result = br.BaseStream.FindPosition(Marker.Select(c => (byte)c).ToArray()) - 1;
                        if (result < 0) break;

                        br.BaseStream.Position = result;

                        string marker = br.ReadString();
                        int ver = br.ReadInt32();
                        int length = br.ReadInt32();

                        if (marker.Equals(Marker) && length > 0)
                        {
                            byte[] bytes = br.ReadBytes(length);
                            internalSceneDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        /* Incomplete/non-existant data */
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        /* Invalid/unexpected deserialized data */
                        // Keep looking for the extended data until we hit the end of the stream in case the marker happens to appear in the data randomly
                    }
                }

                SceneImportEvent(path);
            }

            #endregion

            #region Saving

            [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), typeof(string))]
            private static IEnumerable<CodeInstruction> SceneInfoSaveTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set == false && inst.opcode == OpCodes.Callvirt && (instructionsList[i + 1].opcode == OpCodes.Leave || instructionsList[i + 1].opcode == OpCodes.Leave_S))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SceneInfoSaveHook)));
                        set = true;
                    }
                }

                if (!set)
                    throw new Exception("Failed to patch SceneInfo.Save");
            }

            public static void SceneInfoSaveHook(string path, BinaryWriter bw)
            {
                SceneWriteEvent(path);

                Dictionary<string, PluginData> extendedData = internalSceneDictionary;
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

            #region Pose
            private static OCIChar PoseChar;
            private static string PoseName;
            // Use a single data maker since poses are compatible across games and PH marker is different for some reason.
            private const string PoseMarker = "KKex";

            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Load))]
            private static void PauseCtrl_Load(OCIChar _ociChar, string _path)
            {
                PoseChar = _ociChar;
                PoseName = _path;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Load))]
            private static void PauseCtrl_FileInfo_Load(BinaryReader _reader, PauseCtrl.FileInfo __instance)
            {
                PoseLoadHook(_reader, __instance);
            }

            private static void PoseLoadHook(BinaryReader br, PauseCtrl.FileInfo fileInfo)
            {
                internalPoseDictionary.Clear();

                try
                {
                    string marker = br.ReadString();
                    int version = br.ReadInt32();
                    int length = br.ReadInt32();

                    if (marker.Equals(PoseMarker) && length > 0)
                    {
                        byte[] bytes = br.ReadBytes(length);
                        internalPoseDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
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

                var data = GetPoseExtendedDataById(GUID);
                var gameName = GameNames.Unknown;
                if (data != null && data.data.TryGetValue("gameName", out var loadedData) && loadedData != null)
                    gameName = (GameNames)loadedData;

                PoseReadEvent(PoseName, fileInfo, PoseChar, gameName);
            }


            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Save))]
            private static void PauseCtrl_Save(OCIChar _ociChar, string _name)
            {
                PoseChar = _ociChar;
                PoseName = _name;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Save))]
            private static void PauseCtrl_FileInfo_Save(BinaryWriter _writer, PauseCtrl.FileInfo __instance)
            {
                PoseSaveHook(_writer, __instance);
            }

            private static void PoseSaveHook(BinaryWriter bw, PauseCtrl.FileInfo fileInfo)
            {
                PoseWriteEvent(PoseName, fileInfo, PoseChar, GameName);

                var gameNameData = new PluginData();
                gameNameData.data.Add("gameName", GameName);
                SetPoseExtendedDataById(GUID, gameNameData);

                Dictionary<string, PluginData> extendedData = internalPoseDictionary;
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

                bw.Write(PoseMarker);
                bw.Write(DataVersion);
                bw.Write(data.Length);
                bw.Write(data);
            }

            #endregion

            #region Extended Data Override Hooks
            //Prevent loading extended data when loading the list of characters in Studio since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
            private static void StudioFemaleListPreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
            private static void StudioFemaleListPostHook() => LoadEventsEnabled = true;
            [HarmonyPrefix, HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
            private static void StudioMaleListPreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
            private static void StudioMaleListPostHook() => LoadEventsEnabled = true;

            //Prevent loading extended data when loading the list of coordinates in Studio since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(MPCharCtrl.CostumeInfo), nameof(MPCharCtrl.CostumeInfo.InitFileList))]
            private static void StudioCoordinateListPreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl.CostumeInfo), nameof(MPCharCtrl.CostumeInfo.InitFileList))]
            private static void StudioCoordinateListPostHook() => LoadEventsEnabled = true;
            #endregion
        }
    }
}