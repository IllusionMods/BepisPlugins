using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        internal static List<MapInfo> ExternalMapList { get; private set; } = new List<MapInfo>();

        internal static MapInfo LoadMapCSV(Stream stream)
        {
            MapInfo data = ScriptableObject.CreateInstance<MapInfo>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    MapInfo.Param param = new MapInfo.Param();

                    string line = reader.ReadLine().Trim();

                    if (!line.Contains(','))
                        break;
                    var lineSplit = line.Split(',').ToList();

                    param.MapName = lineSplit[0];
                    param.No = int.Parse(lineSplit[1]);
                    param.AssetBundleName = lineSplit[2];
                    param.AssetName = lineSplit[3];
                    param.isGate = lineSplit[4] == "1";
                    param.is2D = lineSplit[5] == "1";
                    param.isWarning = lineSplit[6] == "1";
                    param.State = int.Parse(lineSplit[7]);
                    param.LookFor = int.Parse(lineSplit[8]);
                    param.isOutdoors = lineSplit[9] == "1";
                    param.isFreeH = lineSplit[10] == "1";
                    param.isSpH = lineSplit[11] == "1";
                    param.ThumbnailBundle = lineSplit[12];
                    param.ThumbnailAsset = lineSplit[13];

                    data.param.Add(param);
                }
            }
            return data;
        }
    }
}
