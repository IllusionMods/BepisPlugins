using System.Collections.Generic;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static readonly HashSet<string> StudioListResolveBlacklist = new HashSet<string>()
        {
            "itemcategory",
            "animecategory",
            "voicecategory",
            "itemgroup",
            "animegroup",
            "voicegroup",
            "bone",
            "mapcategory"
        };
    }
}
