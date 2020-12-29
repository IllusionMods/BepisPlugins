using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepisPlugins;
using HarmonyLib;
using MessagePack;
using UnityEngine;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region Loading

            // Patch the load lambda that contains the actual code
            [HarmonyTranspiler, HarmonyPatch(typeof(SaveData), "<Load>m__4")]
            private static IEnumerable<CodeInstruction> SaveDataLoadTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                // Useless in VR and causes issues in steam version
                if (Application.productName == Constants.VRProcessName ||
                    Application.productName == Constants.VRProcessNameSteam)
                    return instructions;

                return new CodeMatcher(instructions)
                    .End()
                    .MatchBack(false, new CodeMatch(OpCodes.Leave)) // Find the last leave to get the position after all game data is written
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0)) // SaveData instance
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0)) // BinaryReader instance
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SaveDataLoadHook), AccessTools.all)))
                    .Instructions();
            }

            private static void SaveDataLoadHook(SaveData saveData, BinaryReader br)
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

                        internalSaveDataDictionary.Set(saveData, dictionary);
                    }
                    else
                        internalSaveDataDictionary.Set(saveData, new Dictionary<string, PluginData>()); //Overriding with empty data just in case there is some remnant from former loads.

                }
                catch (EndOfStreamException)
                {
                    // Incomplete/non-existant data
                    internalSaveDataDictionary.Set(saveData, new Dictionary<string, PluginData>());
                }
                catch (InvalidOperationException)
                {
                    // Invalid/unexpected deserialized data
                    internalSaveDataDictionary.Set(saveData, new Dictionary<string, PluginData>());
                }

                //Firing the event in any case
                SaveDataReadEvent(saveData);
            }

            #endregion

            #region Saving

            // Nope, not patching the save lambda, not doing it, noooope
            [HarmonyPostfix, HarmonyPatch(typeof(SaveData), "Save", typeof(string), typeof(string))]
            private static void SaveDataSaveHook(SaveData __instance, string path, string fileName)
            {
                SaveDataWriteEvent(__instance);

                Logger.Log(BepInEx.Logging.LogLevel.Debug, "SaveData hook!");

                Dictionary<string, PluginData> extendedData = GetAllExtendedData(__instance);
                if (extendedData == null)
                    return;

                var fullPath = path + fileName;
                if (!File.Exists(fullPath))
                {
                    Logger.LogError("Could not write scene data because the save file doesn't exist at " + fullPath + " even though it should.");
                    return;
                }

                // Append the ext data to the end of the save file, pretty much the same perf as hooking directly into the save method but much less painful
                using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = MessagePackSerializer.Serialize(extendedData);

                    bw.Write(Marker);
                    bw.Write(DataVersion);
                    bw.Write(data.Length);
                    bw.Write(data);
                }
            }

            #endregion
        }
    }
}