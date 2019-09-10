using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Zlib {
    // DEFLATE compression levels 0-9
    public class DeflateCompressLevel {
        public const int NO_COMPRESSION = 0;
        public const int FASTEST = 3;
        public const int DEFAULT = 6;
        public const int OPTIMAL = 9;
    }
}

