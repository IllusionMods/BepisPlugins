using MessagePack;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591

namespace ExtensibleSaveFormat
{
    [MessagePackObject(true)]
    public class BlockHeader
    {
        [MessagePackObject(true)]
        public class Info
        {
            public string name { get; set; } = "";
            public string version { get; set; } = "";
            public long pos { get; set; } = 0;
            public long size { get; set; } = 0;
        }

        public List<Info> lstInfo { get; set; } = new();

        public Info SearchInfo(string name) =>
            lstInfo.Find(info => info.name == name);

        public Info SearchInfo(params string[] names) =>
            lstInfo.Find(info => names.Contains(info.name));
    }
}
