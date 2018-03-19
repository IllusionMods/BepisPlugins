using Harmony;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ExtensibleSaveFormat
{
    public static class Hooks
    {
        public static string Marker = "KKEx";
        public static int Version = 2;

        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.extensiblesaveformat");


            MethodInfo original = AccessTools.Method(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) });

            HarmonyMethod postfix = new HarmonyMethod(typeof(Hooks).GetMethod("SaveFileHook"));

            harmony.Patch(original, null, postfix);


            original = AccessTools.Method(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) });

            postfix = new HarmonyMethod(typeof(Hooks).GetMethod("LoadFileHook"));

            harmony.Patch(original, null, postfix);
        }

        public static void SaveFileHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
        {
            if (!__result)
                return;

            ExtensibleSaveFormat.writeEvent(__instance);

            Dictionary<string, PluginData> extendedData = ExtensibleSaveFormat.GetAllExtendedData(__instance);
            if (extendedData == null )
                return;

            byte[] bytes = MessagePackSerializer.Serialize(extendedData);

            bw.Write(Marker);
            bw.Write(Version);
            foreach (KeyValuePair<string, PluginData> kv in extendedData)
            {
                PluginData dict = kv.Value as PluginData;
            }

            bw.Write((int)bytes.Length);
            bw.Write(bytes);
        }

        public static void LoadFileHook(ChaFile __instance, bool __result, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
        {
            Dictionary<string, PluginData> dictionary = null;

            if (!__result)
                return;

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
            }
            catch (EndOfStreamException) { }
            catch (System.InvalidOperationException) { /* Invalid/unexpected deserialized data */ }

            if (dictionary == null)
            {
                //initialize a new dictionary since it doesn't exist
                dictionary = new Dictionary<string, PluginData>();
            }

            ExtensibleSaveFormat.internalDictionary.Set(__instance, dictionary);
            ExtensibleSaveFormat.readEvent(__instance);
        }
    }
}
