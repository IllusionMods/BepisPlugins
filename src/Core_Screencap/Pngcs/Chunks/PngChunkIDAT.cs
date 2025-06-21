namespace Pngcs.Chunks
{
    /// <summary>
    /// IDAT chunk http://www.w3.org/TR/PNG/#11IDAT
    /// 
    /// This object is dummy placeholder - We treat this chunk in a very different way than ancillary chnks
    /// </summary>
    internal class PngChunkIDAT : PngChunkMultiple
    {
        public const string ID = ChunkHelper.IDAT;

        public PngChunkIDAT(ImageInfo i, int len, long offset) : base(ID, i)
        {
            Length = len;
            Offset = offset;
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
        {
            return ChunkOrderingConstraint.NA;
        }

        public override ChunkRaw CreateRawChunk()
        {// does nothing
            return null;
        }

        public override void ParseFromRaw(ChunkRaw c)
        { // does nothing
        }

        public override void CloneDataFromRead(PngChunk other)
        {
        }
    }
}
