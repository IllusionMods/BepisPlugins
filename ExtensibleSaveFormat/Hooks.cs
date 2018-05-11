using Harmony;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;

namespace ExtensibleSaveFormat
{
	public static class Hooks
	{
		public static string Marker = "KKEx";
		public static int Version = 3;

		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.extensiblesaveformat");
			harmony.PatchAll(typeof(Hooks));
		}


        #region Saving
        private static byte[] currentlySavingData = null;

	    [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
	    public static void SaveFilePreHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
	    {
	        ExtendedSave.writeEvent(__instance);
	    }

	    public static void SaveFileHook(ChaFile file, BlockHeader header, ref long[] array3)
	    {
            Dictionary<string, PluginData> extendedData = ExtendedSave.GetAllExtendedData(file);
	        if (extendedData == null)
	        {
	            currentlySavingData = null;
	            return;
	        }

	        currentlySavingData = MessagePackSerializer.Serialize(extendedData);

            //get offset
	        long offset = array3.Sum();
	        long length = currentlySavingData.LongLength;

            //insert our custom data length at the end
            Array.Resize(ref array3, array3.Length + 1);
	        array3[array3.Length - 1] = length;

            //add info about our data to the block header
	        BlockHeader.Info info = new BlockHeader.Info
	        {
                name = Marker,
                version = Version.ToString(),
                pos = offset,
                size = length
	        };

	        header.lstInfo.Add(info);
	    }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static void SaveFilePostHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
        {
            if (!__result || currentlySavingData == null)
                return;
            
            bw.Write(currentlySavingData);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static IEnumerable<CodeInstruction> SaveFileTranspiler(IEnumerable<CodeInstruction> instructions)
	    {
            List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

            //get the index of the last blockheader creation
	        int blockHeaderIndex = newInstructionSet.FindLastIndex(instruction => CheckNewObjTypeName(instruction, "BlockHeader"));

            //get the index of array3 (which contains data length info)
            int array3Index = newInstructionSet.FindIndex(instruction =>
            {
                //find first int64 array
                return instruction.opcode == OpCodes.Newarr &&
                       instruction.operand.ToString() == "System.Int64";
            });

            LocalBuilder blockHeaderLocalBuilder = (LocalBuilder)newInstructionSet[blockHeaderIndex + 1].operand; //get the local index for the block header
	        LocalBuilder array3LocalBuilder = (LocalBuilder)newInstructionSet[array3Index + 1].operand; //get the local index for array3

            //insert our own hook right after the blockheader creation
            newInstructionSet.InsertRange(blockHeaderIndex + 2, //we insert AFTER the NEXT instruction, which is the store local for the blockheader
	            new[] {
	                new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
	                new CodeInstruction(OpCodes.Ldloc_S, blockHeaderLocalBuilder), //push the BlockHeader instance 
	                new CodeInstruction(OpCodes.Ldloca_S, array3LocalBuilder), //push the array3 instance as ref
                    new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(SaveFileHook))), //call our hook
	            });

	        return newInstructionSet;
	    }
        #endregion

        #region Loading

	    public static void LoadFileHook(ChaFile file, BlockHeader header, BinaryReader reader)
	    {
	        var info = header.SearchInfo(Marker);

	        if (info != null && info.version == Version.ToString())
	        {
	            long originalPosition = reader.BaseStream.Position;
	            long basePosition = originalPosition - header.lstInfo.Sum(x => x.size);

	            reader.BaseStream.Position = basePosition + info.pos;


	            byte[] data = reader.ReadBytes((int)info.size);

	            reader.BaseStream.Position = originalPosition;


	            var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(data);

	            ExtendedSave.internalDictionary.Set(file, dictionary);
	        }
	        else
	        {
	            ExtendedSave.internalDictionary.Set(file, new Dictionary<string, PluginData>());
	        }
	    }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
        public static IEnumerable<CodeInstruction> LoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
	    {
            List<CodeInstruction> newInstructionSet = new List<CodeInstruction>(instructions);

            //get the index of the first searchinfo call
	        int searchInfoIndex = newInstructionSet.FindIndex(instruction => CheckCallVirtName(instruction, "SearchInfo"));

	        //get the index of the last seek call
	        int lastSeekIndex = newInstructionSet.FindLastIndex(instruction => CheckCallVirtName(instruction, "Seek"));

            LocalBuilder blockHeaderLocalBuilder = (LocalBuilder)newInstructionSet[searchInfoIndex - 2].operand; //get the localbuilder for the blockheader

            //insert our own hook right after the last seek
            newInstructionSet.InsertRange(lastSeekIndex + 2, //we insert AFTER the NEXT instruction, which is right before the try block exit
                new[] {
                    new CodeInstruction(OpCodes.Ldarg_0), //push the ChaFile instance
                    new CodeInstruction(OpCodes.Ldloc_S, blockHeaderLocalBuilder), //push the BlockHeader instance
                    new CodeInstruction(OpCodes.Ldarg_1, blockHeaderLocalBuilder), //push the binaryreader instance
                        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(LoadFileHook))), //call our hook
                });

            return newInstructionSet;
	    }

	    [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
	    public static void LoadFilePostHook(ChaFile __instance, bool __result, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
	    {
	        if (!__result)
	            return;


            //Compatibility for ver 1 and 2 ext save data
	        if (br.BaseStream.Position != br.BaseStream.Length)
	        {
	            long originalPosition = br.BaseStream.Position;

	            try
	            {
	                string marker = br.ReadString();
	                int version = br.ReadInt32();

	                if (marker == "KKEx" && version == 2)
	                {
	                    int length = br.ReadInt32();

	                    if (length > 0)
	                    {
	                        byte[] bytes = br.ReadBytes(length);
	                        var dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);

	                        ExtendedSave.internalDictionary.Set(__instance, dictionary);
	                    }
	                }
	                else
	                {
	                    br.BaseStream.Position = originalPosition;
	                }
	            }
	            catch (EndOfStreamException)
	            {
	                /* Incomplete/non-existant data */
	            }
	            catch (InvalidOperationException)
	            {
	                /* Invalid/unexpected deserialized data */
	            }
	        }
	        
	        ExtendedSave.readEvent(__instance);
	    }
	    #endregion

        
	    #region Helper

	    public static bool CheckCallVirtName(CodeInstruction instruction, string name)
	    {
	        return instruction.opcode == OpCodes.Callvirt &&
	               //need to do reflection fuckery here because we can't access MonoMethod which is the operand type, not MehtodInfo like normal reflection
	               instruction.operand.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod().Invoke(instruction.operand, null).ToString().ToString() == name;
	    }

	    public static bool CheckNewObjTypeName(CodeInstruction instruction, string name)
	    {
	        return instruction.opcode == OpCodes.Newobj &&
	               //need to do reflection fuckery here because we can't access MonoCMethod which is the operand type, not ConstructorInfo like normal reflection
	               instruction.operand.GetType().GetProperty("DeclaringType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod().Invoke(instruction.operand, null).ToString() == name;
	    }

	    #endregion
	}
}