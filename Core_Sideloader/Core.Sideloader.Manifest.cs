using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

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

        internal Manifest(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
                manifestDocument = XDocument.Load(reader);
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
