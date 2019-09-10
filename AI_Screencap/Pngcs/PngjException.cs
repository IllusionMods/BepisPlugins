namespace Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Gral exception class for PNGCS library
    /// </summary>
    [Serializable]
    public class PngjException : Exception {
        private const long serialVersionUID = 1L;

        public PngjException(String message, Exception cause)
            : base(message, cause) {
        }

        public PngjException(String message)
            : base(message) {
        }

        public PngjException(Exception cause)
            : base(cause.Message, cause) {
        }
    }
}
