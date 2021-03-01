using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIProject.SaveData;
using ExtensibleSaveFormat;
using HarmonyLib;
using Housing;
using MessagePack;

// ReSharper disable All

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            private const string mapItemInfoKey = "mapItemInfo";

            internal static void InstallMainGameHooks(Harmony harmony)
            {
                ExtendedSave.SaveDataBeingSaved += ExtendedSaveData;
                ExtendedSave.SaveDataBeingLoaded += ExtendedLoad;
                ExtendedSave.HousingBeingSaved += ExtendedHousingSave;
                ExtendedSave.HousingBeingLoaded += ExtendedHousingLoad;
            }

            /// <summary>
            /// a
            /// Warning GameSave resolution is very expensive!
            /// do not resolve other worlds!
            /// </summary>
            /// <param name="save"></param>
            internal static void ExtendedSaveData(SaveData save)
            {
                if (save == null) return;

                if (Sideloader.DebugLoggingResolveInfo.Value)
                    Sideloader.Logger.LogInfo($"Embedding Resolving Data to Main Game Save");

                ExtendedWorldSave(save.AutoData); // autosave world
                foreach (var world in save.WorldList.Values)
                    ExtendedWorldSave(world); // saveslot world
            }

            private static void ExtendedWorldSave(WorldData worldData)
            {
                if (worldData == null) return; // just in case
                // ugh... I don't think this is good idea
                // also this code can't resolve world-level resolve.. if there will be any..
                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();

                foreach (var craftInfoPairs in worldData.HousingData.CraftInfos)
                {
                    ExtendedHousingSave(craftInfoPairs.Value);
                    var data = ExtendedSave.GetExtendedDataById(craftInfoPairs.Value, UARExtID).data;
                    ExtendedData.Add(craftInfoPairs.Key.ToString(), data);
                }

                ExtendedSave.SetExtendedDataById(worldData, UARExtID, new PluginData() {data = ExtendedData});
            }

            internal static void ExtendedLoad(SaveData save)
            {
                if (save == null) return;

                if (Sideloader.DebugLoggingResolveInfo.Value)
                    Sideloader.Logger.LogInfo($"Resolving Extended Save Game");
            }

            private static void ExtendedWorldLoad(WorldData worldData)
            {
                if (worldData == null) return;

                var extResolve = ExtendedSave.GetExtendedDataById(worldData, UARExtID);
                if (extResolve == null) return;

                foreach (var dataPair in extResolve.data)
                {
                    if (int.TryParse(dataPair.Key, out int id) &&
                        worldData.HousingData.CraftInfos.TryGetValue(id, out var craftInfo) &&
                        dataPair.Value is Dictionary<string, object> extData)
                    {
                        ExtendedSave.SetExtendedDataById(craftInfo, UARExtID, new PluginData() {data = extData});
                        ExtendedHousingLoad(craftInfo);
                    }
                }
            }

            private static void HousingObjectIteration<T>(IEnumerable<IObjectInfo> infoList, ref int index, Dictionary<int, T> indexedItems, Func<OIItem, T> callback)
            {
                if (callback == null) return;
                foreach (var info in infoList)
                    if (info is OIItem item) indexedItems.Add(index++, callback(item));
                    else if (info is OIFolder folder) HousingObjectIteration<T>(folder.Child, ref index, indexedItems, callback);
            }

            private static void HousingObjectIteration(IEnumerable<IObjectInfo> infoList, ref int index, Action<OIItem, int> callback)
            {
                if (callback == null) return;
                foreach (var info in infoList)
                    if (info is OIItem item) callback(item, index++);
                    else if (info is OIFolder folder) HousingObjectIteration(folder.Child, ref index, callback);
            }

            internal static void ExtendedHousingSave(CraftInfo info)
            {
                if (info == null) return;

                if (Sideloader.DebugLoggingResolveInfo.Value)
                    Sideloader.Logger.LogInfo($"Embedding Resolving Data to Housing");

                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();

                // housing data has no "key" so it's almost nightmare to resolve the id.
                // and worst of all, the structure of housing card object info is nested.
                // so what im going to do is generating iteration index which can be used for distinguishing objects
                int index = 0;
                Dictionary<int, byte[]> ResolutionInfos = new Dictionary<int, byte[]>();
                HousingObjectIteration<byte[]>(info.ObjectInfos, ref index, ResolutionInfos, (item) =>
                {
                    var extResolve = LoadedMainGameResolutionInfo.Where(x => x.LocalSlot == item.ID).FirstOrDefault();
                    if (extResolve != null)
                    {
                        return new AIGameResolveInfo
                        {
                            GUID = extResolve.GUID,
                            Slot = extResolve.Slot, // I need to know the history about "hard mod compatibility"
                            LocalSlot = extResolve.LocalSlot,
                        }.Serialize();
                    }
                    else
                    {
                        return null;
                    }
                });
                ExtendedData.Add(mapItemInfoKey, ResolutionInfos);

                ExtendedSave.SetExtendedDataById(info, UARExtID, new PluginData() {data = ExtendedData});
            }

            internal static void ExtendedHousingLoad(CraftInfo info)
            {
                if (info == null)
                    return; // TODO: check if this case needs debug logging message.

                if (Sideloader.DebugLoggingResolveInfo.Value)
                    Sideloader.Logger.LogInfo($"Resolving Extended Save Game");

                PluginData ExtendedData = ExtendedSave.GetExtendedDataById(info, UARExtID);
                if (ExtendedData != null)
                {
                    var ResolvedInfo = ((Dictionary<int, byte[]>) ExtendedData.data[mapItemInfoKey]);
                    int index = 0;
                    HousingObjectIteration(info.ObjectInfos, ref index, (item, i) =>
                    {
                        if (!ResolvedInfo.TryGetValue(i, out byte[] bytes)) return;
                        var extResolve = AIGameResolveInfo.Deserialize(bytes);
                        if (extResolve == null) return;
                        item.ID = extResolve.Slot;
                        ResolveHousingFurniture(extResolve, item);
                    });

                    Sideloader.Logger.LogInfo("Successfully loaded extended data. resolving data...");
                }
            }
        }
    }
}
