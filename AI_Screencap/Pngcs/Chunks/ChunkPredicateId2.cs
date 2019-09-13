using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs.Chunks
{
    /// <summary>
    /// match if have same id and, if Text (or SPLT) if have the asame key
    /// </summary>
    /// <remarks>
    /// This is the same as ChunkPredicateEquivalent, the only difference is that does not requires
    /// a chunk at construction time
    /// </remarks>
    internal class ChunkPredicateId2 : ChunkPredicate
    {

        private readonly string id;
        private readonly string innerid;
        public ChunkPredicateId2(string id, string inner)
        {
            this.id = id;
            this.innerid = inner;
        }
        public bool Matches(PngChunk c)
        {
            if (!c.Id.Equals(id))
                return false;

            return true;
        }

    }
}
