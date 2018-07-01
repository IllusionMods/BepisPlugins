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

        public static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();

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
            Dictionary<int, ListInfoBase> dictData;


            if (dictListInfo.TryGetValue(category, out dictData))
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
        #endregion
    }
}
