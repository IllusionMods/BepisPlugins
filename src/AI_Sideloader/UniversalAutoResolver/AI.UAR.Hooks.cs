using System.Collections.Generic;
using System.Linq;
using AIProject.SaveData;
using ExtensibleSaveFormat;
using HarmonyLib;
using Housing;

// ReSharper disable All

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static partial class Hooks
        {
            internal static void InstallMainGameHooks(Harmony harmony)
            {
                ExtendedSave.MainGameSaveBeingSaved += ExtendedMainGameSave;
                ExtendedSave.MainGameSaveBeingLoaded += ExtendedMainGameLoad;
                ExtendedSave.HousingBeingSaved += ExtendedHousingSave;
                ExtendedSave.HousingBeingLoaded += ExtendedHousingLoad;
            }

            /// <summary>
            /// a
            /// Warning GameSave resolution is very expensive!
            /// do not resolve other worlds!
            /// </summary>
            /// <param name="save"></param>
            internal static void ExtendedMainGameSave(SaveData save)
            {
                if (Sideloader.DebugLoggingResolveInfo.Value)
                {
                    Sideloader.Logger.LogInfo($"Embedding Resolving Data to Main Game Save");
                }
            }

            internal static void ExtendedMainGameLoad(SaveData save)
            {
                if (Sideloader.DebugLoggingResolveInfo.Value)
                {
                    Sideloader.Logger.LogInfo($"Resolving Extended Save Game");
                }
            }

            internal static void ExtendedHousingSave(CraftInfo info)
            {
                if (Sideloader.DebugLoggingResolveInfo.Value)
                {
                    Sideloader.Logger.LogInfo($"Embedding Resolving Data to Housing");
                }

                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();
                List<AIGameResolveInfo> MainGameResolutionInfos = new List<AIGameResolveInfo>();

                foreach (IObjectInfo objectInfo in info.ObjectInfos)
                {
                    if (objectInfo is OIItem item)
                    {
                        AIGameResolveInfo extResolve = LoadedMainGameResolutionInfo.Where(x => x.LocalSlot == item.ID).FirstOrDefault();
                        if (extResolve != null)
                        {
                            MainGameResolutionInfos.Add(new AIGameResolveInfo()
                            {
                                Slot = extResolve.Slot,
                                LocalSlot = extResolve.LocalSlot,
                                GUID = extResolve.GUID
                            });

                            //Set item ID back to original non-resolved ID
                            if (Sideloader.DebugLogging.Value) Sideloader.Logger.LogDebug($"Setting ID:{item.ID}->{extResolve.Slot}");
                            item.ID = extResolve.Slot;
                        }
                    }
                }

                ExtendedData.Add("mapItemInfo", MainGameResolutionInfos);
            }

            internal static void ExtendedHousingLoad(CraftInfo info)
            {
                if (Sideloader.DebugLoggingResolveInfo.Value)
                {
                    Sideloader.Logger.LogInfo($"Resolving Extended Save Game");
                }

                PluginData ExtendedData = ExtendedSave.GetExtendedDataById(info, UARExtID);
            }
        }
    }
}
