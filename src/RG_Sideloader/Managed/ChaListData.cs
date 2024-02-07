using MessagePack;
using System.Collections.Generic;

#pragma warning disable CS1591

namespace Sideloader
{
    [MessagePackObject(true)]
    public class ChaListData
    {
        public string mark { get; set; } = "";
        public int categoryNo { get; set; } = 0;
        public int distributionNo { get; set; } = 0;
        public string filePath { get; set; } = "";
        public List<string> lstKey { get; set; } = new();
        public Dictionary<int, List<string>> dictList { get; set; } = new();
    }
}
