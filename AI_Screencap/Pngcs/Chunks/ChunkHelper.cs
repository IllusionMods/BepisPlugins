namespace Pngcs.Chunks {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Pngcs.Zlib;
    
    /// <summary>
    /// Static utility methods for CHunks
    /// </summary>
    /// <remarks>
    /// Client code should rarely need this, see PngMetada and ChunksList
    /// </remarks>
    public class ChunkHelper {
        internal const String IHDR = "IHDR";
        internal const String PLTE = "PLTE";
        internal const String IDAT = "IDAT";
        internal const String IEND = "IEND";
        internal const String cHRM = "cHRM";// No Before PLTE and IDAT
        internal const String gAMA = "gAMA";// No Before PLTE and IDAT
        internal const String iCCP = "iCCP";// No Before PLTE and IDAT
        internal const String sBIT = "sBIT";// No Before PLTE and IDAT
        internal const String sRGB = "sRGB";// No Before PLTE and IDAT
        internal const String bKGD = "bKGD";// No After PLTE; before IDAT
        internal const String hIST = "hIST";// No After PLTE; before IDAT
        internal const String tRNS = "tRNS";// No After PLTE; before IDAT
        internal const String pHYs = "pHYs";// No Before IDAT
        internal const String sPLT = "sPLT";// Yes Before IDAT
        internal const String tIME = "tIME";// No None
        internal const String iTXt = "iTXt";// Yes None
        internal const String tEXt = "tEXt";// Yes None
        internal const String zTXt = "zTXt";// Yes None
        internal static readonly byte[] b_IHDR = ToBytes(IHDR);
        internal static readonly byte[] b_PLTE = ToBytes(PLTE);
        internal static readonly byte[] b_IDAT = ToBytes(IDAT);
        internal static readonly byte[] b_IEND = ToBytes(IEND);



        /// <summary>
        /// Converts to bytes using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static byte[] ToBytes(String x) {
            return Pngcs.PngHelperInternal.charsetLatin1.GetBytes(x);
        }

        /// <summary>
        /// Converts to String using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static String ToString(byte[] x) {
            return Pngcs.PngHelperInternal.charsetLatin1.GetString(x);
        }
        /// <summary>
        ///  Converts to String using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static String ToString(byte[] x, int offset, int len) {
            return Pngcs.PngHelperInternal.charsetLatin1.GetString(x, offset, len);
        }

        /// <summary>
        /// Converts to bytes using UTF-8
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static byte[] ToBytesUTF8(String x) {
            return Pngcs.PngHelperInternal.charsetUtf8.GetBytes(x);
        }

        /// <summary>
        /// Converts to string using UTF-8
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static String ToStringUTF8(byte[] x) {
            return Pngcs.PngHelperInternal.charsetUtf8.GetString(x);
        }
        /// <summary>
        /// Converts to string using UTF-8
        /// </summary>
        /// <param name="x"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static String ToStringUTF8(byte[] x, int offset, int len) {
            return Pngcs.PngHelperInternal.charsetUtf8.GetString(x, offset, len);
        }
        /// <summary>
        /// Writes full array of bytes to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        public static void WriteBytesToStream(Stream stream, byte[] bytes) {
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Critical chunks: first letter is uppercase
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsCritical(String id) {
            // first letter is uppercase
            return (Char.IsUpper(id[0]));
        }

        /// <summary>
        /// Public chunks: second letter is uppercase
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsPublic(String id) { // public chunk?
            return (Char.IsUpper(id[1]));
        }
        /// <summary>
        /// Safe to copy chunk: fourth letter is lower case
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsSafeToCopy(String id) { // safe to copy?
            // fourth letter is lower case
            return (!Char.IsUpper(id[3]));
        }

        /// <summary>
        /// Finds position of null byte in array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>-1 if not found</returns>
        public static int PosNullByte(byte[] bytes) {
            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] == 0)
                    return i;
            return -1;
        }

        /// <summary>
        /// Decides if a chunk should be loaded, according to a ChunkLoadBehaviour
        /// </summary>
        /// <param name="id"></param>
        /// <param name="behav"></param>
        /// <returns></returns>
        public static bool ShouldLoad(String id, ChunkLoadBehaviour behav) {
            if (IsCritical(id))
                return true;
            bool kwown = PngChunk.isKnown(id);
            switch (behav) {
                case ChunkLoadBehaviour.LOAD_CHUNK_ALWAYS:
                    return true;
                case ChunkLoadBehaviour.LOAD_CHUNK_IF_SAFE:
                    return kwown || IsSafeToCopy(id);
                case ChunkLoadBehaviour.LOAD_CHUNK_KNOWN:
                    return kwown;
                case ChunkLoadBehaviour.LOAD_CHUNK_NEVER:
                    return false;
            }
            return false; // should not reach here
        }

        internal static byte[] compressBytes(byte[] ori, bool compress) {
            return compressBytes(ori, 0, ori.Length, compress);
        }

        internal static byte[] compressBytes(byte[] ori, int offset, int len, bool compress) {
            try {
                MemoryStream inb = new MemoryStream(ori, offset, len);
                Stream inx = inb;
                if (!compress) inx = ZlibStreamFactory.createZlibInputStream(inb);
                MemoryStream outb = new MemoryStream();
                Stream outx = outb;
                if (compress) outx = ZlibStreamFactory.createZlibOutputStream(outb);
                shovelInToOut(inx, outx);
                inx.Close();
                outx.Close();
                byte[] res = outb.ToArray();
                return res;
            } catch (Exception e) {
                throw new PngjException(e);
            }
        }

        private static void shovelInToOut(Stream inx, Stream outx) {
            byte[] buffer = new byte[1024];
            int len;
            while ((len = inx.Read(buffer, 0, 1024)) > 0) {
                outx.Write(buffer, 0, len);
            }
        }

        internal static bool maskMatch(int v, int mask) {
            return (v & mask) != 0;
        }

        /// <summary>
        /// Filters a list of Chunks, keeping those which match the predicate
        /// </summary>
        /// <remarks>The original list is not altered</remarks>
        /// <param name="list"></param>
        /// <param name="predicateKeep"></param>
        /// <returns></returns>
        public static List<PngChunk> FilterList(List<PngChunk> list, ChunkPredicate predicateKeep) {
            List<PngChunk> result = new List<PngChunk>();
            foreach (PngChunk element in list) {
                if (predicateKeep.Matches(element)) {
                    result.Add(element);
                }
            }
            return result;
        }
        /// <summary>
        /// Filters a list of Chunks, removing those which match the predicate
        /// </summary>
        /// <remarks>The original list is not altered</remarks>
        /// <param name="list"></param>
        /// <param name="predicateRemove"></param>
        /// <returns></returns>
        public static int TrimList(List<PngChunk> list, ChunkPredicate predicateRemove) {
            int cont = 0;
            for (int i = list.Count - 1; i >= 0; i--) {
                if (predicateRemove.Matches(list[i])) {
                    list.RemoveAt(i);
                    cont++;
                }
            }
            return cont;
        }


        /// <summary>
        /// Ad-hoc criteria for 'equivalent' chunks.
        /// </summary>
        ///  <remarks>
        /// Two chunks are equivalent if they have the same Id AND either:
        /// 1. they are Single
        /// 2. both are textual and have the same key
        /// 3. both are SPLT and have the same palette name
        /// Bear in mind that this is an ad-hoc, non-standard, nor required (nor wrong)
        /// criterion. Use it only if you find it useful. Notice that PNG allows to have
        /// repeated textual keys with same keys.
        /// </remarks>        
        /// <param name="c1">Chunk1</param>
        /// <param name="c2">Chunk1</param>
        /// <returns>true if equivalent</returns>
        public static bool Equivalent(PngChunk c1, PngChunk c2) {
            if (c1 == c2)
                return true;
            if (c1 == null || c2 == null || !c1.Id.Equals(c2.Id))
                return false;
            // same id
            if (c1.GetType() != c2.GetType())
                return false; // should not happen
            if (!c2.AllowsMultiple())
                return true;
            // unknown chunks that allow multiple? consider they don't match
            return false;
        }

    }
}
