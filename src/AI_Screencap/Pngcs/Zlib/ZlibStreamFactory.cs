using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pngcs.Zlib {


    public class ZlibStreamFactory {        
        public static AZlibInputStream createZlibInputStream(Stream st, bool leaveOpen) {
//#if NET45
                return new ZlibInputStreamMs(st,leaveOpen);
//#endif
//#if SHARPZIPLIB
//            return new ZlibInputStreamIs(st, leaveOpen);
//#endif
        }

        public static AZlibInputStream createZlibInputStream(Stream st) {
            return createZlibInputStream(st, false);
        }

        public static AZlibOutputStream createZlibOutputStream(Stream st, int compressLevel, EDeflateCompressStrategy strat, bool leaveOpen) {
//#if NET45
                return new ZlibOutputStreamMs( st, compressLevel,strat, leaveOpen);
//#endif
//#if SHARPZIPLIB
//            return new ZlibOutputStreamIs(st, compressLevel, strat, leaveOpen);
//#endif
        }

        public static AZlibOutputStream createZlibOutputStream(Stream st) {
            return createZlibOutputStream(st, false);
        }

        public static AZlibOutputStream createZlibOutputStream(Stream st, bool leaveOpen) {
            return createZlibOutputStream(st, DeflateCompressLevel.DEFAULT, EDeflateCompressStrategy.Default, leaveOpen);
        }
    }
}
