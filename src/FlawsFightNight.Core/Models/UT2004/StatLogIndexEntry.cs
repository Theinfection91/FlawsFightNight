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
        public bool IsAdminIgnored { get; set; } = false;
        public ulong AdminDiscordID { get; set; }
        public string AdminName { get; set; }
        public DateTime IgnoredAt { get; set; }
        public string? TournamentId { get; set; }
        public string? MatchId { get; set; }
        public string? TournamentName { get; set; }

        // Helper property to quickly check if a log is already tagged to a match
        public bool IsTagged => !string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(TournamentId);

        public StatLogIndexEntry() { }
    }
}