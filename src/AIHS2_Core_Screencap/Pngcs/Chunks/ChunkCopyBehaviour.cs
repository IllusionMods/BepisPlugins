using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Chunks {
    /// <summary>
    /// Behaviours for chunks transfer when reading and writing.
    /// </summary>
    /// <remarks>
    /// They are bitmasks, can be OR-ed
    /// </remarks>
    public class ChunkCopyBehaviour {

        /// <summary>
        /// Don't copy any chunk
        /// </summary>
        public static readonly int COPY_NONE = 0;

        /// <summary>
        /// Copy the Palette, if present
        /// </summary>
        public static readonly int COPY_PALETTE = 1;

        /// <summary>
        /// Copy all SAFE chunks
        /// </summary>
        public static readonly int COPY_ALL_SAFE = 1 << 2;

        /// <summary>
        /// Copy all chunks (includes palette)
        /// </summary>
        public static readonly int COPY_ALL = 1 << 3;

        /// <summary>
        /// Copy Physical resolution (DPI)
        /// </summary>
        public static readonly int COPY_PHYS = 1 << 4;


        /// <summary>
        /// Copy all textual chunks (not safe)
        /// </summary>
        public static readonly int COPY_TEXTUAL = 1 << 5;

        /// <summary>
        /// Copy transparency (not safe)
        /// </summary>
        public static readonly int COPY_TRANSPARENCY = 1 << 6; //


        /// <summary>
        /// Copy chunks unknown by our factory
        /// </summary>
        public static readonly int COPY_UNKNOWN = 1 << 7;

        /// <summary>
        /// Copy all known, except HIST, TIME and textual
        /// </summary>
        public static readonly int COPY_ALMOSTALL = 1 << 8;
    }
}
