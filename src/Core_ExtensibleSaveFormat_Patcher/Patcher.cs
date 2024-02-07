#if !RG
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#else
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.IL2CPP;
using BepInEx.Preloader.Core.Patching;
using BepisPlugins;
using Chara;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;
#endif

namespace ExtensibleSaveFormat
{
#if !RG
    public static class Patcher
    {
        public const string PluginName = "ExtensibleSaveFormat.Patcher";
        public static IEnumerable<string> TargetDLLs { get; } = new[]
        {
            "Assembly-CSharp.dll"
        };

        public static void Patch(AssemblyDefinition ass)
#else
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [PatcherPluginInfo(GUID, PluginName, Version)]
    public class Patcher : BasePatcher
    {
        /// <summary> Plugin GUID </summary>
        public const string GUID = "com.bepis.bepinex.extendedsave.patcher";
        /// <summary> Plugin name </summary>
        public const string PluginName = "ExtensibleSaveFormat.Patcher";
        /// <summary> Plugin version </summary>
        public const string Version = Metadata.PluginsVersion;

        public override void Initialize()
        {
            var path = Path.GetFullPath(Preloader.IL2CPPUnhollowedPath);
            if (!TypeLoader.CecilResolver.GetSearchDirectories().Any(item => Path.GetFullPath(item).Equals(path, StringComparison.OrdinalIgnoreCase)))
                TypeLoader.CecilResolver.AddSearchDirectory(path);
        }

        [TargetAssembly("Assembly-CSharp.dll")]
        private void Patch(AssemblyDefinition ass)
#endif
        {
            TypeDefinition messagePackObject;

#if KK || KKS || EC
            //Body
            messagePackObject = ass.MainModule.GetType("ChaFileBody");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Face
            messagePackObject = ass.MainModule.GetType("ChaFileFace");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileFace/PupilInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Hair
            messagePackObject = ass.MainModule.GetType("ChaFileHair");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileHair/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Clothes
            messagePackObject = ass.MainModule.GetType("ChaFileClothes");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileClothes/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileClothes/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Accessory
            messagePackObject = ass.MainModule.GetType("ChaFileAccessory");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileAccessory/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileStatus");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if KK || KKS
            messagePackObject = ass.MainModule.GetType("ChaFileMakeup");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Awnser");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Denial");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Attribute");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if KKS
            messagePackObject = ass.MainModule.GetType("ChaFileAbout");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Interest");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if EC

            messagePackObject = ass.MainModule.GetType("ChaFileFace/ChaFileMakeup");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //import messagepacks are different from normal messagepacks in-game
            #region Import MessagePacks
            //Body
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileBody");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Face
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileFace");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileFace/PupilInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Hair
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileHair");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileHair/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Clothes
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileClothes");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileClothes/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileClothes/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Accessory
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileAccessory");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileAccessory/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileStatus");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("KoikatsuCharaFile.ChaFileParameter");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            #endregion
#endif

#if AI || HS2
            //Body
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileBody");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Face
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileFace");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileFace/EyesInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileFace/MakeupInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Hair
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileHair");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileHair/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileHair/PartsInfo/BundleInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileHair/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Clothes
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileClothes");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileClothes/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileClothes/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Accessory
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileAccessory");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileAccessory/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileAccessory/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileGameInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileGameInfo/MinMaxInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileParameter");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileStatus");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if HS2
            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileGameInfo2");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("AIChara.ChaFileParameter2");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if RG
            //Body
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileBody)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Face
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileFace)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileFace)}/{nameof(ChaFileFace.EyesInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileFace)}/{nameof(ChaFileFace.MakeupInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Hair
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileHair)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileHair)}/{nameof(ChaFileHair.PartsInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileHair)}/{nameof(ChaFileHair.PartsInfo)}/{nameof(ChaFileHair.PartsInfo.BundleInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileHair)}/{nameof(ChaFileHair.PartsInfo)}/{nameof(ChaFileHair.PartsInfo.ColorInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Clothes
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileClothes)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileClothes)}/{nameof(ChaFileClothes.PartsInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileClothes)}/{nameof(ChaFileClothes.PartsInfo)}/{nameof(ChaFileClothes.PartsInfo.ColorInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            //Accessory
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileAccessory)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileAccessory)}/{nameof(ChaFileAccessory.PartsInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileAccessory)}/{nameof(ChaFileAccessory.PartsInfo)}/{nameof(ChaFileAccessory.PartsInfo.ColorInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileGameInfo)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileParameter)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType($"{nameof(Chara)}.{nameof(ChaFileStatus)}");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif
        }

        public static void PropertyInject(AssemblyDefinition assembly, TypeDefinition assemblyTypes, string propertyName, Type returnType)
        {
            //Import the return type
            var propertyType = assembly.MainModule.ImportReference(returnType);

            //define the field we store the value in
            var field = new FieldDefinition(ConvertToFieldName(propertyName), FieldAttributes.Private, propertyType);
            assemblyTypes.Fields.Add(field);

            //Create the get method
            var get = new MethodDefinition("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType);
            var getProcessor = get.Body.GetILProcessor();
            getProcessor.Append(getProcessor.Create(OpCodes.Ldarg_0));
            getProcessor.Append(getProcessor.Create(OpCodes.Ldfld, field));
            getProcessor.Append(getProcessor.Create(OpCodes.Stloc_0));
            var inst = getProcessor.Create(OpCodes.Ldloc_0);
            getProcessor.Append(getProcessor.Create(OpCodes.Br_S, inst));
            getProcessor.Append(inst);
            getProcessor.Append(getProcessor.Create(OpCodes.Ret));
            get.Body.Variables.Add(new VariableDefinition(propertyType));
            get.Body.InitLocals = true;
            get.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            assemblyTypes.Methods.Add(get);

            //Create the set method
            var set = new MethodDefinition("set_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, assembly.MainModule.ImportReference(typeof(void)));
            var setProcessor = set.Body.GetILProcessor();
            setProcessor.Append(setProcessor.Create(OpCodes.Ldarg_0));
            setProcessor.Append(setProcessor.Create(OpCodes.Ldarg_1));
            setProcessor.Append(setProcessor.Create(OpCodes.Stfld, field));
            setProcessor.Append(setProcessor.Create(OpCodes.Ret));
            set.Parameters.Add(new ParameterDefinition(propertyType) { Name = "value" });
            set.SemanticsAttributes = MethodSemanticsAttributes.Setter;
            assemblyTypes.Methods.Add(set);

            //create the property
            var propertyDefinition = new PropertyDefinition(propertyName, PropertyAttributes.None, propertyType) { GetMethod = get, SetMethod = set };

            //add the property to the type.
            assemblyTypes.Properties.Add(propertyDefinition);
        }

        private static string ConvertToFieldName(string propertyName)
        {
            var fieldName = new System.Text.StringBuilder();
            fieldName.Append("_");
            fieldName.Append(propertyName[0].ToString().ToLower());
            if (propertyName.Length > 1)
                fieldName.Append(propertyName.Substring(1));

            return fieldName.ToString();
        }
    }
}
