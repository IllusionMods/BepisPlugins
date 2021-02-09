namespace Pngcs
{
    /// <summary> Exception for internal problems </summary>
    [System.Serializable]
    internal class PngjExceptionInternal : System.Exception
    {
        public PngjExceptionInternal() : base() { }

        public PngjExceptionInternal(string message, System.Exception cause) : base(message, cause) { }

        public PngjExceptionInternal(string message) : base(message) { }

        public PngjExceptionInternal(System.Exception cause) : base(cause.Message, cause) { }
    }
}
