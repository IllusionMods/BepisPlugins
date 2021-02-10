using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        // internal main game furniture data

        /// <summary>
        /// Currently only resolving
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileName"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        internal static MainGameListData LoadMainGameCSV(Stream stream, string fileName, string assetBundleName, string guid)
        {
            MainGameListData data = new MainGameListData(fileName, assetBundleName);

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                List<string> Header = reader.ReadLine().Trim().Split(',').ToList();
                data.Headers.Add(Header);

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    if (!line.Contains(',')) break;
                    data.Entries.Add(line.Split(',').ToList());
                }
            }

            return data;
        }

        internal static void LoadExcelDataFromResolveInfo(MainGameListData data)
        {
            ExcelData excelData = (ExcelData)ScriptableObject.CreateInstance(typeof(ExcelData));
            excelData.list = data.Entries.Select(x => new ExcelData.Param(){list = x}).ToList();

            if (!ExternalExcelData.ContainsKey(data.AssetBundleName))
                ExternalExcelData[data.AssetBundleName] = new Dictionary<string, ExcelData>();
            ExternalExcelData[data.AssetBundleName][data.FileName] = excelData;
        }

        /// <summary>
        ///
        /// </summary>
        internal class MainGameListData
        {
            public string FileName { get; private set; }
            public string FileNameWithoutExtension { get; private set; }
            public string AssetBundleName { get; private set; }

            public List<List<string>> Headers = new List<List<string>>();
            public List<List<string>> Entries = new List<List<string>>();

            public MainGameListData(string fileName, string assetBundleName)
            {
                FileName = fileName;
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
                AssetBundleName = assetBundleName;
            }
        }
    }
}
