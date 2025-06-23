using System;

namespace Pngcs
{
    /// <summary>
    /// Exception associated with input (reading) operations
    /// </summary>
    [Serializable]
    internal class PngjOutputException : PngjException
    {
        public PngjOutputException(string message, Exception cause) : base(message, cause) { }

        public PngjOutputException(string message) : base(message) { }

        public PngjOutputException(Exception cause) : base(cause) { }
    }
}
