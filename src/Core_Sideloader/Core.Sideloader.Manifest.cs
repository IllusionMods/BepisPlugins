﻿using ICSharpCode.SharpZipLib.Zip;
using Sideloader.AutoResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using MessagePack;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader
{
    /// <summary>
    /// Contains data about the loaded manifest.xml
    /// </summary>
    [MessagePackObject]
    public partial class Manifest
    {
        [Key(0)] public int SchemaVer = 1;

        /// <summary>
        /// Full contents of the manifest.xml.
        /// </summary>
        [IgnoreMember]
        public readonly XDocument manifestDocument; //todo problem when deserializing
        /// <summary>
        /// GUID of the mod.
        /// </summary>
        [Key(1)]
        public string GUID { get; set; }
        /// <summary>
        /// Name of the mod. Only used for display the name of the mod when mods are loaded.
        /// </summary>
        [Key(2)]
        public string Name { get; set; }
        /// <summary>
        /// Version of the mod.
        /// </summary>
        [Key(3)]
        public string Version { get; set; }
        /// <summary>
        /// Author of the mod. Not currently used for anything.
        /// </summary>
        [Key(4)]
        public string Author { get; set; }
        /// <summary>
        /// Website of the mod. Not currently used for anything.
        /// </summary>
        [Key(5)]
        public string Website { get; set; }
        /// <summary>
        /// Description of the mod. Not currently used for anything.
        /// </summary>
        [Key(7)]
        public string Description { get; set; }

        /// <summary>
        /// Game the mod is made for. If specified, the mod will only load for that game. If not specified will load on any game.
        /// </summary>
        [Obsolete("Use Games instead")]
        [IgnoreMember]
        public string Game => Games.FirstOrDefault();

        /// <summary>
        /// Games the mod is made for. If specified, the mod will only load for those games. If not specified will load on any game.
        /// </summary>
        [Key(8)]
        public List<string> Games { get; set; } = new List<string>();
        /// <summary>
        /// List of all migration info for this mod
        /// </summary>
        [Key(9)]
        public List<MigrationInfo> MigrationList = new List<MigrationInfo>();

#if AI || HS2
        [Key(10)]
        public List<HeadPresetInfo> HeadPresetList = new List<HeadPresetInfo>();
        [Key(11)]
        public List<FaceSkinInfo> FaceSkinList = new List<FaceSkinInfo>();
#endif

        public Manifest() { }

        internal Manifest(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
                manifestDocument = XDocument.Load(reader);


            if (manifestDocument?.Root?.Attribute("schema-ver")?.Value != SchemaVer.ToString())
                throw new OperationCanceledException("Manifest.xml is in an invalid format");

            foreach (var element in manifestDocument.Root?.Elements("game"))
                if (element != null && !element.Value.IsNullOrWhiteSpace())
                    Games.Add(element.Value);

            Description = manifestDocument.Root?.Element("description")?.Value;
            Website = manifestDocument.Root?.Element("website")?.Value;
            Author = manifestDocument.Root?.Element("author")?.Value;
            Version = manifestDocument.Root?.Element("version")?.Value;
            Name = manifestDocument.Root?.Element("name")?.Value;
            GUID = manifestDocument.Root?.Element("guid")?.Value;
        }

        private void LoadMigrationInfo()
        {
            if (manifestDocument.Root?.Element("migrationInfo") == null) return;

            foreach (var info in manifestDocument.Root.Element("migrationInfo").Elements("info"))
            {
                try
                {
                    MigrationType migrationType;
                    if (info.Attribute("migrationType")?.Value == null || info.Attribute("migrationType").Value.IsNullOrWhiteSpace())
                        migrationType = MigrationType.Migrate;
                    else
                        migrationType = (MigrationType)Enum.Parse(typeof(MigrationType), info.Attribute("migrationType").Value);

                    string guidOld = info.Attribute("guidOld")?.Value;
                    string guidNew = info.Attribute("guidNew")?.Value;
                    if (guidNew.IsNullOrWhiteSpace())
                        guidNew = GUID;

                    if (guidOld.IsNullOrEmpty())
                        throw new Exception("guidOld must be specified for migration.");
                    if (guidNew.IsNullOrEmpty() && migrationType == MigrationType.Migrate)
                        throw new Exception("guidNew must be specified for migration.");

                    if (migrationType == MigrationType.MigrateAll || migrationType == MigrationType.StripAll)
                    {
                        MigrationList.Add(new MigrationInfo(migrationType, guidOld, guidNew));
                        continue;
                    }

                    if (info.Attribute("category")?.Value == null)
                        throw new Exception("Category must be specified for migration.");

                    ChaListDefine.CategoryNo category = (ChaListDefine.CategoryNo)Enum.Parse(typeof(ChaListDefine.CategoryNo), info.Attribute("category").Value);

                    if (!int.TryParse(info.Attribute("idOld").Value, out int idOld) && migrationType == MigrationType.Migrate)
                        throw new Exception("ID must be specified for migration.");
                    if (!int.TryParse(info.Attribute("idNew").Value, out int idNew) && migrationType == MigrationType.Migrate)
                        throw new Exception("ID must be specified for migration.");

                    MigrationList.Add(new MigrationInfo(migrationType, category, guidOld, guidNew, idOld, idNew));
                }
                catch (Exception ex)
                {
                    Sideloader.Logger.LogError($"Could not load migration data for {GUID}, skipping line.");
                    Sideloader.Logger.LogError(ex);
                }
            }
        }

        internal static bool TryLoadFromZip(ZipFile zip, out Manifest manifest)
        {
            manifest = null;
            try
            {
                manifest = LoadFromZip(zip);
                return true;
            }
            catch (SystemException ex)
            {
                Sideloader.Logger.LogWarning($"Cannot load {Path.GetFileName(zip.Name)} - {ex.Message}.");
                if (!(ex is OperationCanceledException))
                    Sideloader.Logger.LogDebug("Error details: " + ex);
                return false;
            }
        }

        internal static Manifest LoadFromZip(ZipFile zip)
        {
            ZipEntry entry = zip.GetEntry("manifest.xml");

            if (entry == null)
                throw new OperationCanceledException("Manifest.xml is missing, make sure this is a zipmod");

            var manifest = new Manifest(zip.GetInputStream(entry));

            if (manifest.GUID == null)
                throw new OperationCanceledException("Manifest.xml is missing the GUID");

            manifest.LoadMigrationInfo();
#if AI || HS2
            manifest.LoadHeadPresetInfo();
            manifest.LoadFaceSkinInfo();
#endif
            return manifest;
        }

#if AI || HS2
        private void LoadHeadPresetInfo()
        {
            foreach (var info in manifestDocument.Root.Elements("headPresetInfo"))
            {
                try
                {
                    string preset = info.Attribute("preset")?.Value;
                    string headID = info.Element("headID")?.Value;
                    string headGUID = info.Element("headGUID")?.Value;
                    string skinGUID = info.Element("skinGUID")?.Value;
                    string detailGUID = info.Element("detailGUID")?.Value;
                    string eyebrowGUID = info.Element("eyebrowGUID")?.Value;
                    string pupil1GUID = info.Element("pupil1GUID")?.Value;
                    string pupil2GUID = info.Element("pupil2GUID")?.Value;
                    string black1GUID = info.Element("black1GUID")?.Value;
                    string black2GUID = info.Element("black2GUID")?.Value;
                    string hlGUID = info.Element("hlGUID")?.Value;
                    string eyelashesGUID = info.Element("eyelashesGUID")?.Value;
                    string moleGUID = info.Element("moleGUID")?.Value;
                    string eyeshadowGUID = info.Element("eyeshadowGUID")?.Value;
                    string cheekGUID = info.Element("cheekGUID")?.Value;
                    string lipGUID = info.Element("lipGUID")?.Value;
                    string paint1GUID = info.Element("paint1GUID")?.Value;
                    string paint2GUID = info.Element("paint2GUID")?.Value;
                    string layout1GUID = info.Element("layout1GUID")?.Value;
                    string layout2GUID = info.Element("layout2GUID")?.Value;

                    HeadPresetInfo headPresetInfo = new HeadPresetInfo();

                    if (preset.IsNullOrWhiteSpace())
                        throw new Exception("Preset must be specified.");
                    if (!int.TryParse(headID, out int headIDInt))
                        throw new Exception("HeadID must be specified.");
                    headPresetInfo.Preset = preset;
                    headPresetInfo.HeadID = headIDInt;
                    headPresetInfo.HeadGUID = headGUID.IsNullOrWhiteSpace() ? null : headGUID;
                    headPresetInfo.SkinGUID = skinGUID.IsNullOrWhiteSpace() ? null : skinGUID;
                    headPresetInfo.DetailGUID = detailGUID.IsNullOrWhiteSpace() ? null : detailGUID;
                    headPresetInfo.EyebrowGUID = eyebrowGUID.IsNullOrWhiteSpace() ? null : eyebrowGUID;
                    headPresetInfo.Pupil1GUID = pupil1GUID.IsNullOrWhiteSpace() ? null : pupil1GUID;
                    headPresetInfo.Pupil2GUID = pupil2GUID.IsNullOrWhiteSpace() ? null : pupil2GUID;
                    headPresetInfo.Black1GUID = black1GUID.IsNullOrWhiteSpace() ? null : black1GUID;
                    headPresetInfo.Black2GUID = black2GUID.IsNullOrWhiteSpace() ? null : black2GUID;
                    headPresetInfo.HlGUID = hlGUID.IsNullOrWhiteSpace() ? null : hlGUID;
                    headPresetInfo.EyelashesGUID = eyelashesGUID.IsNullOrWhiteSpace() ? null : eyelashesGUID;
                    headPresetInfo.MoleGUID = moleGUID.IsNullOrWhiteSpace() ? null : moleGUID;
                    headPresetInfo.EyeshadowGUID = eyeshadowGUID.IsNullOrWhiteSpace() ? null : eyeshadowGUID;
                    headPresetInfo.CheekGUID = cheekGUID.IsNullOrWhiteSpace() ? null : cheekGUID;
                    headPresetInfo.LipGUID = lipGUID.IsNullOrWhiteSpace() ? null : lipGUID;
                    headPresetInfo.Paint1GUID = paint1GUID.IsNullOrWhiteSpace() ? null : paint1GUID;
                    headPresetInfo.Paint2GUID = paint2GUID.IsNullOrWhiteSpace() ? null : paint2GUID;
                    headPresetInfo.Layout1GUID = layout1GUID.IsNullOrWhiteSpace() ? null : layout1GUID;
                    headPresetInfo.Layout2GUID = layout2GUID.IsNullOrWhiteSpace() ? null : layout2GUID;
                    headPresetInfo.Init();
                    HeadPresetList.Add(headPresetInfo);
                }
                catch (Exception ex)
                {
                    Sideloader.Logger.LogError($"Could not load head preset data for {GUID}, skipping line.");
                    Sideloader.Logger.LogError(ex);
                }
            }
        }

        private void LoadFaceSkinInfo()
        {
            foreach (var info in manifestDocument.Root.Elements("faceSkinInfo"))
            {
                try
                {
                    string skinID = info.Attribute("skinID")?.Value;
                    string headID = info.Attribute("headID")?.Value;
                    string headGUID = info.Attribute("headGUID")?.Value;

                    if (!int.TryParse(skinID, out int skinIDInt))
                        throw new Exception("SkinID must be specified.");
                    if (!int.TryParse(headID, out int headIDInt))
                        throw new Exception("HeadID must be specified.");
                    if (headGUID.IsNullOrWhiteSpace())
                        throw new Exception("HeadGUID must be specified.");

                    FaceSkinInfo faceSkinInfo = new FaceSkinInfo(skinIDInt, GUID, headIDInt, headGUID);
                    FaceSkinList.Add(faceSkinInfo);
                }
                catch (Exception ex)
                {
                    Sideloader.Logger.LogError($"Could not load face skin data for {GUID}, skipping line.");
                    Sideloader.Logger.LogError(ex);
                }
            }
        }
#endif
    }
}
