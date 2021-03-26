using HarmonyLib;
using System.Collections.Generic;
using static ExtensibleSaveFormat.ExtendedSave;

namespace ExtensibleSaveFormat
{
    public static class Extensions
    {
#if KK || EC
        public static bool TryGetExtendedDataById(this ChaFileAccessory.PartsInfo partsInfo, string id, out PluginData data)
        {
            var bytes = (byte[])Traverse.Create(partsInfo).Property("PluginData").GetValue();
            if (bytes != null)
            {
                Dictionary<string, PluginData> pluginData = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                if (pluginData != null && pluginData.TryGetValue(id, out data))
                    return true;
            }

            data = null;
            return false;
        }

        public static void SetExtendedDataById(this ChaFileAccessory.PartsInfo partsInfo, string id, PluginData data)
        {
            var bytes = (byte[])Traverse.Create(partsInfo).Property("PluginData").GetValue();
            Dictionary<string, PluginData> pluginData;
            if (bytes == null)
                pluginData = new Dictionary<string, PluginData>();
            else
                pluginData = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

            pluginData[id] = data;
            bytes = MessagePackSerialize(pluginData);
            Traverse.Create(partsInfo).Property("PluginData").SetValue(bytes);
        }
#endif
    }
}
