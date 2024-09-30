using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepisPlugins;
using HarmonyLib;
using MessagePack;
using SaveData;
using UnityEngine;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region Loading
            
            [HarmonyPostfix, HarmonyPatch(typeof(WorldData), nameof(WorldData.SetBytes))]
            private static void SaveDataLoadHook(BinaryReader br, ref WorldData saveData)
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

            [HarmonyPrefix, HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.Save), typeof(string), typeof(string))]
            [HarmonyPriority(Priority.Last)]
            private static bool WorldDataSaveOverride(SaveData.WorldData __instance, string path, string fileName)
            {
                Illusion.Utils.File.OpenWrite(path + fileName, false, (Action<FileStream>)(f =>
                {
                    try
                    {
                        using (BinaryWriter binaryWriter = new BinaryWriter((Stream)f))
                        {
                            var saveData = __instance;

                            // Data to be written by WorldData.GetBytes
                            byte[] buffer = MessagePackSerializer.Serialize<WorldData>(saveData);
                            binaryWriter.Write(buffer.Length);
                            binaryWriter.Write(buffer);
                            byte[] bytes1 = SaveData.Player.GetBytes(saveData.player);
                            binaryWriter.Write(bytes1.Length);
                            binaryWriter.Write(bytes1);
                            int count = saveData.heroineList.Count;
                            binaryWriter.Write(count);
                            for (int index = 0; index < count; ++index)
                            {
                                byte[] bytes2 = Heroine.GetBytes(saveData.heroineList[index]);
                                binaryWriter.Write(bytes2.Length);
                                binaryWriter.Write(bytes2);
                            }

                            SaveDataWriteEvent(__instance);
                            Logger.Log(BepInEx.Logging.LogLevel.Debug, "SaveData hook!");

                            // Append the ext data
                            Dictionary<string, PluginData> extendedData = GetAllExtendedData(saveData);

                            if (extendedData != null)
                            {
                                binaryWriter.Write(Marker);
                                binaryWriter.Write(DataVersion);
                                byte[] data = MessagePackSerializer.Serialize(extendedData);
                                binaryWriter.Write(data.Length);
                                binaryWriter.Write(data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Error, "Failed to save the game, the save file may be corrupted! Try saving again in an empty slot.\nError: " + ex.Message);
                        UnityEngine.Debug.LogException(ex);
                    }
                }));

                return false;
            }


            [HarmonyPostfix, HarmonyPatch(typeof(WorldData), nameof(WorldData.GetBytes), typeof(WorldData))]
            private static void WorldDataGetBytesDisableHook(WorldData saveData, ref byte[] __result)
            {
                // This function should not be called.
                // Originally called from SaveData.WorldData.Save(), but the original Save() is not called by the patch.
                throw new System.NotSupportedException("Do not use this method, use WorldData.Save instead. More info: https://github.com/IllusionMods/BepisPlugins/pull/197");
            }

            #endregion
        }
    }
}