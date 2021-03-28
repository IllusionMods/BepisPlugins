using HarmonyLib;
using System.Collections.Generic;
using static ExtensibleSaveFormat.ExtendedSave;

namespace ExtensibleSaveFormat
{
    public static class Extensions
    {
        private static bool GetExtendedData(object messagePackObject, string id, out PluginData data)
        {
            try
            {
                var bytes = (byte[])Traverse.Create(messagePackObject).Property(ExtendedSaveDataPropertyName).GetValue();
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

        private static void SetExtendedData(object messagePackObject, string id, PluginData data)
        {
            try
            {
                var bytes = (byte[])Traverse.Create(messagePackObject).Property(ExtendedSaveDataPropertyName).GetValue();
                Dictionary<string, PluginData> pluginData;
                if (bytes == null)
                    pluginData = new Dictionary<string, PluginData>();
                else
                    pluginData = MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);

                pluginData[id] = data;
                bytes = MessagePackSerialize(pluginData);
                Traverse.Create(messagePackObject).Property(ExtendedSaveDataPropertyName).SetValue(bytes);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
        }


#if KK || EC
        public static bool TryGetExtendedDataById(this ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if KK
        public static bool TryGetExtendedDataById(this ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if EC
        public static bool TryGetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif
    }
}
