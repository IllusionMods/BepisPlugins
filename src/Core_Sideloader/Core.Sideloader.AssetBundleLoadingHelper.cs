using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Sideloader
{
    internal static class AssetBundleLoadingHelper
    {
        public static AssetBundle LoadFromFileWithRandomizedCabIfRequired(string path, uint crc, ulong offset)
        {
            var bundle = AssetBundle.LoadFromFile(path, crc, offset);
            if (bundle == null && File.Exists(path))
            {
                var buffer = File.ReadAllBytes(path);
                RandomizeCabWithAnyLength(buffer);

                Sideloader.Logger.LogWarning($"Randomized CAB for '{path}' in order to load it because another asset bundle already uses its CAB-string. You can ignore the previous error message, but this is likely caused by two mods incorrectly using the same CAB-string.");

                return AssetBundle.LoadFromMemory(buffer);
            }
            else
            {
                return bundle;
            }
        }
        private static void RandomizeCabWithAnyLength(byte[] assetBundleData)
        {
            FindAndReplaceCab("CAB-", 0, assetBundleData, 2048);
        }

        private static void FindAndReplaceCab(string ansiStringToStartWith, byte byteToEndWith, byte[] data, int maxIterations = -1)
        {
            var len = Math.Min(data.Length, maxIterations);
            if (len == -1)
            {
                len = data.Length;
            }

            int pos = 0;
            char c;
            byte b;
            int searchLen = ansiStringToStartWith.Length;
            var newCab = Guid.NewGuid().ToString("N");
            int cabIdx = 0;

            for (int i = 0; i < len; i++)
            {
                b = data[i];
                c = (char)b;

                if (pos == searchLen)
                {
                    while ((data[i]) != byteToEndWith)
                    {
                        if (cabIdx >= newCab.Length)
                        {
                            cabIdx = 0;
                            newCab = Guid.NewGuid().ToString("N");
                        }

                        data[i++] = (byte)newCab[cabIdx++];
                    }

                    break;
                }
                else if (c == ansiStringToStartWith[pos])
                {
                    pos++;
                }
                else
                {
                    pos = 0;
                }
            }
        }
    }
}
