using BepInEx.Harmony;
using ChaCustom;
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
    public static partial class Hooks
    {
        private static bool cardReadEventCalled;

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

            ExtendedSave.SceneReadEvent(path);
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

            ExtendedSave.SceneImportEvent(path);
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
            ExtendedSave.SceneWriteEvent(path);

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

        #region Extended Data Override Hooks
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