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

        // Cumulative Combat Stats
        public int TotalScore { get; set; } = 0;
        public int TotalKills { get; set; } = 0;
        public int TotalDeaths { get; set; } = 0;
        public int TotalSuicides { get; set; } = 0;
        public int TotalHeadshots { get; set; } = 0;

        // Career Bests (for achievements/leaderboards)
        public int BestKillStreak { get; set; } = 0;
        public int BestMultiKill { get; set; } = 0;
        public int MostKillsInMatch { get; set; } = 0;
        public int MostDeathsInMatch { get; set; } = 0;
        public int HighestScoreInMatch { get; set; } = 0;

        // Cumulative iCTF Stats
        public int TotalFlagCaptures { get; set; } = 0;          // flag_cap_final
        public int TotalFlagGrabs { get; set; } = 0;             // flag_taken (picking up enemy flag from base)
        public int TotalFlagPickups { get; set; } = 0;           // flag_pickup (picking up dropped flag)
        public int TotalFlagDrops { get; set; } = 0;             // flag_dropped (dropping the flag)
        public int TotalFlagReturns { get; set; } = 0;           // Total flag returns (all types)
        public int TotalFlagReturnsEnemy { get; set; } = 0;      // flag_ret_enemy
        public int TotalFlagReturnsFriendly { get; set; } = 0;   // flag_ret_friendly
        public int TotalFlagDenials { get; set; } = 0;           // flag_denial
        public int TotalFlagCaptureAssists { get; set; } = 0;    // flag_cap_assist
        public int TotalFlagCaptureFirstTouch { get; set; } = 0; // flag_cap_1st_touch
        public int TotalTeamProtectFrags { get; set; } = 0;      // team_protect_frag
        public int TotalCriticalFrags { get; set; } = 0;         // critical_frag
        public int MostFlagCapsInMatch { get; set; } = 0;
        public int MostFlagReturnsInMatch { get; set; } = 0;

        // Cumulative TAM Stats
        public int TotalTAMMatches { get; set; } = 0;
        public int TotalTAMWins { get; set; } = 0;
        public int TotalTAMLosses { get; set; } = 0;
        public int TotalDamageDealt { get; set; } = 0;           // Sum of all damage dealt to enemies
        public int TotalDamageTaken { get; set; } = 0;           // Sum of all damage received
        public int TotalFriendlyFireDamage { get; set; } = 0;    // Damage dealt to teammates
        public int TotalRoundEndingKills { get; set; } = 0;      // Kills that ended a round
        public int TotalRoundsWon { get; set; } = 0;             // Individual rounds won
        public int TotalRoundsPlayed { get; set; } = 0;          // Total rounds participated in
        public int MostDamageInMatch { get; set; } = 0;
        public int MostRoundEndingKillsInMatch { get; set; } = 0;
        public int MostRoundsWonInMatch { get; set; } = 0;

        // Weapon Stats - Cumulative across all game modes
        public Dictionary<string, int> TotalWeaponKills { get; set; } = new Dictionary<string, int>();
        
        // Weapon Stats - Detailed TAM accuracy tracking (cumulative)
        public Dictionary<string, WeaponStats> TotalWeaponStatistics { get; set; } = new Dictionary<string, WeaponStats>();

        // Cumulative iBR Stats
        // TODO: Add when iBR is implemented

        // Calculated Properties - General
        public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches : 0;
        public double KDRatio => TotalDeaths > 0 ? (double)TotalKills / TotalDeaths : TotalKills;
        public double AverageScorePerMatch => TotalMatches > 0 ? (double)TotalScore / TotalMatches : 0;
        
        // Calculated Properties - iCTF
        public double AverageCapturesPerMatch => TotalMatches > 0 ? (double)TotalFlagCaptures / TotalMatches : 0;

        // Calculated Properties - TAM
        public double TAMWinRate => TotalTAMMatches > 0 ? (double)TotalTAMWins / TotalTAMMatches : 0;
        public double TAMRoundWinRate => TotalRoundsPlayed > 0 ? (double)TotalRoundsWon / TotalRoundsPlayed : 0;
        public double AverageDamagePerMatch => TotalTAMMatches > 0 ? (double)TotalDamageDealt / TotalTAMMatches : 0;
        public double AverageDamagePerRound => TotalRoundsPlayed > 0 ? (double)TotalDamageDealt / TotalRoundsPlayed : 0;
        public double AverageRoundsWonPerMatch => TotalTAMMatches > 0 ? (double)TotalRoundsWon / TotalTAMMatches : 0;
        public double DamageEfficiency => TotalDamageTaken > 0 ? (double)TotalDamageDealt / TotalDamageTaken : TotalDamageDealt;
        public double OverallWeaponAccuracy
        {
            get
            {
                int totalShots = TotalWeaponStatistics.Values.Sum(w => w.ShotsFired);
                int totalHits = TotalWeaponStatistics.Values.Sum(w => w.Hits);
                return totalShots > 0 ? (double)totalHits / totalShots * 100.0 : 0.0;
            }
        }

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
        public void UpdateStatsFromMatch(UTPlayerMatchStats matchStats, UT2004GameMode gameMode)
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

            // Game Mode Specific Stats
            if (gameMode == UT2004GameMode.iCTF)
            {
                UpdateCTFStats(matchStats);
            }
            else if (gameMode == UT2004GameMode.TAM)
            {
                UpdateTAMStats(matchStats);
            }
            else if (gameMode == UT2004GameMode.iBR)
            {
                UpdateBRStats(matchStats);
            }

            // Update career bests (general)
            BestKillStreak = Math.Max(BestKillStreak, matchStats.BestKillStreak);
            BestMultiKill = Math.Max(BestMultiKill, matchStats.BestMultiKill);
            MostKillsInMatch = Math.Max(MostKillsInMatch, matchStats.Kills);
            MostDeathsInMatch = Math.Max(MostDeathsInMatch, matchStats.Deaths);
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

            // Update weapon statistics (accuracy tracking)
            foreach (var weaponStat in matchStats.WeaponStatistics)
            {
                if (!TotalWeaponStatistics.ContainsKey(weaponStat.Key))
                {
                    TotalWeaponStatistics[weaponStat.Key] = new WeaponStats
                    {
                        WeaponName = weaponStat.Key
                    };
                }

                var totalStat = TotalWeaponStatistics[weaponStat.Key];
                totalStat.ShotsFired += weaponStat.Value.ShotsFired;
                totalStat.Hits += weaponStat.Value.Hits;
                totalStat.DamageDealt += weaponStat.Value.DamageDealt;
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

        private void UpdateCTFStats(UTPlayerMatchStats matchStats)
        {
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

            // Career bests
            MostFlagCapsInMatch = Math.Max(MostFlagCapsInMatch, matchStats.FlagCaptures);
            MostFlagReturnsInMatch = Math.Max(MostFlagReturnsInMatch, matchStats.FlagReturns);
        }

        private void UpdateTAMStats(UTPlayerMatchStats matchStats)
        {
            TotalTAMMatches++;
            if (matchStats.IsWinner) TotalTAMWins++;
            else TotalTAMLosses++;

            // TAM Combat Stats
            TotalDamageDealt += matchStats.TotalDamageDealt;
            TotalDamageTaken += matchStats.TotalDamageTaken;
            TotalFriendlyFireDamage += matchStats.FriendlyFireDamage;
            
            // TAM Round Stats
            TotalRoundEndingKills += matchStats.RoundEndingKills;
            TotalRoundsWon += matchStats.RoundsWon;
            TotalRoundsPlayed += matchStats.RoundsPlayed;

            // TAM uses TeamProtectFrags and CriticalFrags too
            TotalTeamProtectFrags += matchStats.TeamProtectFrags;
            TotalCriticalFrags += matchStats.CriticalFrags;

            // Career bests
            MostDamageInMatch = Math.Max(MostDamageInMatch, matchStats.TotalDamageDealt);
            MostRoundEndingKillsInMatch = Math.Max(MostRoundEndingKillsInMatch, matchStats.RoundEndingKills);
            MostRoundsWonInMatch = Math.Max(MostRoundsWonInMatch, matchStats.RoundsWon);
        }

        private void UpdateBRStats(UTPlayerMatchStats matchStats)
        {
            // TODO: Implement when iBR stats are defined
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
