using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace ExtensibleSaveFormat
{
    public static class Patcher
    {
        public const string PluginName = "ExtensibleSaveFormat.Patcher";
        public static IEnumerable<string> TargetDLLs { get; } = new[]
        {
            "Assembly-CSharp.dll"
        };

        public static void Patch(AssemblyDefinition ass)
        {
            TypeDefinition messagePackObject;

#if KK || EC
            messagePackObject = ass.MainModule.GetType("ChaFileAccessory");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileAccessory/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileClothes");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileClothes/PartsInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileClothes/PartsInfo/ColorInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileStatus");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileFace");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileFace/PupilInfo");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#endif

#if KK
            //KK
            messagePackObject = ass.MainModule.GetType("ChaFileMakeup");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));

            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Awnser");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Denial");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
            messagePackObject = ass.MainModule.GetType("ChaFileParameter/Attribute");
            PropertyInject(ass, messagePackObject, ExtendedSave.ExtendedSaveDataPropertyName, typeof(object));
#elif EC
            messagePackObject = ass.MainModule.GetType("ChaFileFace/ChaFileMakeup");
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