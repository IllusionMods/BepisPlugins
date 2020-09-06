using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Patch for loading h/common/ stuff for Sideloader maps
            /// </summary>
            internal static void LoadAllFolderPostfix(string _findFolder, string _strLoadFile, ref List<Object> __result)
            {
                if (__result.Count() == 0 && (_findFolder == "h/common/" || _findFolder == "vr/common/"))
                    foreach (var kvp in BundleManager.Bundles.Where(x => x.Key.StartsWith(_findFolder)))
                        foreach (var lazyList in kvp.Value)
                            foreach (var assetName in lazyList.Instance.GetAllAssetNames())
                                if (_strLoadFile.ToLower() == Path.GetFileNameWithoutExtension(assetName.ToLower()))
                                {
                                    GameObject go = CommonLib.LoadAsset<GameObject>(kvp.Key, assetName);
                                    if (go)
                                        __result.Add(go);
                                }
            }
        }
    }
}