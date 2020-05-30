namespace Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Exception for unsupported operation or feature
    /// </summary>
    [Serializable]
    public class PngjUnsupportedException : Exception {
        private const long serialVersionUID = 1L;

        public PngjUnsupportedException()
            : base() {
        }

        public PngjUnsupportedException(String message, Exception cause)
            : base(message, cause) {
        }

        public PngjUnsupportedException(String message)
            : base(message) {
        }

        public PngjUnsupportedException(Exception cause)
            : base(cause.Message, cause) {
        }
    }
}
