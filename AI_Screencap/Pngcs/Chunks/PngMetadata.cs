namespace Pngcs.Chunks
{

    /// <summary>Image Metadata, wrapper over a ChunksList</summary>
    /// <remarks>
    /// Additional image info, apart from the ImageInfo and the pixels themselves. 
    /// Includes Palette and ancillary chunks.
    /// This class provides a wrapper over the collection of chunks of a image (read or to write) and provides some high
    /// level methods to access them
    /// </remarks>
    public class PngMetadata
    {
        readonly ChunksList chunkList;
        readonly bool ReadOnly;// readonly

        internal PngMetadata ( ChunksList chunks )
        {
            this.chunkList = chunks;
            if( chunks is ChunksListForWrite )
            {
                this.ReadOnly = false;
            }
            else
            {
                this.ReadOnly = true;
            }
        }

        /// <summary>Queues the chunk at the writer</summary>
        /// <param name="chunk">Chunk, ready for write</param>
        /// <param name="lazyOverwrite">Ovewrite lazily equivalent chunks</param>
        /// <remarks>Warning: the overwriting applies to equivalent chunks, see <c>ChunkPredicateEquiv</c>
        /// and will only make sense for queued (not yet writen) chunks
        /// </remarks>
        public void QueueChunk ( PngChunk chunk , bool lazyOverwrite )
        {
            ChunksListForWrite cl = getChunkListW();
            if( ReadOnly ) { throw new PngjException( "cannot set chunk : readonly metadata" ); }
            if( lazyOverwrite )
            {
                ChunkHelper.TrimList( cl.GetQueuedChunks() , new ChunkPredicateEquiv( chunk ) );
            }
            cl.Queue( chunk );
        }

        /// <summary>Queues the chunk at the writer</summary>
        /// <param name="chunk">Chunk, ready for write</param>
        public void QueueChunk ( PngChunk chunk ) => QueueChunk( chunk , true );

        ChunksListForWrite getChunkListW () => (ChunksListForWrite)chunkList;


    }
}
