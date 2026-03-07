using FlawsFightNight.Core.Attributes;
using System;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class StatLogIndexEntry
    {
        public string Id { get; set; }
        public DateTime MatchDate { get; set; }
        public string? ServerName { get; set; }

        public StatLogIndexEntry() { }
    }
}