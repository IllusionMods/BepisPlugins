using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using System.Xml.Linq;
#if KK || EC
using ChaCustom;
#elif AI || HS2
using AIChara;
#endif

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        internal const int CategoryMultiplier = 1000000;
        private const string CheckItemFile = "UserData/save/checkitem.xml";

        internal static readonly FieldInfo r_dictListInfo = typeof(ChaListControl).GetField("dictListInfo", AccessTools.all);

#if KK || EC
        internal static Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>();
#elif AI || HS2
        internal static Dictionary<int, Dictionary<int, ListInfoBase>> InternalDataList { get; private set; } = new Dictionary<int, Dictionary<int, ListInfoBase>>();
#endif

        internal static List<ChaListData> ExternalDataList { get; private set; } = new List<ChaListData>();
        //AssetBundle/AssetName/ExcelData
        internal static Dictionary<string, Dictionary<string, ExcelData>> ExternalExcelData { get; private set; } = new Dictionary<string, Dictionary<string, ExcelData>>();

        internal static int CalculateGlobalID(int category, int ID) => (category * CategoryMultiplier) + ID;
        //GUID/Category/ID
        internal static Dictionary<string, Dictionary<int, HashSet<int>>> CheckItemList = new Dictionary<string, Dictionary<int, HashSet<int>>>();

        internal static void LoadAllLists(ChaListControl instance)
        {
#if KK || EC
            InternalDataList = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);
#elif AI || HS2
            InternalDataList = r_dictListInfo.GetValue<Dictionary<int, Dictionary<int, ListInfoBase>>>(instance);
#endif

            foreach (ChaListData data in ExternalDataList)
                LoadList(instance, data);

            instance.LoadItemID();
        }

        internal static void LoadList(this ChaListControl instance, ChaListData data) => LoadList(instance, (ChaListDefine.CategoryNo)data.categoryNo, data);

#if KK || EC
        internal static void LoadList(this ChaListControl instance, ChaListDefine.CategoryNo category, ChaListData data)
        {
            var dictListInfo = r_dictListInfo.GetValue<Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>>(instance);

            if (dictListInfo.TryGetValue(category, out Dictionary<int, ListInfoBase> dictData))
            {
                LoadListInternal(instance, dictData, data);
            }
        }
#elif AI || HS2
        internal static void LoadList(this ChaListControl instance, ChaListDefine.CategoryNo category, ChaListData data)
        {
            var dictListInfo = r_dictListInfo.GetValue<Dictionary<int, Dictionary<int, ListInfoBase>>>(instance);

            if (dictListInfo.TryGetValue((int)category, out Dictionary<int, ListInfoBase> dictData))
            {
                LoadListInternal(instance, dictData, data);
            }
        }
#endif

#if KK || EC
        internal static void LoadListInternal(this ChaListControl instance, Dictionary<int, ListInfoBase> dictData, ChaListData chaListData)
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
#elif AI || HS2
        internal static void LoadListInternal(this ChaListControl instance, Dictionary<int, ListInfoBase> dictData, ChaListData chaListData)
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

        internal static ChaListData LoadCSV(Stream stream)
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

                    if (!line.Contains(',')) break;
                    var lineSplit = line.Split(',');

                    chaListData.dictList.Add(i++, lineSplit.ToList());
                }
            }

            return chaListData;
        }

        internal static void LoadExcelDataCSV(string assetBundleName, string assetName, Stream stream)
        {
            ExcelData excelData = (ExcelData)ScriptableObject.CreateInstance(typeof(ExcelData));
            excelData.list = new List<ExcelData.Param>();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();

                    ExcelData.Param param = new ExcelData.Param { list = line.Split(',').ToList() };

                    excelData.list.Add(param);
                }
            }

            if (!ExternalExcelData.ContainsKey(assetBundleName))
                ExternalExcelData[assetBundleName] = new Dictionary<string, ExcelData>();
            ExternalExcelData[assetBundleName][assetName] = excelData;
        }

        internal static void LoadCheckItemList()
        {
            if (!File.Exists(CheckItemFile)) return;
            if (CheckItemList.Count > 0) return;
            try
            {
                XDocument checkItemListXML = XDocument.Load(CheckItemFile);

                var checkItemList = checkItemListXML.Element("checkitem");
                foreach (var modElement in checkItemList.Elements("mod"))
                {
                    string guid = modElement.Attribute("guid")?.Value;
                    if (!guid.IsNullOrEmpty())
                    {
                        foreach (var categoryElement in modElement.Elements("category"))
                        {
                            if (int.TryParse(categoryElement.Attribute("number")?.Value, out int category))
                            {
                                foreach (var itemElement in categoryElement.Elements("item"))
                                {
                                    if (int.TryParse(itemElement.Attribute("id")?.Value, out int id))
                                    {
                                        if (!CheckItemList.ContainsKey(guid))
                                            CheckItemList[guid] = new Dictionary<int, HashSet<int>>();
                                        if (!CheckItemList[guid].ContainsKey(category))
                                            CheckItemList[guid][category] = new HashSet<int>();
                                        CheckItemList[guid][category].Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                File.Delete(CheckItemFile);
            }
        }

        internal static void SaveCheckItemList()
        {
            File.Delete(CheckItemFile);

            try
            {
                XDocument itemNewListXML = new XDocument();
                XElement checkItemListElement = new XElement("checkitem");
                itemNewListXML.Add(checkItemListElement);

                foreach (var x in CheckItemList)
                {
                    XElement modElement = new XElement("mod");
                    modElement.SetAttributeValue("guid", x.Key);
                    checkItemListElement.Add(modElement);

                    foreach (var y in x.Value)
                    {
                        XElement categoryElement = new XElement("category");
                        categoryElement.SetAttributeValue("number", y.Key);
                        modElement.Add(categoryElement);

                        foreach (var z in y.Value)
                        {
                            XElement itemElement = new XElement("item");
                            itemElement.SetAttributeValue("id", z);
                            categoryElement.Add(itemElement);
                        }
                    }
                }

                itemNewListXML.Save(CheckItemFile);
            }
            catch
            {
                File.Delete(CheckItemFile);
            }
        }
    }
}
