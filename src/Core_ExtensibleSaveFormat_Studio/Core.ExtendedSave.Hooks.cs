#if !RG
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
#else
using HarmonyLib;
using Illusion.IO;
using MessagePack;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BinaryReader = Il2CppSystem.IO.BinaryReader;
using BinaryWriter = Il2CppSystem.IO.BinaryWriter;
using FileAccess   = Il2CppSystem.IO.FileAccess;
using FileMode     = Il2CppSystem.IO.FileMode;
using FileShare    = Il2CppSystem.IO.FileShare;
using FileStream   = Il2CppSystem.IO.FileStream;
using SeekOrigin   = Il2CppSystem.IO.SeekOrigin;
using Stream       = Il2CppSystem.IO.Stream;
using Version      = Il2CppSystem.Version;
#endif

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
#if RG
            private static readonly MethodInfo streamCloseMethod = typeof(Stream).GetMethod(nameof(Stream.Close), Type.EmptyTypes);
            private static readonly HarmonyMethod streamClosePrefix = new(typeof(Hooks), nameof(Hooks.StreamClosePreHook));
            private static Dictionary<string, long> fileStreamClosePosition = new(StringComparer.OrdinalIgnoreCase);
#endif

            #region SceneInfo

            #region Loading

#if !RG
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
#else
            private static void StreamClosePreHook(Stream __instance)
            {
                if (!__instance.CanSeek && !__instance.CanRead && !__instance.CanWrite)
                    return;
                var fileStream = __instance.TryCast<FileStream>();
                if (fileStream is null)
                    return;
                var path = Path.GetFullPath(fileStream.name);
                if (fileStreamClosePosition.ContainsKey(path))
                    fileStreamClosePosition[path] = __instance.Position;
            }

            private static IEnumerator UnpatchStreamCloseAtEndOfFrame()
            {
                yield return new WaitForEndOfFrame();
                harmony.Unpatch(streamCloseMethod, streamClosePrefix.method);
                fileStreamClosePosition.Clear();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Load), typeof(string))]
            private static bool SceneInfoLoadHook(ref bool __result, object __instance, string _path)
            {
                __result = (__instance as SceneInfo).Load(_path, out _);
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Load),
                new Type[] { typeof(string), typeof(Version) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
            private static void SceneInfoLoadPreHook(string _path)
            {
                var path = Path.GetFullPath(_path);
                if (fileStreamClosePosition.ContainsKey(path))
                    return;
                if (!fileStreamClosePosition.Any())
                {
                    harmony.Patch(streamCloseMethod, prefix: streamClosePrefix);
                    StartCoroutine(UnpatchStreamCloseAtEndOfFrame());
                }
                fileStreamClosePosition.Add(path, -1);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Load),
                new Type[] { typeof(string), typeof(Version) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
            private static void SceneInfoLoadPostHook(bool __result, string _path)
            {
                var path = Path.GetFullPath(_path);
                if (!fileStreamClosePosition.TryGetValue(path, out var position))
                    return;
                fileStreamClosePosition.Remove(path);
                if (!fileStreamClosePosition.Any())
                    harmony.Unpatch(streamCloseMethod, streamClosePrefix.method);

                if (position == -1 || !__result)
                    return;

                var st = new FileStream(_path, FileMode.Open, FileAccess.Read);
                try
                {
                    var br = new BinaryReader(st);
                    try
                    {
                        br.BaseStream.Seek(position, SeekOrigin.Begin);
                        SceneInfoLoadHook(_path, br);
                    }
                    finally
                    {
                        br.Dispose();
                    }
                }
                finally
                {
                    st.Dispose();
                }
            }
#endif

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
                        var bytes = br.ReadBytes(length);
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

#if !RG
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
#else
            [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Import), typeof(string))]
            private static bool SceneInfoImportHook(ref bool __result, object __instance, string _path)
            {
                var fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    var binaryReader = new BinaryReader(fileStream);
                    try
                    {
                        PngFile.SkipPng(binaryReader);
                        var version = new Version(binaryReader.ReadString());
                        (__instance as SceneInfo).Import(binaryReader, version);
                    }
                    finally
                    {
                        binaryReader.Dispose();
                    }
                }
                finally
                {
                    fileStream.Dispose();
                }
                __result = true;
                return false;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Import), typeof(BinaryReader), typeof(Version))]
            private static void SceneInfoImportPostHook(BinaryReader _reader, Version _version)
            {
                var st = _reader.BaseStream.TryCast<FileStream>();
                if (st is not null)
                    SceneInfoImportHook(st.name, _reader, _version);
            }
#endif

            public static void SceneInfoImportHook(string path, BinaryReader br, Version _)
            {
                internalSceneDictionary.Clear();

                while (true)
                {
                    try
                    {
                        // -1 to go back to the length specifier of the string
#if !RG
                        var result = br.BaseStream.FindPosition(Marker.Select(c => (byte)c).ToArray()) - 1;
#else
                        var result = br.BaseStream.FindPosition(MarkerBytes);
#endif
                        if (result < 0) break;

                        br.BaseStream.Position = result;

                        string marker = br.ReadString();
                        int ver = br.ReadInt32();
                        int length = br.ReadInt32();

                        if (marker.Equals(Marker) && length > 0)
                        {
                            var bytes = br.ReadBytes(length);
                            internalSceneDictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
                        }
                    }
#if !RG
                    catch (EndOfStreamException)
                    {
#else
                    catch (Il2CppException ex)
                    {
                        if (!typeof(EndOfStreamException).IsAssignableFrom(ex.GetTypeFromMessage()))
                            throw;
#endif
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

#if !RG
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
#else
            [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), typeof(string))]
            private static void SceneInfoSavePostHook(string _path)
            {
                var st = new FileStream(_path, FileMode.Open, FileAccess.Write);
                try
                {
                    var bw = new BinaryWriter(st);
                    try
                    {
                        bw.OutStream.Seek(0, SeekOrigin.End);
                        SceneInfoSaveHook(_path, bw);
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
            }
#endif

            public static void SceneInfoSaveHook(string path, BinaryWriter bw)
            {
                SceneWriteEvent(path);

                Dictionary<string, PluginData> extendedData = internalSceneDictionary;
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

            #region Pose
#if !RG
            private static OCIChar PoseChar;
#else
            private static object PoseChar;
#endif
            private static string PoseName;
            // Use a single data maker since poses are compatible across games and PH marker is different for some reason.
            private const string PoseMarker = "KKEx";

            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Load))]
#if !RG
            private static void PauseCtrl_Load(OCIChar _ociChar, string _path)
#else
            private static void PauseCtrl_Load(object _ociChar, string _path)
#endif
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
                        var bytes = br.ReadBytes(length);
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
#if !RG
                    gameName = (GameNames)loadedData;
#else
                    gameName = (GameNames)(byte)loadedData;
#endif

                PoseReadEvent(PoseName, fileInfo, PoseChar, gameName);
            }


#if !RG
            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Save))]
            private static void PauseCtrl_Save(OCIChar _ociChar, string _name)
#else
            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Save), typeof(OCIChar), typeof(string))]
            private static void PauseCtrl_Save(object _ociChar, string _name)
#endif
            {
                PoseChar = _ociChar;
                PoseName = _name;
            }

#if !RG
            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Save))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Save), typeof(BinaryWriter))]
#endif
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

                // Remove null entries
                foreach (var key in extendedData.Where(entry => entry.Value == null).Select(entry => entry.Key).ToArray())
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
