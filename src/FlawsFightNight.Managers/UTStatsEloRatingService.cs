using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawsFightNight.Managers
{
    /// <summary>
    /// UTStatsDB-style ELO variant for team games (documentation mode) with conservative safety guards.
    /// Conservative fix:
    ///  - clamp opponent average score to >= 1.0 before RankCalc
    ///  - per-match change capped to MaxSingleMatchRatingChange (default 100)
    ///  - time-scaling capped (MaxTimeScaleRatio = 2.0)
    ///  - exclude very short playtimes from avg-time (< MinPlayerTimeForScaling = 30s)
    ///  - skip trivial matches where opponentAvgScore <= 1 AND playerScore <= 1 (non-events)
    /// </summary>
    public class UTStatsEloRatingService
    {
        public bool RankBots { get; set; } = false;
        public int MinRankTime { get; set; } = 0;
        public int MinRankMatches { get; set; } = 0;
        public int SkippedYoungPlayers { get; set; } = 0;

        // Team multiplier applied after RankCalc (K=16 inside RankCalc)
        public double TeamGameMultiplier { get; set; } = 6.0;

        // Safety caps for time-scaling
        private const double MaxTimeScaleRatio = 1.0;
        private const double MinPlayerTimeForScaling = 60.0;

        // Conservative: maximum rating change allowed from a single match (prevents extreme spikes)
        public double MaxSingleMatchRatingChange { get; set; } = 100.0;

        // Debugging helpers (off by default)
        public bool VerboseLogging { get; set; } = false;
        // If set, only log entries for this GUID (helps reduce noise)
        public string? VerbosePlayerGuid { get; set; } = "cc64eb45e190de68c0deaf75231e1ab8";

        public UTStatsEloRatingService(bool rankBots = false, int minRankTime = 0, int minRankMatches = 0)
        {
            RankBots = rankBots;
            MinRankTime = minRankTime;
            MinRankMatches = minRankMatches;
        }

        // RankCalc implements UTStatsDB core: change = round(16 * (actual - expected), 8)
        private double RankCalc(double rank1, double rank2, double score1, double score2)
        {
            double mscore1 = Math.Max(0.0, score1);
            double mscore2 = Math.Max(0.0, score2);

            double calc = 1.0 + Math.Pow(10.0, (-(rank1 - rank2) / 400.0));
            double dif = calc == 0.0 ? 1.0 : 1.0 / calc;

            double basePerf = (mscore1 + mscore2) == 0.0 ? 0.5 : mscore1 / (mscore1 + mscore2);

            return Math.Round(16.0 * (basePerf - dif), 8);
        }

        /// <summary>
        /// Updates ELO ratings for a team match (CTF/BR/TAM).
        /// Uses opponent-team averages per documentation mode with conservative safety guards.
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            if (match == null) return;

            if (match.GameMode != UT2004GameMode.iCTF &&
                match.GameMode != UT2004GameMode.iBR &&
                match.GameMode != UT2004GameMode.TAM) return;

            if (match.Players == null || match.Players.Count != 2) return;

            var team0 = match.Players[0].Where(p => RankBots || !p.IsBot).ToList();
            var team1 = match.Players[1].Where(p => RankBots || !p.IsBot).ToList();

            if (!team0.Any() || !team1.Any()) return;

            foreach (var player in match.Players)
            {
                foreach (var playerStats in player)
                {
                    if (playerStats.FlagReturns > 1000)
                    {
                        Console.WriteLine($"[DEBUG] Player {playerStats.LastKnownName} ({playerStats.Guid}) has unusually high FlagReturns: {playerStats.FlagReturns} on {match.MatchDate}");
                    }
                }
            }

            double avgRank0 = team0
                .Select(p => profiles.TryGetValue(p.Guid, out var pr) ? GetCurrentEloRating(pr, match.GameMode) : (double?)null)
                .Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0.0).Average();

            double avgRank1 = team1
                .Select(p => profiles.TryGetValue(p.Guid, out var pr) ? GetCurrentEloRating(pr, match.GameMode) : (double?)null)
                .Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0.0).Average();

            // Compute raw averages first (used to detect trivial matches)
            double rawAvgScore0 = team0.Any() ? team0.Average(p => p.Score) : 0.0;
            double rawAvgScore1 = team1.Any() ? team1.Average(p => p.Score) : 0.0;

            // Conservative clamp: opponent average score minimum 1.0 to avoid extreme basePerf when opponentAvgScore ~= 0
            double avgScore0 = Math.Max(1.0, rawAvgScore0);
            double avgScore1 = Math.Max(1.0, rawAvgScore1);

            var allPlayers = team0.Concat(team1).ToList();
            var timedPlayers = allPlayers.Where(p => p.TotalTimeSeconds >= MinPlayerTimeForScaling).ToList();
            double avgTimeSeconds = timedPlayers.Any() ? timedPlayers.Average(p => p.TotalTimeSeconds) : 0.0;

            // For each player, skip trivial matches where opponent avg <= 1 AND player score <= 1
            foreach (var p in team0)
            {
                if (!profiles.ContainsKey(p.Guid)) continue;

                // Skip trivial/non-event matches (conservative); prevents RankCalc explosion on degenerate logs
                if (rawAvgScore1 <= 5.0 && p.Score <= 5.0)
                    continue;

                ProcessPlayerRankChange(p, profiles[p.Guid], match.GameMode,
                    opponentAvgRank: avgRank1, opponentAvgScore: avgScore1, avgMatchTimeSeconds: avgTimeSeconds, matchDate: match.MatchDate);
            }

            foreach (var p in team1)
            {
                if (!profiles.ContainsKey(p.Guid)) continue;

                // Skip trivial/non-event matches (conservative)
                if (rawAvgScore0 <= 1.0 && p.Score <= 1.0)
                    continue;

                ProcessPlayerRankChange(p, profiles[p.Guid], match.GameMode,
                    opponentAvgRank: avgRank0, opponentAvgScore: avgScore0, avgMatchTimeSeconds: avgTimeSeconds, matchDate: match.MatchDate);
            }
        }

        private void ProcessPlayerRankChange(
            UTPlayerMatchStats playerStats,
            UT2004PlayerProfile profile,
            UT2004GameMode gameMode,
            double opponentAvgRank,
            double opponentAvgScore,
            double avgMatchTimeSeconds,
            DateTime matchDate)
        {
            if (profile == null || playerStats == null) return;

            if (playerStats.TotalTimeSeconds < MinPlayerTimeForScaling)
            {
                // treat very short participation as non-rated
                SkippedYoungPlayers++;
                return;
            }

            if (MinRankTime > 0 && GetTotalPlayTime(profile) < MinRankTime) { SkippedYoungPlayers++; return; }
            if (MinRankMatches > 0 && GetTotalMatches(profile, gameMode) < MinRankMatches) { SkippedYoungPlayers++; return; }

            double currentRank = GetCurrentEloRating(profile, gameMode);
            double playerScore = Math.Max(0.0, playerStats.Score);

            // Ensure opponentAvgScore respects the conservative minimum
            opponentAvgScore = Math.Max(1.0, opponentAvgScore);

            double rcBefore = RankCalc(currentRank, opponentAvgRank, playerScore, opponentAvgScore);
            double rc = rcBefore * TeamGameMultiplier;

            double playerTime = playerStats.TotalTimeSeconds;
            double appliedScale = 1.0;
            if (playerTime >= MinPlayerTimeForScaling && avgMatchTimeSeconds >= MinPlayerTimeForScaling)
            {
                if (rc > 0.0)
                {
                    appliedScale = Math.Min(avgMatchTimeSeconds / playerTime, MaxTimeScaleRatio);
                    rc *= appliedScale;
                }
                else if (rc < 0.0)
                {
                    appliedScale = Math.Min(playerTime / avgMatchTimeSeconds, MaxTimeScaleRatio);
                    rc *= appliedScale;
                }
            }

            // Clamp per-match rating change to avoid extreme single-match spikes
            if (!double.IsNaN(MaxSingleMatchRatingChange) && MaxSingleMatchRatingChange > 0.0)
            {
                rc = Math.Max(-MaxSingleMatchRatingChange, Math.Min(MaxSingleMatchRatingChange, rc));
            }

            // Unbalanced-rank gating
            bool shouldApply = false;
            if (rc > 0.0 || opponentAvgScore > 0.0)
            {
                if (rc < 0.0 || currentRank < opponentAvgRank + 250.0 || currentRank < opponentAvgRank * 8.0)
                    shouldApply = true;
            }
            if (!shouldApply && rc < 0.0) shouldApply = true;
            if (!shouldApply) return;

            // Floor at 0
            if (rc < 0.0 && (currentRank + rc) < 0.0) rc = -currentRank;

            // Verbose trace (filtered)
            if (VerboseLogging && (VerbosePlayerGuid == null || VerbosePlayerGuid == profile.Guid))
            {
                Console.WriteLine($"[ELO TRACE] MatchDate={matchDate:yyyy-MM-dd} GUID={profile.Guid} Name={profile.CurrentName}");
                Console.WriteLine($"  Mode={gameMode} CurrRank={currentRank:F4} PlayerScore={playerScore:F2} OppAvgScore={opponentAvgScore:F2} OppAvgRank={opponentAvgRank:F4}");
                Console.WriteLine($"  RankCalc(before*K)={rcBefore:F6} TeamK={TeamGameMultiplier} rcRaw={rcBefore * TeamGameMultiplier:F6}");
                Console.WriteLine($"  PlayerTime={playerTime}s AvgTime={avgMatchTimeSeconds}s AppliedScale={appliedScale:F4} rcAfterScale={rc:F6}");
                Console.WriteLine($"  FinalChange={rc:F6} NewRank={Math.Max(0.0, currentRank + rc):F6}");
            }

            SetEloRating(profile, gameMode, currentRank + rc, rc);
        }

        private double GetCurrentEloRating(UT2004PlayerProfile profile, UT2004GameMode gameMode)
        {
            return gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagElo.Rating,
                UT2004GameMode.iBR  => profile.BombingRunElo.Rating,
                UT2004GameMode.TAM  => profile.TAMElo.Rating,
                _                   => 0.0
            };
        }

        private void SetEloRating(UT2004PlayerProfile profile, UT2004GameMode gameMode, double newRating, double change)
        {
            var elo = gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagElo,
                UT2004GameMode.iBR  => profile.BombingRunElo,
                UT2004GameMode.TAM  => profile.TAMElo,
                _                   => null
            };
            elo?.UpdateRating(newRating, change);
        }

        private int GetTotalPlayTime(UT2004PlayerProfile profile) => profile.TotalMatches * 600;
        private int GetTotalMatches(UT2004PlayerProfile profile, UT2004GameMode gameMode) => gameMode switch
        {
            UT2004GameMode.iCTF => profile.TotalCTFMatches,
            UT2004GameMode.iBR  => profile.TotalBRMatches,
            UT2004GameMode.TAM  => profile.TotalTAMMatches,
            _                   => 0
        };
    }
}