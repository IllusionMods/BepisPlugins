namespace Pngcs.Chunks
{
    /// <summary>
    /// A Chunk type that allows duplicate in an image
    /// </summary>
    internal abstract class PngChunkMultiple : PngChunk
    {
        internal PngChunkMultiple(string id, ImageInfo imgInfo) : base(id, imgInfo) { }

        public sealed override bool AllowsMultiple()
        {
            return true;
        }
    }
}
