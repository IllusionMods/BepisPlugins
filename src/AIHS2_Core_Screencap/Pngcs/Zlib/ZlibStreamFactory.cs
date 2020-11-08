using System.IO;

namespace Pngcs.Zlib
{
    internal class ZlibStreamFactory
    {
        public static AZlibInputStream CreateZlibInputStream(Stream st, bool leaveOpen)
        {
            //#if NET45
            return new ZlibInputStreamMs(st, leaveOpen);
            //#endif
            //#if SHARPZIPLIB
            //            return new ZlibInputStreamIs(st, leaveOpen);
            //#endif
        }

        public static AZlibInputStream CreateZlibInputStream(Stream st)
        {
            return CreateZlibInputStream(st, false);
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st, int compressLevel, EDeflateCompressStrategy strat, bool leaveOpen)
        {
            //#if NET45
            return new ZlibOutputStreamMs(st, compressLevel, strat, leaveOpen);
            //#endif
            //#if SHARPZIPLIB
            //            return new ZlibOutputStreamIs(st, compressLevel, strat, leaveOpen);
            //#endif
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st)
        {
            return CreateZlibOutputStream(st, false);
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st, bool leaveOpen)
        {
            return CreateZlibOutputStream(st, DeflateCompressLevel.DEFAULT, EDeflateCompressStrategy.Default, leaveOpen);
        }
    }
}
