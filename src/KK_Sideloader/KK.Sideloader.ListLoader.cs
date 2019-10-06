using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        internal static HashSet<int> _internalStudioItemList = null;
        internal static List<MapInfo> ExternalMapList { get; private set; } = new List<MapInfo>();
        internal static List<StudioListData> ExternalStudioDataList { get; private set; } = new List<StudioListData>();

        internal static HashSet<int> InternalStudioItemList
        {
            get
            {
                //Generate a list of all the studio item IDs regardless of group/category
                if (_internalStudioItemList == null)
                {
                    _internalStudioItemList = new HashSet<int>();
                    foreach (var x in Singleton<Studio.Info>.Instance.dicItemLoadInfo)
                        foreach (var y in x.Value)
                            foreach (var z in y.Value)
                                _internalStudioItemList.Add(z.Key);
                }
                return _internalStudioItemList;
            }
        }

        internal static StudioListData LoadStudioCSV(Stream stream, string fileName)
        {
            StudioListData data = new StudioListData(fileName);

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                List<string> Header = reader.ReadLine().Trim().Split(',').ToList();
                data.Headers.Add(Header);
                List<string> Header2 = reader.ReadLine().Trim().Split(',').ToList();

                if (int.TryParse(Header2[0], out int cell))
                    //First cell of the row is a numeric ID, this is a data row
                    data.Entries.Add(Header2);
                else
                    //This is a second header row, as used by maps, animations, and voices
                    data.Headers.Add(Header2);

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();

                    if (!line.Contains(','))
                        break;

                    data.Entries.Add(line.Split(',').ToList());
                }
            }
            return data;
        }
        internal class StudioListData
        {
            public string FileName { get; private set; }
            public string FileNameWithoutExtension { get; private set; }
            public string AssetBundleName { get; private set; }
            public List<List<string>> Headers = new List<List<string>>();
            public List<List<string>> Entries = new List<List<string>>();

            public StudioListData(string fileName)
            {
                FileName = fileName;
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
                AssetBundleName = FileName.Remove(FileName.LastIndexOf('/')).Remove(0, FileName.IndexOf('/') + 1) + ".unity3d";
            }
        }

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
