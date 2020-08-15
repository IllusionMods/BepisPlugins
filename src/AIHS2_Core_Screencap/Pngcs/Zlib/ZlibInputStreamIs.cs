using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

#if SHARPZIPLIB

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
// ONLY IF SHARPZIPLIB IS AVAILABLE

namespace Pngcs.Zlib {


    /// <summary>
    /// Zip input (inflater) based on ShaprZipLib
    /// </summary>
    class ZlibInputStreamIs : AZlibInputStream {

        private InflaterInputStream ist;

        public ZlibInputStreamIs(Stream st, bool leaveOpen)
            : base(st, leaveOpen) {
            ist = new InflaterInputStream(st);
            ist.IsStreamOwner = !leaveOpen;
        }

        public override int Read(byte[] array, int offset, int count) {
            return ist.Read(array, offset, count);
        }

        public override int ReadByte() {
            return ist.ReadByte();
        }

        public override void Close() {
            ist.Close();
        }


        public override void Flush() {
            ist.Flush();
        }

        public override String getImplementationId() {
            return "Zlib inflater: SharpZipLib";
        }
    }
}

#endif
