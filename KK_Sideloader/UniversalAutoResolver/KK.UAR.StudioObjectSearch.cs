using Studio;
using System;
using System.Collections.Generic;

namespace Sideloader.AutoResolver
{
    public class StudioObjectSearch
    {
        public enum SearchType { All, Import }
        /// <summary>
        /// Returns a dictionary of ObjectInfo.dicKey and their order in a scene for the specified ObjectInfo type.
        /// </summary>
        public static Dictionary<int, int> FindObjectInfoOrder(SearchType searchType, Type objectType)
        {
            Dictionary<int, ObjectInfo> dicObjectInfo = new Dictionary<int, ObjectInfo>();
            Dictionary<int, int> dicObjectInfoOrder = new Dictionary<int, int>();
            int ObjectOrder = 0;
            Dictionary<int, ObjectInfo> SearchList;

            if (searchType == SearchType.All)
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicObject;
            else //Import
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicImport;

            foreach (var kv in SearchList)
            {
                if (kv.Value.GetType() == objectType)
                    dicObjectInfoOrder.Add(kv.Key, ObjectOrder++);
                FindObjectsRecursive(kv.Value, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
            }

            return dicObjectInfoOrder;
        }
        /// <summary>
        /// Returns a dictionary of ObjectInfo.dicKey and ObjectInfo of every ObjectInfo in a scene
        /// </summary>
        public static Dictionary<int, ObjectInfo> FindObjectInfo(SearchType searchType)
        {
            Dictionary<int, ObjectInfo> dicObjectInfo = new Dictionary<int, ObjectInfo>();
            Dictionary<int, int> dicObjectInfoOrder = new Dictionary<int, int>();
            int ObjectOrder = 0;
            Dictionary<int, ObjectInfo> SearchList;

            if (searchType == SearchType.All)
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicObject;
            else //Import
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicImport;

            foreach (var kv in SearchList)
            {
                dicObjectInfo.Add(kv.Key, kv.Value);
                FindObjectsRecursive(kv.Value, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, null);
            }

            return dicObjectInfo;
        }
        /// <summary>
        /// Returns a dictionary of ObjectInfo.dicKey and ObjectInfo of every ObjectInfo in a scene.
        /// Also a dictionary of ObjectInfo.dicKey and their order in a scene for the specified ObjectInfo type as an out parameter.
        /// </summary>
        public static Dictionary<int, ObjectInfo> FindObjectInfoAndOrder(SearchType searchType, Type objectType, out Dictionary<int, int> dicObjectInfoOrder)
        {
            Dictionary<int, ObjectInfo> dicObjectInfo = new Dictionary<int, ObjectInfo>();
            dicObjectInfoOrder = new Dictionary<int, int>();
            int ObjectOrder = 0;
            Dictionary<int, ObjectInfo> SearchList;

            if (searchType == SearchType.All)
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicObject;
            else //Import
                SearchList = Singleton<Studio.Studio>.Instance.sceneInfo.dicImport;

            foreach (var kv in SearchList)
            {
                dicObjectInfo.Add(kv.Value.dicKey, kv.Value);
                if (objectType != null && kv.Value.GetType() == objectType)
                    dicObjectInfoOrder.Add(kv.Value.dicKey, ObjectOrder++);
                FindObjectsRecursive(kv.Value, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
            }

            return dicObjectInfo;
        }
        /// <summary>
        /// Function for finding all ObjectInfo recursively
        /// </summary>
        private static void FindObjectsRecursive(ObjectInfo objectInfo, ref Dictionary<int, ObjectInfo> dicObjectInfo, ref Dictionary<int, int> dicObjectInfoOrder, ref int ObjectOrder, Type objectType)
        {
            switch (objectInfo)
            {
                case OICharInfo charInfo:
                    foreach (var kv in charInfo.child)
                    {
                        foreach (ObjectInfo oi in kv.Value)
                        {
                            dicObjectInfo.Add(oi.dicKey, oi);
                            if (objectType != null && oi.GetType() == objectType)
                                dicObjectInfoOrder.Add(oi.dicKey, ObjectOrder++);
                            FindObjectsRecursive(oi, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
                        }
                    }
                    break;
                case OIItemInfo itemInfo:
                    foreach (ObjectInfo oi in itemInfo.child)
                    {
                        dicObjectInfo.Add(oi.dicKey, oi);
                        if (objectType != null && oi.GetType() == objectType)
                            dicObjectInfoOrder.Add(oi.dicKey, ObjectOrder++);
                        FindObjectsRecursive(oi, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
                    }
                    break;
                case OIFolderInfo folderInfo:
                    foreach (ObjectInfo oi in folderInfo.child)
                    {
                        dicObjectInfo.Add(oi.dicKey, oi);
                        if (objectType != null && oi.GetType() == objectType)
                            dicObjectInfoOrder.Add(oi.dicKey, ObjectOrder++);
                        FindObjectsRecursive(oi, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
                    }
                    break;
                case OIRouteInfo routeInfo:
                    foreach (ObjectInfo oi in routeInfo.child)
                    {
                        dicObjectInfo.Add(oi.dicKey, oi);
                        if (objectType != null && oi.GetType() == objectType)
                            dicObjectInfoOrder.Add(oi.dicKey, ObjectOrder++);
                        FindObjectsRecursive(oi, ref dicObjectInfo, ref dicObjectInfoOrder, ref ObjectOrder, objectType);
                    }
                    break;
                default:
                    //other types don't have children
                    //Note: If any other types are added in future updates and they can have chilren they need to be added here.
                    //Do a version check (Singleton<Studio.Studio>.Instance.sceneInfo.dataVersion) and figure out which types existed at the time the scene was made.
                    //Then only find objects that could have existed in that version so that the object order will still match on import.
                    break;
            }
        }
    }
}
