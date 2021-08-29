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

            // Nope, not patching the save lambda, not doing it, noooope
            [HarmonyPostfix, HarmonyPatch(typeof(WorldData), nameof(WorldData.GetBytes), typeof(WorldData))]
            private static void SaveDataSaveHook(WorldData saveData, ref byte[] __result)
            {
                try
                {
                    SaveDataWriteEvent(saveData);

                    Logger.Log(BepInEx.Logging.LogLevel.Debug, "SaveData hook!");

                    Dictionary<string, PluginData> extendedData = GetAllExtendedData(saveData);
                    if (extendedData == null)
                        return;

                    // Append the ext data
                    using (var ms = new MemoryStream())
                    {
                        using (BinaryWriter bw = new BinaryWriter(ms))
                        {
                            // Write the original data first
                            bw.Write(__result);

                            // Then write our data
                            bw.Write(Marker);
                            bw.Write(DataVersion);
                            byte[] data = MessagePackSerializer.Serialize(extendedData);
                            bw.Write(data.Length);
                            bw.Write(data);
                        }

                        // Replace the result, not the most efficient way but save files shouldn't be big enough to matter since it's all done in memory
                        __result = ms.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            #endregion
        }
    }
}