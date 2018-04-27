using Harmony;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExtensibleSaveFormat
{
	public static class Hooks
	{
		public static string Marker = "KKEx";
		public static int Version = 2;

		public static void InstallHooks()
		{
			var harmony = HarmonyInstance.Create("com.bepis.bepinex.extensiblesaveformat");
			harmony.PatchAll(typeof(Hooks));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) })]
		public static void SaveFileHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
		{
			if (!__result)
				return;

			ExtendedSave.writeEvent(__instance);

			Dictionary<string, PluginData> extendedData = ExtendedSave.GetAllExtendedData(__instance);
			if (extendedData == null)
				return;

			byte[] bytes = MessagePackSerializer.Serialize(extendedData);

			bw.Write(Marker);
			bw.Write(Version);
			foreach (KeyValuePair<string, PluginData> kv in extendedData)
			{
				PluginData dict = kv.Value as PluginData;
			}

			bw.Write((int) bytes.Length);
			bw.Write(bytes);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] {typeof(BinaryReader), typeof(bool), typeof(bool)})]
		public static void LoadFileHook(ChaFile __instance, bool __result, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
		{
			Dictionary<string, PluginData> dictionary = null;

			if (!__result)
				return;

			if (br.BaseStream.Position != br.BaseStream.Length)
			{
			    long originalPosition = br.BaseStream.Position;

				try
				{
					string marker = br.ReadString();
					int version = br.ReadInt32();

					if (marker == Marker && version == Version)
					{
						int length = br.ReadInt32();

						if (length > 0)
						{
							byte[] bytes = br.ReadBytes(length);
							dictionary = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(bytes);
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

			if (dictionary == null)
			{
				//initialize a new dictionary since it doesn't exist
				dictionary = new Dictionary<string, PluginData>();
			}

			ExtendedSave.internalDictionary.Set(__instance, dictionary);
			ExtendedSave.readEvent(__instance);
		}
	}
}