using System;

namespace Pngcs
{
    /// <summary>
    /// Gral exception class for PNGCS library
    /// </summary>
    [Serializable]
    internal class PngjException : Exception
    {
        public PngjException(string message, Exception cause) : base(message, cause) { }

        public PngjException(string message) : base(message) { }

        public PngjException(Exception cause) : base(cause.Message, cause) { }
    }
}
