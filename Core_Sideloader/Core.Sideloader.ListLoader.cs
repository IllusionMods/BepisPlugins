using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if AI
using AIChara;
#endif

namespace Sideloader.ListLoader
{
    public static partial class Lists
    {
        internal const int CategoryMultiplier = 1000000;

        private static readonly FieldInfo r_dictListInfo = typeof(ChaListControl).GetField("dictListInfo", BindingFlags.Instance | BindingFlags.NonPublic);

#if KK || EC
        public static Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>();
#elif AI
        public static Dictionary<int, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<int, Dictionary<int, ListInfoBase>>();
#endif

        public static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();

        public static int CalculateGlobalID(int category, int ID) => (category * CategoryMultiplier) + ID;

        internal static void LoadAllLists(ChaListControl instance)
        {
#if KK || EC
            InternalDataList = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);
#elif AI
            InternalDataList = r_dictListInfo.GetValue<Dictionary<int, Dictionary<int, ListInfoBase>>>(instance);
#endif

            foreach (ChaListData data in ExternalDataList)
                LoadList(instance, data);

            instance.LoadItemID();
        }

        public static void LoadList(this ChaListControl instance, ChaListData data) => LoadList(instance, (ChaListDefine.CategoryNo)data.categoryNo, data);

#if KK || EC
        public static void LoadList(this ChaListControl instance, ChaListDefine.CategoryNo category, ChaListData data)
        {
            var dictListInfo = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);

            if (dictListInfo.TryGetValue(category, out Dictionary<int, ListInfoBase> dictData))
            {
                loadListInternal(instance, dictData, data);
            }
        }
#elif AI
        public static void LoadList(this ChaListControl instance, ChaListDefine.CategoryNo category, ChaListData data)
        {
            var dictListInfo = r_dictListInfo.GetValue<Dictionary<int, Dictionary<int, ListInfoBase>>>(instance);

            if (dictListInfo.TryGetValue((int)category, out Dictionary<int, ListInfoBase> dictData))
            {
                loadListInternal(instance, dictData, data);
            }
        }
#endif

#if KK || EC
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
#elif AI
        private static void loadListInternal(this ChaListControl instance, Dictionary<int, ListInfoBase> dictData, ChaListData chaListData)
        {
            foreach (KeyValuePair<int, List<string>> keyValuePair in chaListData.dictList)
            {
                int count = dictData.Count;
                ListInfoBase listInfoBase = new ListInfoBase();

                if (listInfoBase.Set(count, chaListData.categoryNo, chaListData.distributionNo, chaListData.lstKey, keyValuePair.Value))
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
#endif

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
    }
}
