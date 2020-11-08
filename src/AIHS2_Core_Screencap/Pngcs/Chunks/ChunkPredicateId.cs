namespace Pngcs.Chunks
{
    /// <summary>
    /// Match if have same Chunk Id
    /// </summary>
    internal class ChunkPredicateId : IChunkPredicate
    {
        private readonly string id;
        public ChunkPredicateId(string id)
        {
            this.id = id;
        }
        public bool Matches(PngChunk c)
        {
            return c.Id.Equals(id);
        }
    }
}
