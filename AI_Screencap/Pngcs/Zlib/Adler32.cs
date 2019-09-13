using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Zlib {

    public class Adler32 {
        private UInt32 a = 1;
        private UInt32 b = 0;
        private const int _base = 65521; /* largest prime smaller than 65536 */
        private const int _nmax = 5550;
        private int pend = 0; // how many bytes have I read witouth computing modulus


        public void Update(byte data) {
            if (pend >= _nmax) updateModulus();
            a += data;
            b += a;
            pend++;
        }

        public void Update(byte[] data) {
            Update(data, 0, data.Length);
        }

        public void Update(byte[] data, int offset, int length) {
            int nextJToComputeModulus = _nmax - pend;
            for (int j = 0; j < length; j++) {
                if (j == nextJToComputeModulus) {
                    updateModulus();
                    nextJToComputeModulus = j + _nmax;
                }
                unchecked {
                    a += data[j + offset];
                }
                b += a;
                pend++;
            }
        }

        public void Reset() {
            a = 1;
            b = 0;
            pend = 0;
        }

        private void updateModulus() {
            a %= _base;
            b %= _base;
            pend = 0;
        }

        public UInt32 GetValue() {
            if (pend > 0) updateModulus();
            return (b << 16) | a;
        }
    }
}
