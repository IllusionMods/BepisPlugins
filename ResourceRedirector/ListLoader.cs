using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceRedirector
{
    public static class ListLoader
    {
        internal static int CategoryMultiplier = 1000000; //was originally 1000 but that means the limit is 999 for each item

        private static FieldInfo r_dictListInfo = typeof(ChaListControl).GetField("dictListInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>();
        private static HashSet<int> _internalStudioItemList = null;

        public static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();
        public static List<StudioListData> ExternalStudioDataList { get; private set; } = new List<StudioListData>();

        public static int CalculateGlobalID(int category, int ID)
        {
            return (category * CategoryMultiplier) + ID;
        }

        internal static void LoadAllLists(ChaListControl instance)
        {
            InternalDataList = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);

            foreach (ChaListData data in ExternalDataList)
                LoadList(instance, data);

            instance.LoadItemID();
        }

        public static void LoadList(this ChaListControl instance, ChaListData data)
        {
            LoadList(instance, (ChaListDefine.CategoryNo)data.categoryNo, data);
        }

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
                chaListData.categoryNo = int.Parse(reader.ReadLine().Trim());
                chaListData.distributionNo = int.Parse(reader.ReadLine().Trim());
                chaListData.filePath = reader.ReadLine().Trim();
                chaListData.lstKey = reader.ReadLine().Trim().Split(',').ToList();

                int columnCount = chaListData.lstKey.Count;
                int i = 0;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();

                    if (!line.Contains(','))
                        break;

                    List<string> lineSplit = line.Split(',').ToList();
                    if (lineSplit.Count == columnCount)
                        chaListData.dictList.Add(i++, lineSplit);
                    else
                        throw new System.Exception("Row column count does not match header column count.");
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
                int columnCount = Header.Count;
                data.Headers.Add(Header);

                List<string> Header2 = reader.ReadLine().Trim().Split(',').ToList();
                if (Header2.Count != columnCount)
                    throw new System.Exception("Row column count does not match header column count.");
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

                    List<string> lineSplit = line.Split(',').ToList();
                    if (lineSplit.Count == columnCount)
                        data.Entries.Add(lineSplit);
                    else
                        //Only add lines that have the same number of columns or problems might happen.
                        throw new System.Exception("Row column count does not match header column count.");
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
        #endregion
    }
}
