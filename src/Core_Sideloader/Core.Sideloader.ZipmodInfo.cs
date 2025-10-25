using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using MessagePack;
using Sideloader.ListLoader;
using UnityEngine;
using XUnity.ResourceRedirector;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader
{
    [MessagePackObject]
    internal class ZipmodInfo : IDisposable
    {
        [Key(0)] public Manifest Manifest;
        [Key(1)] public string FileName;
        [Key(2)] public string RelativeFileName;
        [Key(3)] public DateTime LastWriteTime;
        [Key(4)] public long FileSize;
        [Key(5)] public string Error;

        [Key(6)] public List<string> PngNames = new List<string>();
        [Key(7)] public List<ChaListData> CharaLists = new List<ChaListData>();
        [Key(8)] public List<BundleLoadInfo> BundleInfos = new List<BundleLoadInfo>();
#if !EC
        [Key(9)] public List<Lists.StudioListData> BoneLists = new List<Lists.StudioListData>();
        [Key(10)] public List<Lists.StudioListData> StudioLists = new List<Lists.StudioListData>();
        [Key(11)] public List<Lists.StudioListData> MapLists = new List<Lists.StudioListData>();
#endif

        [IgnoreMember] private ZipFile _zipFile;
        [IgnoreMember] public bool Loaded;
        [IgnoreMember] public bool Valid => Manifest != null && Error == null;

        [SerializationConstructor]
        public ZipmodInfo() { }

        public ZipmodInfo(string fileName)
        {
            FileName = fileName;
            RelativeFileName = Sideloader.GetRelativeArchiveDir(fileName);

            var fi = new FileInfo(fileName);
            LastWriteTime = fi.LastWriteTimeUtc;
            FileSize = fi.Length;
        }

        public ZipFile GetZipFile()
        {
            if (_zipFile == null)
            {
                _zipFile = new ZipFile(FileName);
            }
            else if (_zipFile.Count == 0)
            {
                // Disposing makes entry count = 0, but it should be at least 1 for the manifest.
                // Still try to dispose the old zipfile just to be safe.
                _zipFile.Close();
                _zipFile = new ZipFile(FileName);
            }

            return _zipFile;
        }

        public void Dispose()
        {
            if (_zipFile != null)
            {
                _zipFile.Close();
                _zipFile = null;
            }
        }

        private static readonly MethodInfo _LocateZipEntryMethodInfo = typeof(ZipFile).GetMethod("LocateEntry", AccessTools.all);
        public void LoadAllLists()
        {
            var zipmod = this;
            var arc = zipmod.GetZipFile();
            var manifest = zipmod.Manifest;

            foreach (ZipEntry entry in arc)
            {
                var fullName = entry.Name;
                // Find bundles in the archive
                if (fullName.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
                {
                    string assetBundlePath = fullName;

                    if (assetBundlePath.Contains('/'))
                        assetBundlePath = assetBundlePath.Remove(0, assetBundlePath.IndexOf('/') + 1);

                    if (entry.CompressionMethod == CompressionMethod.Stored)
                    {
                        long index = (long)_LocateZipEntryMethodInfo.Invoke(arc, new object[] { entry });
                        zipmod.BundleInfos.Add(new BundleLoadInfo(arc.Name, index, fullName, assetBundlePath));
                    }
                    else
                    {
                        zipmod.BundleInfos.Add(new BundleLoadInfo(arc.Name, -1, fullName, assetBundlePath));
                    }
                }
                // Find all list files in the archive
                else if (fullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (fullName.StartsWith("abdata/list/characustom", StringComparison.OrdinalIgnoreCase))
                        {
                            var stream = arc.GetInputStream(entry);
                            var chaListData = Lists.LoadCSV(stream);

                            SetPossessNew(chaListData);

                            zipmod.CharaLists.Add(chaListData);
                        }
#if !EC
                        else if (fullName.StartsWith("abdata/studio/info", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Path.GetFileNameWithoutExtension(fullName).ToLower().StartsWith("itembonelist_"))
                            {
                                var stream = arc.GetInputStream(entry);
                                var studioListData = Lists.LoadStudioCSV(stream, fullName, manifest.GUID);

                                zipmod.BoneLists.Add(studioListData);
                            }
                            else
                            {
                                var stream = arc.GetInputStream(entry);
                                var studioListData = Lists.LoadStudioCSV(stream, fullName, manifest.GUID);

                                zipmod.StudioLists.Add(studioListData);
                            }
                        }
#endif
#if AI || HS2
                        else if (fullName.StartsWith("abdata/list/map/", StringComparison.OrdinalIgnoreCase))
                        {
                            var stream = arc.GetInputStream(entry);
                            var data = Lists.LoadExcelDataCSV(stream, fullName);
                            zipmod.MapLists.Add(data);
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Sideloader.Logger.LogError($"Failed to load list file \"{fullName}\" from archive \"{Sideloader.GetRelativeArchiveDir(arc.Name)}\" with error: {ex}");
                    }
                }
                // Find all png files in the archive
                else if (fullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    // Only list folders for .pngs in abdata folder, i.e. skip preview pics or character cards that might be included with the mod
                    if (fullName.StartsWith("abdata/", StringComparison.OrdinalIgnoreCase))
                    {
                        zipmod.PngNames.Add(fullName);
                    }
                }
            }
        }

        private static void SetPossessNew(ChaListData data)
        {
            for (int i = 0; i < data.lstKey.Count; i++)
            {
                if (data.lstKey[i] == "Possess")
                {
                    foreach (var kv in data.dictList)
                        kv.Value[i] = "1";
                    break;
                }
            }
        }
    }

    [MessagePackObject]
    internal class BundleLoadInfo
    {
        [SerializationConstructor]
        public BundleLoadInfo(string archiveFilename, long streamOffset, string bundleFullPath, string bundleTrimmedPath)
        {
            ArchiveFilename = archiveFilename;
            StreamOffset = streamOffset;
            BundleFullPath = bundleFullPath;
            BundleTrimmedPath = bundleTrimmedPath;
        }
        [Key(0)] public string ArchiveFilename { get; }
        [Key(1)] public long StreamOffset { get; }
        [IgnoreMember] public bool CanBeStreamed => StreamOffset > 0;
        [Key(2)] public string BundleFullPath { get; }
        [Key(3)] public string BundleTrimmedPath { get; }

        public AssetBundle LoadBundle()
        {
            AssetBundle bundle;

            if (CanBeStreamed)
            {
                if (Sideloader.DebugLogging.Value)
                    Sideloader.Logger.LogDebug($"Streaming \"{BundleFullPath}\" ({Sideloader.GetRelativeArchiveDir(ArchiveFilename)}) unity3d file from disk, offset {StreamOffset}");

                bundle = AssetBundle.LoadFromFile(ArchiveFilename, 0, (ulong)StreamOffset);
            }
            else
            {
                Sideloader.Logger.LogDebug($"Cannot stream \"{BundleFullPath}\" ({Sideloader.GetRelativeArchiveDir(ArchiveFilename)}) unity3d file from disk, loading to RAM instead");

                var arc = Sideloader.Zipmods[ArchiveFilename].GetZipFile();
                var entry = arc.GetEntry(BundleFullPath);
                var stream = arc.GetInputStream(entry);

                byte[] buffer = new byte[entry.Size];

                _ = stream.Read(buffer, 0, (int)entry.Size);

                // The line below can either be commented in or out - it doesn't really matter. 
                //  - If in: It will generate successive unique CAB-strings for these asset bundles
                //  - If out: The CAB of the actual asset bundle will be used if possible, otherwise a random CAB is generated by the Resource Redirector due to the call to 'ResourceRedirection.EnableRandomizeCabIfConflict(-2000, false)'
                //BundleManager.RandomizeCAB(buffer);

                bundle = AssetBundleHelper.LoadFromMemory($"\"{BundleFullPath}\" ({Sideloader.GetRelativeArchiveDir(ArchiveFilename)})", buffer, 0);
            }

            if (bundle == null)
            {
                Sideloader.Logger.LogError($"Asset bundle \"{BundleFullPath}\" ({Sideloader.GetRelativeArchiveDir(ArchiveFilename)}) failed to load. It might have a conflicting CAB string.");
            }

            return bundle;
        }
    }
}
