namespace Pngcs.Chunks {

    using Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    /// <summary>
    /// IDAT chunk http://www.w3.org/TR/PNG/#11IDAT
    /// 
    /// This object is dummy placeholder - We treat this chunk in a very different way than ancillary chnks
    /// </summary>
    public class PngChunkIDAT : PngChunkMultiple {
        public const String ID = ChunkHelper.IDAT;

        public PngChunkIDAT(ImageInfo i,int len, long offset)
            : base(ID, i) {
            this.Length = len;
            this.Offset = offset;
        }

        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.NA;
        }

        public override ChunkRaw CreateRawChunk() {// does nothing
            return null;
        }

        public override void ParseFromRaw(ChunkRaw c) { // does nothing
        }

        public override void CloneDataFromRead(PngChunk other) {
        }
    }
}
