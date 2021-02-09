namespace Pngcs.Chunks
{
    /// <summary>
    /// A Chunk type that does not allow duplicate in an image
    /// </summary>
    internal abstract class PngChunkSingle : PngChunk
    {
        public PngChunkSingle(string id, ImageInfo imgInfo) : base(id, imgInfo) { }

        public sealed override bool AllowsMultiple()
        {
            return false;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((Id == null) ? 0 : Id.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            return obj is PngChunkSingle single && Id != null && Id.Equals(single.Id);
        }
    }
}
