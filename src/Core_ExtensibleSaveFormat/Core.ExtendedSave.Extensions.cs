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
            try
            {
                var bytes = (byte[])Traverse.Create(partsInfo).Property(ExtendedSaveDataPropertyName).GetValue();
                if (bytes != null)
                {
                    Dictionary<string, PluginData> pluginData = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                    if (pluginData != null && pluginData.TryGetValue(id, out data))
                        return true;
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
            data = null;
            return false;
        }

        public static void SetExtendedDataById(this ChaFileAccessory.PartsInfo partsInfo, string id, PluginData data)
        {
            try
            {
                var bytes = (byte[])Traverse.Create(partsInfo).Property(ExtendedSaveDataPropertyName).GetValue();
                Dictionary<string, PluginData> pluginData;
                if (bytes == null)
                    pluginData = new Dictionary<string, PluginData>();
                else
                    pluginData = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                pluginData[id] = data;
                bytes = MessagePackSerialize(pluginData);
                Traverse.Create(partsInfo).Property(ExtendedSaveDataPropertyName).SetValue(bytes);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
        }
#endif
    }
}
