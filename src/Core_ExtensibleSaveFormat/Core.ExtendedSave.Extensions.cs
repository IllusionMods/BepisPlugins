using HarmonyLib;
using System.Collections.Generic;
using static ExtensibleSaveFormat.ExtendedSave;

#pragma warning disable CS1591

namespace ExtensibleSaveFormat
{
    public static class Extensions
    {
        private static Dictionary<string, PluginData> GetExtendedData(object messagePackObject)
        {
            try
            {
                if (messagePackObject == null) throw new System.ArgumentNullException(nameof(messagePackObject));

                var tv = Traverse.Create(messagePackObject);
                var prop = tv.Property(ExtendedSaveDataPropertyName);

                if (!prop.PropertyExists())
                    throw new System.NotSupportedException($"The type '{messagePackObject?.GetType()}' does not have the '{ExtendedSaveDataPropertyName}' property. Make sure the extended save patcher is installed and working.");

                var bytes = (byte[])prop.GetValue();
                if (bytes != null)
                {
                    return MessagePackDeserialize<Dictionary<string, PluginData>>(bytes);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
            return null;
        }

        private static bool GetExtendedData(object messagePackObject, string id, out PluginData data)
        {
            var pluginData = GetExtendedData(messagePackObject);

            if (pluginData != null && pluginData.TryGetValue(id, out data))
                return true;
            data = null;
            return false;
        }

        private static void SetExtendedData(object messagePackObject, string id, PluginData data)
        {
            var pluginData = GetExtendedData(messagePackObject);
            if (pluginData == null) pluginData = new Dictionary<string, PluginData>();

            if (data == null)
                pluginData.Remove(id);
            else
                pluginData[id] = data;

            try
            {
                var bytes = MessagePackSerialize(pluginData);

                Traverse.Create(messagePackObject).Property(ExtendedSaveDataPropertyName).SetValue(bytes);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        //currently only used to transfer data for EC because data is manually converted for EC format and messagepack objects are different
        internal static void TransferSerializedExtendedData(this object destination, object source)
        {
            try
            {
                if (source == null) throw new System.ArgumentNullException(nameof(source));

                var tv = Traverse.Create(source);
                var prop = tv.Property(ExtendedSaveDataPropertyName);

                if (!prop.PropertyExists())
                    throw new System.NotSupportedException($"The type '{source?.GetType()}' does not have the '{ExtendedSaveDataPropertyName}' property. Make sure the extended save patcher is installed and working.");

                var data = prop.GetValue();
                if (data == null) return;
                Traverse.Create(destination).Property(ExtendedSaveDataPropertyName).SetValue(data);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex);
            }
        }
#if KK || KKS || EC
        //Body
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileBody messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileBody messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileBody messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Face
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileFace messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileFace.PupilInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.PupilInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Hair
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileHair messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileHair messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileHair messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileHair.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileHair.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileHair.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Clothes
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileClothes messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileClothes.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Accessory
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileAccessory messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileAccessory.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileStatus messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileParameter messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if KK || KKS
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileMakeup messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileParameter.Attribute messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Attribute messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileParameter.Awnser messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Awnser messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileParameter.Denial messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Denial messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if KKS
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileAbout messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileAbout messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileAbout messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileParameter.Interest messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileParameter.Interest messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileParameter.Interest messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if EC
        public static Dictionary<string, PluginData> GetAllExtendedData(this ChaFileFace.ChaFileMakeup messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this ChaFileFace.ChaFileMakeup messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if AI || HS2
        //Body
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileBody messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileBody messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileBody messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Face
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileFace messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileFace.EyesInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace.EyesInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace.EyesInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileFace.MakeupInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileFace.MakeupInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileFace.MakeupInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Hair
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileHair messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileHair.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileHair.PartsInfo.BundleInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileHair.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Clothes
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileClothes messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileClothes.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Accessory
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileAccessory messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileAccessory.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileGameInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileGameInfo.MinMaxInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo.MinMaxInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo.MinMaxInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileParameter messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileStatus messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if HS2
        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileGameInfo2 messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileGameInfo2 messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileGameInfo2 messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this AIChara.ChaFileParameter2 messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this AIChara.ChaFileParameter2 messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this AIChara.ChaFileParameter2 messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif

#if RG
        //Body
        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileBody messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileBody messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileBody messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Face
        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileFace messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileFace messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileFace messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileFace.EyesInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileFace.EyesInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileFace.EyesInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileFace.MakeupInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileFace.MakeupInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileFace.MakeupInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Hair
        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileHair messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileHair messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileHair messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileHair.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileHair.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileHair.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileHair.PartsInfo.BundleInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileHair.PartsInfo.BundleInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileHair.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileHair.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Clothes
        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileClothes messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileClothes messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileClothes messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileClothes.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileClothes.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileClothes.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileClothes.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        //Accessory
        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileAccessory messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileAccessory messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileAccessory messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileAccessory.PartsInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileAccessory.PartsInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileAccessory.PartsInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileAccessory.PartsInfo.ColorInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileGameInfo messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileGameInfo messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileGameInfo messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileParameter messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileParameter messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileParameter messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);

        public static Dictionary<string, PluginData> GetAllExtendedData(this Chara.ChaFileStatus messagePackObject) => GetExtendedData(messagePackObject);
        public static bool TryGetExtendedDataById(this Chara.ChaFileStatus messagePackObject, string id, out PluginData data) => GetExtendedData(messagePackObject, id, out data);
        public static void SetExtendedDataById(this Chara.ChaFileStatus messagePackObject, string id, PluginData data) => SetExtendedData(messagePackObject, id, data);
#endif
    }
}
