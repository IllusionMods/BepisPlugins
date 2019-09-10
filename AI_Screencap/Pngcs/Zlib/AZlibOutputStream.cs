using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pngcs.Zlib {

    public abstract class AZlibOutputStream : Stream {
        readonly protected Stream rawStream;
        readonly protected bool leaveOpen;
        protected int compressLevel;
        protected EDeflateCompressStrategy strategy;

        public AZlibOutputStream(Stream st, int compressLevel, EDeflateCompressStrategy strat, bool leaveOpen) {
            rawStream = st;
            this.leaveOpen = leaveOpen;
            this.strategy = strat;
            this.compressLevel = compressLevel;
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }


        public override bool CanSeek {
            get { return false; }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override long Length {
            get { throw new NotImplementedException(); }
        }


        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        public override bool CanRead {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override bool CanTimeout {
            get {
                return false;
            }
        }

        /// <summary>
        /// mainly for debugging
        /// </summary>
        /// <returns></returns>
        public abstract String getImplementationId();
    }
}
