using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Pngcs.Zlib {

    public abstract class AZlibInputStream : Stream {
        readonly protected Stream rawStream;
        readonly protected bool leaveOpen;

        public AZlibInputStream(Stream st, bool leaveOpen) {
            rawStream = st;
            this.leaveOpen = leaveOpen;
        }

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanWrite {
            get { return false; }
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


        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
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

