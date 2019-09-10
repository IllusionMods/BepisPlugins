using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Zlib {
        
    public class CRC32 { // based on http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net

            private const UInt32 defaultPolynomial = 0xedb88320;
            private const UInt32 defaultSeed = 0xffffffff;
            private static UInt32[] defaultTable;

            private UInt32 hash;
            private UInt32 seed;
            private UInt32[] table;

            public CRC32()
                : this(defaultPolynomial, defaultSeed) {
            }

            public CRC32(UInt32 polynomial, UInt32 seed) {
                table = InitializeTable(polynomial);
                this.seed = seed;
                this.hash = seed;
            }

            public void Update(byte[] buffer) {
                Update(buffer, 0, buffer.Length);
            }

            public void Update(byte[] buffer, int start, int length) {
                for (int i = 0, j = start; i < length; i++, j++) {
                    unchecked {
                        hash = (hash >> 8) ^ table[buffer[j] ^ hash & 0xff];
                    }
                }
            }

            public UInt32 GetValue() {
                return ~hash;
            }

            public void Reset() {
                this.hash = seed;
            }
        
            private static UInt32[] InitializeTable(UInt32 polynomial) {
                if (polynomial == defaultPolynomial && defaultTable != null)
                    return defaultTable;
                UInt32[] createTable = new UInt32[256];
                for (int i = 0; i < 256; i++) {
                    UInt32 entry = (UInt32)i;
                    for (int j = 0; j < 8; j++)
                        if ((entry & 1) == 1)
                            entry = (entry >> 1) ^ polynomial;
                        else
                            entry = entry >> 1;
                    createTable[i] = entry;
                }
                if (polynomial == defaultPolynomial)
                    defaultTable = createTable;
                return createTable;
            }

        }
}
