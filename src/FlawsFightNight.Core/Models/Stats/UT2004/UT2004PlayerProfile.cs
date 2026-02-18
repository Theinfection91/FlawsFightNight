using FlawsFightNight.Core.Enums.UT2004;
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

        // Skill Ratings
        public UT2004GameRating CaptureTheFlagRating { get; set; } = new(); // iCTF
        public UT2004GameRating TAMRating { get; set; } = new(); // TAM
        public UT2004GameRating BombingRunRating { get; set; } = new(); // iBR

        public double CTFMu { get; set; } = 25.0;
        public double CTFSigma { get; set; } = 25.0 / 3.0;
        public double CTFRating => CTFMu - (3 * CTFSigma);

        // ReTAM Skill Rating

        // Cumulative Combat Stats
        public int TotalScore { get; set; } = 0;
        public int TotalKills { get; set; } = 0;
        public int TotalDeaths { get; set; } = 0;
        public int TotalSuicides { get; set; } = 0;
        public int TotalHeadshots { get; set; } = 0;

        // Cumulative Flag Objective Stats - Primary Actions
        public int TotalFlagCaptures { get; set; } = 0;          // flag_cap_final
        public int TotalFlagGrabs { get; set; } = 0;             // flag_taken (picking up enemy flag from base)
        public int TotalFlagPickups { get; set; } = 0;           // flag_pickup (picking up dropped flag)
        public int TotalFlagDrops { get; set; } = 0;             // flag_dropped (dropping the flag)

        // Cumulative Flag Objective Stats - Defensive Actions
        public int TotalFlagReturns { get; set; } = 0;           // Total flag returns (all types)
        public int TotalFlagReturnsEnemy { get; set; } = 0;      // flag_ret_enemy
        public int TotalFlagReturnsFriendly { get; set; } = 0;   // flag_ret_friendly
        public int TotalFlagDenials { get; set; } = 0;           // flag_denial

        // Cumulative Flag Objective Stats - Support Actions
        public int TotalFlagCaptureAssists { get; set; } = 0;    // flag_cap_assist
        public int TotalFlagCaptureFirstTouch { get; set; } = 0; // flag_cap_1st_touch
        public int TotalTeamProtectFrags { get; set; } = 0;      // team_protect_frag
        public int TotalCriticalFrags { get; set; } = 0;         // critical_frag

        // Career Bests (for achievements/leaderboards)
        public int BestKillStreak { get; set; } = 0;
        public int BestMultiKill { get; set; } = 0;
        public int MostKillsInMatch { get; set; } = 0;
        public int MostDeathsInMatch { get; set; } = 0;
        public int MostFlagCapsInMatch { get; set; } = 0;
        public int MostFlagReturnsInMatch { get; set; } = 0;
        public int HighestScoreInMatch { get; set; } = 0;

        // Weapon Stats - Cumulative kills per weapon
        public Dictionary<string, int> TotalWeaponKills { get; set; } = new Dictionary<string, int>();

        // Calculated Properties
        public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches : 0;
        public double KDRatio => TotalDeaths > 0 ? (double)TotalKills / TotalDeaths : TotalKills;
        public double AverageScorePerMatch => TotalMatches > 0 ? (double)TotalScore / TotalMatches : 0;
        public double AverageCapturesPerMatch => TotalMatches > 0 ? (double)TotalFlagCaptures / TotalMatches : 0;

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

            // Combat Stats
            TotalScore += matchStats.Score;
            TotalKills += matchStats.Kills;
            TotalDeaths += matchStats.Deaths;
            TotalSuicides += matchStats.Suicides;
            TotalHeadshots += matchStats.Headshots;

            // Flag Objective Stats - Primary Actions
            TotalFlagCaptures += matchStats.FlagCaptures;
            TotalFlagGrabs += matchStats.FlagGrabs;
            TotalFlagPickups += matchStats.FlagPickups;
            TotalFlagDrops += matchStats.FlagDrops;

            // Flag Objective Stats - Defensive Actions
            TotalFlagReturns += matchStats.FlagReturns;
            TotalFlagReturnsEnemy += matchStats.FlagReturnsEnemy;
            TotalFlagReturnsFriendly += matchStats.FlagReturnsFriendly;
            TotalFlagDenials += matchStats.FlagDenials;

            // Flag Objective Stats - Support Actions
            TotalFlagCaptureAssists += matchStats.FlagCaptureAssists;
            TotalFlagCaptureFirstTouch += matchStats.FlagCaptureFirstTouch;
            TotalTeamProtectFrags += matchStats.TeamProtectFrags;
            TotalCriticalFrags += matchStats.CriticalFrags;

            // Update career bests
            BestKillStreak = Math.Max(BestKillStreak, matchStats.BestKillStreak);
            BestMultiKill = Math.Max(BestMultiKill, matchStats.BestMultiKill);
            MostKillsInMatch = Math.Max(MostKillsInMatch, matchStats.Kills);
            MostDeathsInMatch = Math.Max(MostDeathsInMatch, matchStats.Deaths);
            MostFlagCapsInMatch = Math.Max(MostFlagCapsInMatch, matchStats.FlagCaptures);
            MostFlagReturnsInMatch = Math.Max(MostFlagReturnsInMatch, matchStats.FlagReturns);
            HighestScoreInMatch = Math.Max(HighestScoreInMatch, matchStats.Score);

            // Update weapon kill totals
            foreach (var weaponKill in matchStats.WeaponKills)
            {
                if (!TotalWeaponKills.ContainsKey(weaponKill.Key))
                {
                    TotalWeaponKills[weaponKill.Key] = 0;
                }
                TotalWeaponKills[weaponKill.Key] += weaponKill.Value;
            }

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

        public void GetMuSigma(UT2004GameMode gameMode, out double mu, out double sigma)
        {
            switch (gameMode)
            {
                case UT2004GameMode.iCTF:
                    mu = CaptureTheFlagRating.Mu;
                    sigma = CaptureTheFlagRating.Sigma;
                    break;
                case UT2004GameMode.TAM:
                    mu = TAMRating.Mu;
                    sigma = TAMRating.Sigma;
                    break;
                case UT2004GameMode.iBR:
                    mu = BombingRunRating.Mu;
                    sigma = BombingRunRating.Sigma;
                    break;
                default:
                    mu = 25.0;
                    sigma = 25.0 / 3.0;
                    break;
            }
        }

        public void UpdateSkillRating(UT2004GameMode gameMode, double newMu, double newSigma)
        {
            switch (gameMode)
            {
                case UT2004GameMode.iCTF:
                    CaptureTheFlagRating.UpdateSkillRating(newMu, newSigma);
                    break;
                case UT2004GameMode.TAM:
                    TAMRating.UpdateSkillRating(newMu, newSigma);
                    break;
                case UT2004GameMode.iBR:
                    BombingRunRating.UpdateSkillRating(newMu, newSigma);
                    break;
            }
        }
    }
}
