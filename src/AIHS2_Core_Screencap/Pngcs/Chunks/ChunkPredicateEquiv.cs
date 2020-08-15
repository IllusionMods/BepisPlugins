using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Chunks {
    /// <summary>
    /// An ad-hoc criterion, perhaps useful, for equivalence.
    /// <see cref="ChunkHelper.Equivalent(PngChunk,PngChunk)"/> 
    /// </summary>
    internal class ChunkPredicateEquiv : ChunkPredicate {

        private readonly PngChunk chunk;
        /// <summary>
        /// Creates predicate based of reference chunk
        /// </summary>
        /// <param name="chunk"></param>
        public ChunkPredicateEquiv(PngChunk chunk) {
            this.chunk = chunk;
        }
        /// <summary>
        /// Check for match
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Matches(PngChunk c) {
            return ChunkHelper.Equivalent(c, chunk);
        }
    }

}
