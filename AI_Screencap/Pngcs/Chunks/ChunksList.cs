namespace Pngcs.Chunks {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// All chunks that form an image, read or to be written
    /// 
    /// http://www.w3.org/TR/PNG/#table53
    /// </summary>
    ///
    public class ChunksList {
        internal const int CHUNK_GROUP_0_IDHR = 0; // required - single
        internal const int CHUNK_GROUP_1_AFTERIDHR = 1; // optional - multiple
        internal const int CHUNK_GROUP_2_PLTE = 2; // optional - single
        internal const int CHUNK_GROUP_3_AFTERPLTE = 3; // optional - multple
        internal const int CHUNK_GROUP_4_IDAT = 4; // required (single pseudo chunk)
        internal const int CHUNK_GROUP_5_AFTERIDAT = 5; // optional - multple
        internal const int CHUNK_GROUP_6_END = 6; // only 1 chunk - requried

        /// <summary>
        ///  Includes all chunks, but IDAT is a single pseudo chunk without data
        /// </summary>
        protected List<PngChunk> chunks;

        internal readonly ImageInfo imageInfo; // only required for writing

        internal ChunksList(ImageInfo imfinfo) {
            this.chunks = new List<PngChunk>();
            this.imageInfo = imfinfo;
        }

        /// <summary>
        /// Keys of processed (read or writen) chunks
        /// </summary>
        /// <returns>key:chunk id, val: number of occurrences</returns>
        public Dictionary<String,int> GetChunksKeys() {
            Dictionary<String, int> ck = new Dictionary<String, int>();
            foreach (PngChunk c in chunks) {
                ck[c.Id] =  ck.ContainsKey(c.Id) ? ck[c.Id] + 1 : 1;
            }
            return ck;
        }

     
        /// <summary>
        /// Returns a copy of the chunk list (but the chunks are not copied) 
        /// </summary>
        /// <remarks>This should not be used for general metadata handling
        /// </remarks>
        /// <returns></returns>
        public List<PngChunk> GetChunks() {
            return new List<PngChunk>(chunks);
        }

        internal static List<PngChunk> GetXById(List<PngChunk> list, String id, String innerid) {
            if (innerid == null)
                return ChunkHelper.FilterList(list, new ChunkPredicateId(id));
            else
                return ChunkHelper.FilterList(list, new ChunkPredicateId2(id, innerid));
        }

        /// <summary>
        /// Adds chunk in next position. This is used only by the pngReader
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="chunkGroup"></param>
        public void AppendReadChunk(PngChunk chunk, int chunkGroup) {
            chunk.ChunkGroup = chunkGroup;
            chunks.Add(chunk);
        }
        /// <summary>
        /// All chunks with this ID
        /// </summary>
        /// <remarks>The GetBy... methods never include queued chunks</remarks>
        /// <param name="id"></param>
        /// <returns>List, empty if none</returns>
        public List<PngChunk> GetById(String id) {
            return GetById(id, null);
        }

        /// <summary>
        /// Same as ID, but we an additional discriminator for textual keys
        /// </summary>
        /// <remarks>If innerid!=null and the chunk is PngChunkTextVar or PngChunkSPLT, it's filtered by that id</remarks>
        /// <param name="id"></param>
        /// <param name="innerid">Only used for text and SPLT chunks</param>
        /// <returns>List, empty if none</returns>
        public List<PngChunk> GetById(String id, String innerid) {
            return GetXById(chunks, id, innerid);
        }
        /// <summary>
        /// Returns only one chunk 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>First chunk found, null if not found</returns>
        public PngChunk GetById1(String id) {
            return GetById1(id, false);
        }


        /// <summary>
        /// Returns only one chunk
        /// </summary>
        /// <param name="id"></param>
        /// <param name="failIfMultiple">true, and more than one found: exception</param>
        /// <returns>null if not found</returns>
        public PngChunk GetById1(String id, bool failIfMultiple) {
            return GetById1(id, null, failIfMultiple);
        }

        /// <summary>
        /// Sames as <c>GetById1(String id, bool failIfMultiple)</c> but allows an additional innerid
        /// </summary>
        /// <param name="id"></param>
        /// <param name="innerid"></param>
        /// <param name="failIfMultiple">true, and more than one found: exception</param>
        /// <returns>null if not found</returns>
        public PngChunk GetById1(String id, String innerid, bool failIfMultiple) {
            List<PngChunk> list = GetById(id, innerid);
            if (list.Count == 0)
                return null;
            if (list.Count > 1 && (failIfMultiple || !list[0].AllowsMultiple()))
                throw new PngjException("unexpected multiple chunks id=" + id);
            return list[list.Count - 1];
        }
            
        /// <summary>
        /// Finds all chunks "equivalent" to this one
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns>Empty if nothing found</returns>
        public List<PngChunk> GetEquivalent(PngChunk chunk) {
            return ChunkHelper.FilterList(chunks, new ChunkPredicateEquiv(chunk));
        }

        /// <summary>
        /// Only the amount of chunks
        /// </summary>
        /// <returns></returns>
        public override String ToString() {
            return "ChunkList: read: " + chunks.Count;
        }
        /// <summary>
        /// Detailed information, for debugging
        /// </summary>
        /// <returns></returns>
        public String ToStringFull() {
            StringBuilder sb = new StringBuilder(ToString());
            sb.Append("\n Read:\n");
            foreach (PngChunk chunk in chunks) {
                sb.Append(chunk).Append(" G=" + chunk.ChunkGroup + "\n");
            }
            return sb.ToString();
        }

    }
}
