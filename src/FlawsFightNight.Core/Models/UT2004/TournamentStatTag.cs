using FlawsFightNight.Core.Attributes;
using System;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class TournamentStatTag
    {
        /// <summary>
        /// The original stat log file name (e.g. "iCTF000042.json") — stable across re-processing.
        /// After a re-process the ID may change, but the FileName on the UT2004StatLog is set from the
        /// original FTP file name and remains constant.
        /// </summary>
        public string StatLogFileName { get; set; } = string.Empty;

        /// <summary>
        /// Snapshot of the stat log ID at the time of tagging. Used for quick lookups when the index
        /// is already loaded; falls back to FileName matching after a restore + re-process.
        /// </summary>
        public string StatLogId { get; set; } = string.Empty;

        public DateTime MatchDate { get; set; }
        public string? ServerName { get; set; }

        // Tournament link
        public string TournamentId { get; set; } = string.Empty;
        public string MatchId { get; set; } = string.Empty;
        public string? TournamentName { get; set; }
        public DateTime TaggedAt { get; set; }

        // Admin-ignore state (null/default when not ignored)
        public bool IsAdminIgnored { get; set; }
        public ulong AdminDiscordID { get; set; }
        public string? AdminName { get; set; }
        public DateTime? IgnoredAt { get; set; }
    }
}