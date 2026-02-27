using FlawsFightNight.Core.Enums.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawsFightNight.Core.Models.UT2004
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
        public List<string> PreviousNames { get; set; } = new();

        // Match History
        public int TotalMatches { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public DateTime LastPlayed { get; set; }
        public DateTime FirstSeen { get; set; }

        // ELO Ratings (UTStatsDB-style)
        public UT2004EloRating CaptureTheFlagElo { get; set; } = new();
        public UT2004EloRating TAMElo { get; set; } = new();
        public UT2004EloRating BombingRunElo { get; set; } = new();

        // OpenSkill Ratings
        public UT2004OpenSkillRating CaptureTheFlagRating { get; set; } = new(); // iCTF
        public UT2004OpenSkillRating TAMRating { get; set; } = new(); // TAM
        public UT2004OpenSkillRating BombingRunRating { get; set; } = new(); // iBR

        // Calculated Properties - General
        public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches : 0;
        public double KDRatio => TotalDeaths > 0 ? (double)TotalKills / TotalDeaths : TotalKills;
        public double AverageScorePerMatch => TotalMatches > 0 ? (double)TotalScore / TotalMatches : 0;

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

        // Cumulative iBR Stats
        public int TotalBRMatches { get; set; } = 0;
        public int TotalBRWins { get; set; } = 0;
        public int TotalBRLosses { get; set; } = 0;
        public int TotalBRScore { get; set; } = 0;
        public int TotalBRKills { get; set; } = 0;
        public int TotalBRDeaths { get; set; } = 0;
        public int TotalBRSuicides { get; set; } = 0;
        public int TotalBRHeadshots { get; set; } = 0;
        public int TotalBallCaptures { get; set; } = 0;         // ball_cap_final
        public int TotalBallScoreAssists { get; set; } = 0;     // ball_score_assist
        public int TotalBallThrownFinals { get; set; } = 0;     // ball_thrown_final
        public int TotalBombPickups { get; set; } = 0;          // bomb_pickup
        public int TotalBombDrops { get; set; } = 0;            // bomb_dropped
        public int TotalBombTaken { get; set; } = 0;            // bomb_taken
        public int TotalBombReturnedTimeouts { get; set; } = 0; // bomb_returned_timeout (attributable)
        public int MostBallCapsInMatch { get; set; } = 0;
        public int MostBombPickupsInMatch { get; set; } = 0;
        public int MostBombTakenInMatch { get; set; } = 0;

        public double BRWinRate => TotalBRMatches > 0 ? (double)TotalBRWins / TotalBRMatches : 0;
        public double BRKDRatio => TotalBRDeaths > 0 ? (double)TotalBRKills / TotalBRDeaths : TotalBRKills;
        public double AverageBallCapsPerBRMatch => TotalBRMatches > 0 ? (double)TotalBallCaptures / TotalBRMatches : 0;
        public double AverageBombPickupsPerBRMatch => TotalBRMatches > 0 ? (double)TotalBombPickups / TotalBRMatches : 0;

        // Cumulative iCTF Stats
        public int TotalCTFMatches { get; set; } = 0;
        public int TotalCTFWins { get; set; } = 0;
        public int TotalCTFLosses { get; set; } = 0;
        public int TotalCTFScore { get; set; } = 0;
        public int TotalCTFKills { get; set; } = 0;
        public int TotalCTFDeaths { get; set; } = 0;
        public int TotalCTFSuicides { get; set; } = 0;
        public int TotalCTFHeadshots { get; set; } = 0;
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
        public double CTFWinRate => TotalCTFMatches > 0 ? (double)TotalCTFWins / TotalCTFMatches : 0;
        public double AverageScorePerCTFMatch => TotalCTFMatches > 0 ? (double)TotalCTFScore / TotalCTFMatches : 0;
        public double AverageKillsPerCTFMatch => TotalCTFMatches > 0 ? (double)TotalCTFKills / TotalCTFMatches : 0;
        public double CTFKDRatio => TotalCTFDeaths > 0 ? (double)TotalCTFKills / TotalCTFDeaths : TotalCTFKills;
        public double AverageCapturesPerMatch => TotalCTFMatches > 0 ? (double)TotalFlagCaptures / TotalCTFMatches : 0;

        // Cumulative TAM Stats
        public int TotalTAMMatches { get; set; } = 0;
        public int TotalTAMWins { get; set; } = 0;
        public int TotalTAMLosses { get; set; } = 0;
        public int TotalTAMScore { get; set; } = 0;
        public int TotalTAMKills { get; set; } = 0;
        public int TotalTAMDeaths { get; set; } = 0;
        public int TotalTAMSuicides { get; set; } = 0;
        public int TotalTAMHeadshots { get; set; } = 0;
        public int TotalDamageDealt { get; set; } = 0;           // Sum of all damage dealt to enemies
        public int TotalDamageTaken { get; set; } = 0;           // Sum of all damage received
        public int TotalFriendlyFireDamage { get; set; } = 0;    // Damage dealt to teammates
        public int TotalRoundEndingKills { get; set; } = 0;      // Kills that ended a round
        public int TotalRoundsWon { get; set; } = 0;             // Individual rounds won
        public int TotalRoundsPlayed { get; set; } = 0;          // Total rounds participated in
        public int MostDamageInMatch { get; set; } = 0;
        public int MostRoundEndingKillsInMatch { get; set; } = 0;
        public int MostRoundsWonInMatch { get; set; } = 0;
        public double TAMWinRate => TotalTAMMatches > 0 ? (double)TotalTAMWins / TotalTAMMatches : 0;
        public double TAMRoundWinRate => TotalRoundsPlayed > 0 ? (double)TotalRoundsWon / TotalRoundsPlayed : 0;
        public double TAMKDRatio => TotalTAMDeaths > 0 ? (double)TotalTAMKills / TotalTAMDeaths : TotalTAMKills;
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
        // Weapon Stats - Cumulative across all game modes
        public Dictionary<string, int> TotalWeaponKills { get; set; } = new Dictionary<string, int>();

        // Weapon Stats - Detailed TAM accuracy tracking (cumulative)
        public Dictionary<string, WeaponStats> TotalWeaponStatistics { get; set; } = new Dictionary<string, WeaponStats>();

        public UT2004PlayerProfile() 
        { 
            // Keep default dates unset — they'll be set from match data during rebuild.
            FirstSeen = DateTime.MinValue;
            LastPlayed = DateTime.MinValue;
        }

        public UT2004PlayerProfile(string guid)
        {
            Guid = guid;
            FirstSeen = DateTime.MinValue;
            LastPlayed = DateTime.MinValue;
        }

        /// <summary>
        /// Update cumulative stats after a match
        /// </summary>
        public void UpdateStatsFromMatch(UTPlayerMatchStats matchStats, UT2004GameMode gameMode, DateTime matchDate)
        {
            // Set first-seen on first update
            if (FirstSeen == DateTime.MinValue)
                FirstSeen = matchDate;

            // Update LastPlayed to the match date
            LastPlayed = matchDate;

            TotalMatches++;
            if (matchStats.IsWinner) Wins++;
            else Losses++;

            // Combat Stats (overall)
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
        }

        private void UpdateCTFStats(UTPlayerMatchStats matchStats)
        {
            // Aggregate match count and W/L for iCTF
            TotalCTFMatches++;
            if (matchStats.IsWinner) TotalCTFWins++;
            else TotalCTFLosses++;

            // Per-mode combat aggregation
            TotalCTFScore += matchStats.Score;
            TotalCTFKills += matchStats.Kills;
            TotalCTFDeaths += matchStats.Deaths;
            TotalCTFSuicides += matchStats.Suicides;
            TotalCTFHeadshots += matchStats.Headshots;

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

            // Per-mode combat aggregation
            TotalTAMScore += matchStats.Score;
            TotalTAMKills += matchStats.Kills;
            TotalTAMDeaths += matchStats.Deaths;
            TotalTAMSuicides += matchStats.Suicides;
            TotalTAMHeadshots += matchStats.Headshots;

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
            // Aggregate match count and W/L
            TotalBRMatches++;
            if (matchStats.IsWinner) TotalBRWins++;
            else TotalBRLosses++;

            // Per-mode combat aggregation
            TotalBRScore += matchStats.Score;
            TotalBRKills += matchStats.Kills;
            TotalBRDeaths += matchStats.Deaths;
            TotalBRSuicides += matchStats.Suicides;
            TotalBRHeadshots += matchStats.Headshots;

            // Objective aggregation
            TotalBallCaptures += matchStats.BallCaptures;
            TotalBallScoreAssists += matchStats.BallScoreAssists;
            TotalBallThrownFinals += matchStats.BallThrownFinals;

            // Bomb events
            TotalBombPickups += matchStats.BombPickups;
            TotalBombDrops += matchStats.BombDrops;
            TotalBombTaken += matchStats.BombTaken;
            TotalBombReturnedTimeouts += matchStats.BombReturnedTimeouts;

            // Career bests for iBR
            MostBallCapsInMatch = Math.Max(MostBallCapsInMatch, matchStats.BallCaptures);
            MostBombPickupsInMatch = Math.Max(MostBombPickupsInMatch, matchStats.BombPickups);
            MostBombTakenInMatch = Math.Max(MostBombTakenInMatch, matchStats.BombTaken);
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
