using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Sideloader
{
    public static class BundleManager
    {
        public static Dictionary<string, Lazy<AssetBundle>> Bundles = new Dictionary<string, Lazy<AssetBundle>>();


        public static string DummyPath => "list/characustom/00.unity3d";
        
        private static long CABCounter = 0;

        public static string GenerateCAB()
        {
            StringBuilder sb = new StringBuilder("CAB-", 36);

            sb.Append(Interlocked.Increment(ref CABCounter).ToString("x32"));

            return sb.ToString();
        }

        public static void RandomizeCAB(byte[] assetBundleData)
        {
            string ascii = Encoding.ASCII.GetString(assetBundleData, 0, 256);

            int cabIndex = ascii.IndexOf("CAB-");

            if (cabIndex < 0)
                return;

            string CAB = GenerateCAB();
            byte[] cabBytes = Encoding.ASCII.GetBytes(CAB);

            Buffer.BlockCopy(cabBytes, 0, assetBundleData, cabIndex, 36);
        }

        public static void AddBundleLoader(Func<AssetBundle> func, string path)
        {
            Bundles.Add(path, Lazy<AssetBundle>.Create(func));
        }

        public static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            bool result =  TryGetObjectFromName(name, assetBundle, typeof(T), out UnityEngine.Object tObj);

            obj = (T)tObj;

            return result;
        }

        public static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out Lazy<AssetBundle> lazyBundle))
            {
                AssetBundle bundle = (AssetBundle)lazyBundle;

                if (bundle.Contains(name))
                {
                    obj = bundle.LoadAsset(name, type);
                    return true;
                }
            }

            return false;
        }
    }
}
