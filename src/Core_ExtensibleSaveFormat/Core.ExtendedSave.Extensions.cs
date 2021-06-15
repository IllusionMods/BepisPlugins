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


#if KK || KKS || EC
        //Body
        public static bool TryGetExtendedDataById(this ChaFileBody messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileBody messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Face
        public static bool TryGetExtendedDataById(this ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Hair
        public static bool TryGetExtendedDataById(this ChaFileHair messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileHair messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileHair.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileHair.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Clothes
        public static bool TryGetExtendedDataById(this ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Accessory
        public static bool TryGetExtendedDataById(this ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if KK || KKS
        public static bool TryGetExtendedDataById(this ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if KKS
        public static bool TryGetExtendedDataById(this ChaFileAbout messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAbout messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this ChaFileParameter.Interest messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Interest messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if EC
        public static bool TryGetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if AI || HS2
        //Body
        public static bool TryGetExtendedDataById(this AIChara.ChaFileBody messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileBody messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Face
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace.EyesInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace.EyesInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace.MakeupInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace.MakeupInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Hair
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Clothes
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Accessory
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo.MinMaxInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo.MinMaxInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this AIChara.ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this AIChara.ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if HS2
        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo2 messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo2 messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static bool TryGetExtendedDataById(this AIChara.ChaFileParameter2 messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileParameter2 messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

#endif
    }
}
