using ICSharpCode.SharpZipLib.Zip;
using Sideloader.AutoResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
#if AI
using AIChara;
#endif

namespace Sideloader
{
    /// <summary>
    /// Contains data about the loaded manifest.xml
    /// </summary>
    public class Manifest
    {
        private readonly int SchemaVer = 1;

        /// <summary>
        /// Full contents of the manifest.xml.
        /// </summary>
        public readonly XDocument manifestDocument;
        /// <summary>
        /// GUID of the mod.
        /// </summary>
        public string GUID => manifestDocument.Root?.Element("guid")?.Value;
        /// <summary>
        /// Name of the mod. Only used for display the name of the mod when mods are loaded.
        /// </summary>
        public string Name => manifestDocument.Root?.Element("name")?.Value;
        /// <summary>
        /// Version of the mod.
        /// </summary>
        public string Version => manifestDocument.Root?.Element("version")?.Value;
        /// <summary>
        /// Author of the mod. Not currently used for anything.
        /// </summary>
        public string Author => manifestDocument.Root?.Element("author")?.Value;
        /// <summary>
        /// Website of the mod. Not currently used for anything.
        /// </summary>
        public string Website => manifestDocument.Root?.Element("website")?.Value;
        /// <summary>
        /// Description of the mod. Not currently used for anything.
        /// </summary>
        public string Description => manifestDocument.Root?.Element("description")?.Value;
        /// <summary>
        /// Game the mod is made for. If specified, the mod will only load for that game. If not specified will load on any game.
        /// </summary>
        public string Game => manifestDocument.Root?.Element("game")?.Value;
        /// <summary>
        /// List of all migration info for this mod
        /// </summary>
        public List<MigrationInfo> MigrationList = new List<MigrationInfo>();

        internal Manifest(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
                manifestDocument = XDocument.Load(reader);
        }

        internal void LoadMigrationInfo()
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

                    ChaListDefine.CategoryNo category = (ChaListDefine.CategoryNo)Enum.Parse(typeof(ChaListDefine.CategoryNo), info.Attribute("category").Value);
                    string guidOld = info.Attribute("guidOld")?.Value;
                    string guidNew = info.Attribute("guidNew")?.Value;

                    if (!int.TryParse(info.Attribute("idOld").Value, out int idOld) && migrationType == MigrationType.Migrate)
                        throw new Exception("ID must be specified for migration.");
                    if (!int.TryParse(info.Attribute("idNew").Value, out int idNew) && migrationType == MigrationType.Migrate)
                        throw new Exception("ID must be specified for migration.");
                    if (guidOld.IsNullOrEmpty())
                        throw new Exception("guidOld must be specified for migration.");
                    if (guidNew.IsNullOrEmpty() && migrationType == MigrationType.Migrate)
                        throw new Exception("guidNew must be specified for migration.");

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
                ZipEntry entry = zip.GetEntry("manifest.xml");

                if (entry == null)
                    throw new OperationCanceledException("Manifest.xml is missing, make sure this is a zipmod");

                manifest = new Manifest(zip.GetInputStream(entry));

                if (manifest.manifestDocument?.Root?.Attribute("schema-ver")?.Value != manifest.SchemaVer.ToString())
                    throw new OperationCanceledException("Manifest.xml is in an invalid format");

                if (manifest.GUID == null)
                    throw new OperationCanceledException("Manifest.xml is missing the GUID");

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
    }
}
