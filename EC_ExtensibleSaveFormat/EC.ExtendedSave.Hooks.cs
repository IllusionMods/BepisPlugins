using BepInEx.Logging;
using HarmonyLib;
using HEdit;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExtensibleSaveFormat
{
    public static partial class Hooks
    {
        #region HEditData

        #region Loading

        // HEdit.HEditData
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HEditData), nameof(HEditData.Load), new[] { typeof(BinaryReader), typeof(int), typeof(YS_Node.NodeControl), typeof(HEditData.InfoData), typeof(bool) })]
        private static bool HEditDataLoadHook(bool __result, HEditData __instance, ref BinaryReader _reader, ref int _loadKind, ref YS_Node.NodeControl _nodeControl, ref HEditData.InfoData _info, ref bool _isEdit)
        {
            var originalPosition = _reader.BaseStream.Position;
            try
            {
                var marker = _reader.ReadString();
                var version = _reader.ReadInt32();
                var length = _reader.ReadInt32();
                if (marker == Marker && version == Version && length > 0)
                {
                    var bytes = _reader.ReadBytes(length);
                    var dictionary = ExtendedSave.MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);
                    ExtendedSave._internalHEditDataDictionary.Set(__instance, dictionary);
                }
                else
                {
                    ExtendedSave._internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                    _reader.BaseStream.Position = originalPosition;
                }
            }
            catch (EndOfStreamException)
            {
                /* Incomplete/non-existant data */
                ExtendedSave._internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                _reader.BaseStream.Position = originalPosition;
            }
            catch (InvalidOperationException)
            {
                /* Invalid/unexpected deserialized data */
                ExtendedSave._internalHEditDataDictionary.Set(__instance, new Dictionary<string, PluginData>());
                _reader.BaseStream.Position = originalPosition;
            }

            ExtendedSave.HEditDataReadEvent(__instance);

            return __result;
        }

        #endregion

        #region Saving

        // HEdit.HEditData
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HEditData), nameof(HEditData.Save), new[] { typeof(BinaryWriter), typeof(YS_Node.NodeControl), typeof(bool) })]
        private static bool HEditDataSaveHook(bool __result, HEditData __instance, ref BinaryWriter _writer, ref YS_Node.NodeControl _nodeControl, ref bool _isInitUserID)
        {
            ExtendedSave.HEditDataWriteEvent(__instance);

            ExtendedSave.Logger.Log(LogLevel.Debug, "MapInfo hook!");

            var extendedData = ExtendedSave.GetAllExtendedData(__instance);
            if (extendedData == null || extendedData.Count == 0)
                return __result;

            var originalLength = _writer.BaseStream.Length;
            var originalPosition = _writer.BaseStream.Position;
            try
            {
                var bytes = ExtendedSave.MessagePackSerialize(extendedData);

                _writer.Write(Marker);
                _writer.Write(Version);
                _writer.Write(bytes.Length);
                _writer.Write(bytes);
            }
            catch (Exception e)
            {
                ExtendedSave.Logger.Log(LogLevel.Warning, $"Failed to save extended data in card. {e.Message}");
                _writer.BaseStream.Position = originalPosition;
                _writer.BaseStream.SetLength(originalLength);
            }

            return __result;
        }

        #endregion

        #endregion

    }
}
