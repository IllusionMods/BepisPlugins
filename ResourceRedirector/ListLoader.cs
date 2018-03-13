using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceRedirector
{
    public static class ListLoader
    {
        const int CategoryMultiplier = 10000000; //was originally 1000 but that means the limit is 999 for each item

        private static FieldInfo r_dictListInfo = typeof(ChaListControl).GetField("dictListInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo r_lstItemIsInit = typeof(ChaListControl).GetField("lstItemIsInit", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo r_lstItemIsNew = typeof(ChaListControl).GetField("lstItemIsNew", BindingFlags.Instance | BindingFlags.NonPublic);

        public static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();

        internal static void LoadAllLists(ChaListControl instance)
        {
            foreach (ChaListData data in ExternalDataList)
                LoadList(instance, data);
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
					    int item = (listInfoBase.Category * CategoryMultiplier) + listInfoBase.Id;
					    if (infoInt == 1)
					    {
                            var lstItemIsInit = r_lstItemIsInit.GetValue<List<int>>(instance);
						    lstItemIsInit.Add(item);
					    }
					    else if (infoInt == 2)
					    {
                            var lstItemIsNew = r_lstItemIsNew.GetValue<List<int>>(instance);
						    lstItemIsNew.Add(item);
					    }
				    }
			    }
		    }
        }
    }
}
