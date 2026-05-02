using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces.UT2004
{
    public interface IMatchSummarizer
    {
        /// <summary>
        /// Produce a human-readable summary for a completed match.
        /// - match: parsed UT2004StatLog
        /// - profiles: current profile map (guid -> profile)
        /// - eloChanges: optional per-guid rating delta applied for this match
        /// Returns a plain-text summary suitable for logging, notifications, or further processing.
        /// </summary>
        string Summarize(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles, Dictionary<string, double>? eloChanges = null);
    }
}
