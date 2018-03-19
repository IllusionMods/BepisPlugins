using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtensibleSaveFormat
{
    //[MessagePackObject(keyAsPropertyName: true)]
    [MessagePackObject]
    public class PluginData
    {
        [Key(0)]
        public int version;
        [Key(1)]
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }
}
