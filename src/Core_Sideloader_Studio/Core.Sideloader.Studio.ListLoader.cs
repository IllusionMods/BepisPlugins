using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sideloader.ListLoader
{
    internal static partial class Lists
    {
        internal static HashSet<int> _internalStudioItemList = null;
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

        internal static StudioListData LoadStudioCSV(Stream stream, string fileName, string guid)
        {
            StudioListData data = new StudioListData(fileName);

            string fileNameStripped = fileName.Remove(0, fileName.LastIndexOf('/') + 1);
            string listType = fileNameStripped.Split('_')[0].ToLower();

            bool CategoryOrGroup = false;
            if (fileNameStripped.Contains("_"))
                if (Sideloader.StudioListResolveBlacklist.Contains(listType))
                    CategoryOrGroup = true;

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line == null) return data;
                List<string> Header = line.Trim().Split(',').ToList();
                data.Headers.Add(Header);

                line = reader.ReadLine();
                if (line == null) return data;
                List<string> Header2 = line.Trim().Split(',').ToList();

                if (int.TryParse(Header2[0], out int cell))
                    //First cell of the row is a numeric ID, this is a data row
                    data.Entries.Add(FormatList(Header2, CategoryOrGroup));
                else
                    //This is a second header row, as used by maps, animations, and voices
                    data.Headers.Add(Header2);

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Trim();

                    if (!line.Contains(',')) break;
                    var lineSplit = line.Split(',');

                    data.Entries.Add(FormatList(line.Split(',').ToList(), CategoryOrGroup));
                }
            }
            return data;

            List<string> FormatList(List<string> line, bool categoryOrGroup)
            {
#if AI || HS2
                //Convert group and category from KK to AI
                if (CategoryOrGroup)
                {
                    if (line.Count == 2)
                    {
                        string temp = line[1];
                        line[1] = line[0];
                        line.Add(temp);
                    }
                }
#endif

                return line;
            }
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
    }
}
