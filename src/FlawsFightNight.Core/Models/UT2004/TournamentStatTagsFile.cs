using FlawsFightNight.Core.Attributes;
using System.Collections.Generic;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class TournamentStatTagsFile
    {
        public List<TournamentStatTag> Tags { get; set; } = new();
    }
}