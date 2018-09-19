using System.IO;
using System.Xml.Linq;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;

namespace Sideloader
{
    public class Manifest
    {
        protected readonly int SchemaVer = 1;

        protected XDocument manifestDocument;

        public string GUID => manifestDocument.Root?.Element("guid")?.Value;
        public string Name => manifestDocument.Root?.Element("name")?.Value;
        public string Version => manifestDocument.Root?.Element("version")?.Value;
        public string Author => manifestDocument.Root?.Element("author")?.Value;
        public string Website => manifestDocument.Root?.Element("website")?.Value;
        public string Description => manifestDocument.Root?.Element("description")?.Value;

        public Manifest(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
                manifestDocument = XDocument.Load(reader);
        }

        public static bool TryLoadFromZip(ZipFile zip, out Manifest manifest)
        {
            manifest = null;
            try
            {
                ZipEntry entry = zip.GetEntry("manifest.xml");

                if (entry == null)
                    return false;

                manifest = new Manifest(zip.GetInputStream(entry));

                if (manifest.manifestDocument?.Root?.Attribute("schema-ver")?.Value != manifest.SchemaVer.ToString())
                    return false;

                if (manifest.GUID == null)
                    return false;

                return true;
            }
            catch
            {
                // Badly formatted manifest or bad data
                return false;
            }
        }
    }
}
