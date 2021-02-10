using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using Housing;
using Sideloader.ListLoader;

namespace Sideloader.AutoResolver
{
    public static partial class UniversalAutoResolver
    {
        internal static List<AIGameResolveInfo> LoadedMainGameResolutionInfo = new List<AIGameResolveInfo>();

        /// <summary>
        /// A Method for generating AI Main-Game's resolution information.
        /// At this moment, it will only resolve furniture information.
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="data"></param>
        internal static void GenerateMainGameResolutionInfo(Manifest manifest, Lists.MainGameListData data)
        {
            // Add conditions here in case when there is new need of resolution type
            foreach (List<string> entry in data.Entries)
            {
                int newSlot = Interlocked.Increment(ref CurrentSlotID);
                LoadedMainGameResolutionInfo.Add(new AIGameResolveInfo
                {
                    GUID = manifest.GUID,
                    Slot = int.Parse(entry[0]),
                    LocalSlot = newSlot,
                    ResolveItem = true
                });

                if (Sideloader.DebugLoggingResolveInfo.Value)
                {
                    Sideloader.Logger.LogInfo($"MainGameResolveInfo - " +
                                              $"GUID: {manifest.GUID} " +
                                              $"Slot: {int.Parse(entry[0])} " +
                                              $"LocalSlot: {newSlot} " +
                                              $"Count: {LoadedStudioResolutionInfo.Count}");
                }

                entry[0] = newSlot.ToString();
            }
        }

        internal static void ResolveHousingFurniture(AIGameResolveInfo extResolve, OIItem OI, ResolveType resolveType = ResolveType.Load)
        {
            AIGameResolveInfo intResolve = LoadedMainGameResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == extResolve.Slot && x.GUID == extResolve.GUID); // originally OI.ID, maybe something gotta be changed related with hard mod compatibility?
            if (intResolve != null)
            {
                if (resolveType == ResolveType.Load && Sideloader.DebugLogging.Value)
                    Sideloader.Logger.LogDebug($"Resolving (Main-Game Furniture) [{extResolve.GUID}] {OI.ID}->{intResolve.LocalSlot}");
                OI.ID = intResolve.LocalSlot;
            }
            else if (resolveType == ResolveType.Load)
                ShowGUIDError(extResolve.GUID); // TODO: does craft menu is fault proof?
        }
    }
}
