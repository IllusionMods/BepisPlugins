using Pngcs.Chunks;
using System.IO;

namespace Pngcs
{
    /// <summary>
    /// outputs the stream for IDAT chunk , fragmented at fixed size (32k default).
    /// </summary>
    ///
    internal class PngIDatChunkOutputStream : ProgressiveOutputStream
    {
        private const int SIZE_DEFAULT = 32768;// 32k
        private readonly Stream outputStream;

        public PngIDatChunkOutputStream(Stream outputStream_0) : this(outputStream_0, SIZE_DEFAULT) { }

        public PngIDatChunkOutputStream(Stream outputStream_0, int size) : base(size > 8 ? size : SIZE_DEFAULT)
        {
            outputStream = outputStream_0;
        }

        protected override void FlushBuffer(byte[] b, int len)
        {
            ChunkRaw c = new ChunkRaw(len, ChunkHelper.b_IDAT, false) { Data = b };
            c.WriteChunk(outputStream);
        }

        public override void Close()
        {
            // closing the IDAT stream only flushes it, it does not close the underlying stream
            Flush();
        }
    }
}
