using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ResourceRedirector
{
    public static class ListLoader
    {
        internal static int CategoryMultiplier = 1000000; //was originally 1000 but that means the limit is 999 for each item

        private static readonly FieldInfo r_dictListInfo = typeof(ChaListControl).GetField("dictListInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>();
        private static HashSet<int> _internalStudioItemList = null;

        public static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();
        public static List<StudioListData> ExternalStudioDataList { get; private set; } = new List<StudioListData>();
        public static List<MapInfo> ExternalMapList { get; private set; } = new List<MapInfo>();

        public static int CalculateGlobalID(int category, int ID) => (category * CategoryMultiplier) + ID;

        internal static void LoadAllLists(ChaListControl instance)
        {
            InternalDataList = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);

            foreach (ChaListData data in ExternalDataList)
                LoadList(instance, data);

            instance.LoadItemID();
        }

        public static void LoadList(this ChaListControl instance, ChaListData data) => LoadList(instance, (ChaListDefine.CategoryNo)data.categoryNo, data);

        public static void LoadList(this ChaListControl instance, ChaListDefine.CategoryNo category, ChaListData data)
        {
            var dictListInfo = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);

            if (dictListInfo.TryGetValue(category, out Dictionary<int, ListInfoBase> dictData))
            {
                loadListInternal(instance, dictData, data);
            }
        }

        private static void loadListInternal(this ChaListControl instance, Dictionary<int, ListInfoBase> dictData, ChaListData chaListData)
        {
            foreach (KeyValuePair<int, List<string>> keyValuePair in chaListData.dictList)
            {
                ListInfoBase listInfoBase = new ListInfoBase();

                if (listInfoBase.Set(chaListData.categoryNo, chaListData.distributionNo, chaListData.lstKey, keyValuePair.Value))
                {
                    if (!dictData.ContainsKey(listInfoBase.Id))
                    {
                        dictData[listInfoBase.Id] = listInfoBase;
                        int infoInt = listInfoBase.GetInfoInt(ChaListDefine.KeyType.Possess);
                        int item = CalculateGlobalID(listInfoBase.Category, listInfoBase.Id);
                        instance.AddItemID(item, (byte)infoInt);
                    }
                }
            }
        }

        public static HashSet<int> InternalStudioItemList
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


        #region Helpers
        public static ChaListData LoadCSV(Stream stream)
        {
            ChaListData chaListData = new ChaListData();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                chaListData.categoryNo = int.Parse(reader.ReadLine().Split(',')[0].Trim());
                chaListData.distributionNo = int.Parse(reader.ReadLine().Split(',')[0].Trim());
                chaListData.filePath = reader.ReadLine().Split(',')[0].Trim();
                chaListData.lstKey = reader.ReadLine().Trim().Split(',').ToList();

                int i = 0;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();

                    if (!line.Contains(','))
                        break;

                    chaListData.dictList.Add(i++, line.Split(',').ToList());
                }
            }

            return chaListData;
        }

        public static StudioListData LoadStudioCSV(Stream stream, string fileName)
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
        public class StudioListData
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

        public static MapInfo LoadMapCSV(Stream stream)
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
        #endregion
    }
}
