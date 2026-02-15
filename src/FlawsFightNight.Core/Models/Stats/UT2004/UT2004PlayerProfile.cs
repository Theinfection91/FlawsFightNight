using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    /// <summary>
    /// Represents a player's persistent profile across all UT2004 matches.
    /// Stored in database and updated after each match.
    /// </summary>
    public class UT2004PlayerProfile
    {
        // Identity
        public string Guid { get; set; } = string.Empty;
        public string CurrentName { get; set; } = string.Empty;
        public List<string> PreviousNames { get; set; } = new List<string>();
        
        // Match History
        public int TotalMatches { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public DateTime LastPlayed { get; set; }
        public DateTime FirstSeen { get; set; }

        // Skill Rating
        public double Mu { get; set; } = 25.0;           // Skill estimate
        public double Sigma { get; set; } = 25.0 / 3.0;  // Uncertainty
        
        /// <summary>
        /// Conservative skill rating for display (Mu - 3*Sigma)
        /// </summary>
        public double Rating => Mu - (3 * Sigma);

        // Cumulative Career Stats
        public int TotalKills { get; set; } = 0;
        public int TotalDeaths { get; set; } = 0;
        public int TotalSuicides { get; set; } = 0;
        public int TotalHeadshots { get; set; } = 0;
        public int TotalFlagCaptures { get; set; } = 0;
        public int TotalFlagReturns { get; set; } = 0;
        public int TotalScore { get; set; } = 0;

        // Career Bests (for achievements/leaderboards)
        public int BestKillStreak { get; set; } = 0;
        public int BestMultiKill { get; set; } = 0;
        public int MostKillsInMatch { get; set; } = 0;
        public int MostFlagCapsInMatch { get; set; } = 0;

        // Calculated Properties
        public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches : 0;
        public double KDRatio => TotalDeaths > 0 ? (double)TotalKills / TotalDeaths : TotalKills;

        public UT2004PlayerProfile() { }

        public UT2004PlayerProfile(string guid)
        {
            Guid = guid;
            FirstSeen = DateTime.UtcNow;
            LastPlayed = DateTime.UtcNow;
        }

        /// <summary>
        /// Update cumulative stats after a match
        /// </summary>
        public void UpdateStatsFromMatch(UTPlayerMatchStats matchStats)
        {
            TotalMatches++;
            if (matchStats.IsWinner) Wins++;
            else Losses++;

            TotalKills += matchStats.Kills;
            TotalDeaths += matchStats.Deaths;
            TotalSuicides += matchStats.Suicides;
            TotalHeadshots += matchStats.Headshots;
            TotalFlagCaptures += matchStats.FlagCaptures;
            TotalFlagReturns += matchStats.FlagReturns;
            TotalScore += matchStats.Score;

            // Update career bests
            BestKillStreak = Math.Max(BestKillStreak, matchStats.BestKillStreak);
            BestMultiKill = Math.Max(BestMultiKill, matchStats.BestMultiKill);
            MostKillsInMatch = Math.Max(MostKillsInMatch, matchStats.Kills);
            MostFlagCapsInMatch = Math.Max(MostFlagCapsInMatch, matchStats.FlagCaptures);

            // Update name if changed
            if (!CurrentName.Equals(matchStats.LastKnownName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(CurrentName) && !PreviousNames.Contains(CurrentName))
                {
                    PreviousNames.Add(CurrentName);
                }
                CurrentName = matchStats.LastKnownName ?? CurrentName;
            }

            LastPlayed = DateTime.UtcNow;
        }

        /// <summary>
        /// Update skill rating after match
        /// </summary>
        public void UpdateRating(double newMu, double newSigma)
        {
            Mu = newMu;
            Sigma = newSigma;
        }
    }
}
