using BepInEx.Logging;
using HarmonyLib;
using HEdit;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExtensibleSaveFormat
{
    public partial class ExtendedSave
    {
        internal static partial class Hooks
        {
            #region HEditData

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HEditData), nameof(HEditData.Load), typeof(BinaryReader), typeof(int), typeof(YS_Node.NodeControl), typeof(HEditData.InfoData), typeof(bool))]
            internal static bool HEditDataLoadHook(bool __result, HEditData __instance, ref BinaryReader _reader)
            {
                var originalPosition = _reader.BaseStream.Position;
                try
                {
                    var marker = _reader.ReadString();
                    var version = _reader.ReadInt32();
                    var length = _reader.ReadInt32();
                    if (marker == Marker && version == DataVersion && length > 0)
                    {
                        var bytes = _reader.ReadBytes(length);
                        var dictionary = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);
                        _internalHEditDataDictionary.Set(__instance, dictionary);
                    }
                    else
                    {
                        _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                        _reader.BaseStream.Position = originalPosition;
                    }
                }
                catch (EndOfStreamException)
                {
                    /* Incomplete/non-existant data */
                    _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    _reader.BaseStream.Position = originalPosition;
                }
                catch (InvalidOperationException)
                {
                    /* Invalid/unexpected deserialized data */
                    _internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    _reader.BaseStream.Position = originalPosition;
                }

                HEditDataReadEvent(__instance);

                return __result;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HEditData), nameof(HEditData.Save), typeof(BinaryWriter), typeof(YS_Node.NodeControl), typeof(bool))]
            internal static bool HEditDataSaveHook(bool __result, HEditData __instance, ref BinaryWriter _writer)
            {
                HEditDataWriteEvent(__instance);

                Logger.Log(LogLevel.Debug, "MapInfo hook!");

                var extendedData = GetAllExtendedData(__instance);
                if (extendedData == null || extendedData.Count == 0)
                    return __result;

                var originalLength = _writer.BaseStream.Length;
                var originalPosition = _writer.BaseStream.Position;
                try
                {
                    var bytes = MessagePackSerialize(extendedData);

                    _writer.Write(Marker);
                    _writer.Write(DataVersion);
                    _writer.Write(bytes.Length);
                    _writer.Write(bytes);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"Failed to save extended data in card. {e.Message}");
                    _writer.BaseStream.Position = originalPosition;
                    _writer.BaseStream.SetLength(originalLength);
                }

                return __result;
            }

            #endregion

        }
    }
}