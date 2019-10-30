using BepInEx.Harmony;
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

            [ParameterByRef(1)]
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(SceneInfo), "Load", typeof(string), typeof(Version))]
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

            [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), "Import", typeof(string))]
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

            [HarmonyTranspiler, HarmonyPatch(typeof(SceneInfo), "Save", typeof(string))]
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
                SceneWriteEvent(path);

                Dictionary<string, PluginData> extendedData = internalSceneDictionary;
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

            #region Extended Data Override Hooks
            //Prevent loading extended data when loading the list of characters in Studio since it is irrelevant here
            [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
            public static void StudioFemaleListPreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
            public static void StudioFemaleListPostHook() => LoadEventsEnabled = true;
            [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitMaleList")]
            public static void StudioMaleListPreHook() => LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitMaleList")]
            public static void StudioMaleListPostHook() => LoadEventsEnabled = true;

            //Prevent loading extended data when loading the list of coordinates in Studio since it is irrelevant here
            public static void StudioCoordinateListPreHook() => LoadEventsEnabled = false;
            public static void StudioCoordinateListPostHook() => LoadEventsEnabled = true;
            #endregion
        }
    }
}