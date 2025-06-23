using Pngcs.Zlib;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary> Wraps the raw chunk data </summary>
    /// <remarks>
    /// Short lived object, to be created while serialing/deserializing 
    /// 
    /// Do not reuse it for different chunks
    /// 
    /// See http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
    ///</remarks>
    internal class ChunkRaw
    {
        /// <summary>
        /// The length counts only the data field, not itself, the chunk type code, or the CRC. Zero is a valid length.
        /// Although encoders and decoders should treat the length as unsigned, its value must not exceed 2^31-1 bytes.
        /// </summary>
        public readonly int Len;
        /// <summary>
        /// Chunk Id, as array of 4 bytes
        /// </summary>
        public readonly byte[] IdBytes;
        public readonly string Id;
        /// <summary>
        /// Raw data, crc not included
        /// </summary>
        public byte[] Data;
        private int crcval;
        internal long offset = 0;

        //dodane przeze mnie:
        public void SetOffset(long value)
        {
            offset = value;
        }

        /// <summary>
        /// Creates an empty raw chunk
        /// </summary>
        internal ChunkRaw(int length, string idb, bool alloc)
        {
            Id = idb;
            IdBytes = ChunkHelper.ToBytes(Id);
            Data = null;
            crcval = 0;
            Len = length;
            if (alloc)
                AllocData();
        }

        internal ChunkRaw(int length, byte[] idbytes, bool alloc) : this(length, ChunkHelper.ToString(idbytes), alloc) { }

        /// <summary>
        /// Called after setting data, before writing to os
        /// </summary>
        private int ComputeCrc()
        {
            CRC32 crcengine = PngHelperInternal.GetCRC();
            crcengine.Reset();
            crcengine.Update(IdBytes, 0, 4);
            if (Len > 0)
                crcengine.Update(Data, 0, Len); //
            return (int)crcengine.GetValue();
        }

        internal void WriteChunk(Stream os)
        {
            if (IdBytes.Length != 4)
                throw new PngjOutputException("bad chunkid [" + ChunkHelper.ToString(IdBytes) + "]");
            crcval = ComputeCrc();
            PngHelperInternal.WriteInt4(os, Len);
            PngHelperInternal.WriteBytes(os, IdBytes);
            if (Len > 0)
                PngHelperInternal.WriteBytes(os, Data, 0, Len);
            //Console.WriteLine("writing chunk " + this.ToString() + "crc=" + crcval);
            PngHelperInternal.WriteInt4(os, crcval);
        }

        internal MemoryStream GetAsByteStream()
        { // only the data
            return new MemoryStream(Data);
        }

        private void AllocData()
        {
            if (Data == null || Data.Length < Len)
                Data = new byte[Len];
        }
        /// <summary>
        /// Just id and length
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "chunkid=" + ChunkHelper.ToString(IdBytes) + " len=" + Len;
        }
    }
}
