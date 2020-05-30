using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Chunks {
    /// <summary>
    /// A Chunk type that allows duplicate in an image
    /// </summary>
    public abstract class PngChunkMultiple : PngChunk {
        internal PngChunkMultiple(String id, ImageInfo imgInfo)
            : base(id, imgInfo) {

        }

        public sealed override bool AllowsMultiple() {
            return true;
        }

    }
}
