namespace Pngcs
{
    using System.IO;

    /// <summary>
    /// A few utility static methods to read and write files
    /// </summary>
    internal class FileHelper
    {
        public static Stream OpenFileForWriting(string file, bool allowOverwrite)
        {
            if (File.Exists(file) && !allowOverwrite)
                throw new PngjOutputException($"File already exists ({ file }) and overwrite=false");
            Stream osx = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
            return osx;
        }

        /// <summary>
        /// Given a filename and a ImageInfo, produces a PngWriter object, ready for writing.</summary>
        /// <param name="fileName">Path of file</param>
        /// <param name="imgInfo">ImageInfo object</param>
        /// <param name="allowOverwrite">Flag: if false and file exists, a PngjOutputException is thrown</param>
        /// <returns>A PngWriter object, ready for writing</returns>
        public static PngWriter CreatePngWriter(string fileName, ImageInfo imgInfo, bool allowOverwrite)
        {
            return new PngWriter(OpenFileForWriting(fileName, allowOverwrite), imgInfo, fileName);
        }
    }
}
