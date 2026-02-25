using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawsFightNight.Managers
{
    /// <summary>
    /// Implements the UTStatsDB ELO variant ranking system.
    /// Team-game branch (CTF/BR/TAM): raw tscore vs ALL-match average, no bonuses, *8, time-scaled.
    /// </summary>
    public class UTStatsEloRatingService
    {
        // Configuration
        public bool RankBots { get; set; } = false;
        public int MinRankTime { get; set; } = 0;
        public int MinRankMatches { get; set; } = 0;

        // Statistics
        public int SkippedYoungPlayers { get; set; } = 0;

        // Canonical UTStatsDB team multiplier (*8 applied after RankCalc K=16)
        public double TeamGameMultiplier { get; set; } = 8.0;

        // Cap on time-scaling ratio to prevent extreme amplification from late-joiners
        private const double MaxTimeScaleRatio = 2.0;

        // Minimum player active time (seconds) required to apply time scaling
        private const double MinPlayerTimeForScaling = 30.0;

        public UTStatsEloRatingService(bool rankBots = false, int minRankTime = 0, int minRankMatches = 0)
        {
            RankBots = rankBots;
            MinRankTime = minRankTime;
            MinRankMatches = minRankMatches;
        }

        /// <summary>
        /// Core ELO calculation formula from UTStatsDB (K=16).
        /// </summary>
        private double RankCalc(double rank1, double rank2, double score1, double score2)
        {
            double mscore1 = Math.Max(0.0, score1);
            double mscore2 = Math.Max(0.0, score2);

            double calc = 1.0 + Math.Pow(10.0, (-(rank1 - rank2) / 400.0));
            double dif = calc == 0.0 ? 1.0 : 1.0 / calc;

            double basePerf = (mscore1 + mscore2) == 0 ? 0.5 : mscore1 / (mscore1 + mscore2);

            return Math.Round(16.0 * (basePerf - dif), 8);
        }

        /// <summary>
        /// Updates ELO ratings for all players in a match.
        /// Matches PHP team branch exactly: raw tscore, all-match averages, no bonus adjustments.
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            if (match.GameMode != UT2004GameMode.iCTF &&
                match.GameMode != UT2004GameMode.iBR &&
                match.GameMode != UT2004GameMode.TAM)
            {
                return;
            }

            if (match.Players.Count != 2 ||
                !match.Players[0].Any() ||
                !match.Players[1].Any())
            {
                return;
            }

            var team0Players = match.Players[0].Where(p => RankBots || !p.IsBot).ToList();
            var team1Players = match.Players[1].Where(p => RankBots || !p.IsBot).ToList();

            if (!team0Players.Any() || !team1Players.Any())
                return;

            var allPlayers = team0Players.Concat(team1Players).ToList();

            // PHP: all-match averages computed across BOTH teams combined
            // $totrank, $totscore, $tottime summed over all players then divided by $totcount
            var allRanks = allPlayers
                .Select(p => profiles.TryGetValue(p.Guid, out var prof)
                    ? GetCurrentEloRating(prof, match.GameMode)
                    : (double?)null)
                .Where(v => v.HasValue).Select(v => v.Value).ToList();

            if (!allRanks.Any()) return;

            // PHP: $avgrank = $totrank / $totcount  (all players, both teams)
            double avgRankAll = allRanks.Average();

            // PHP: $avgscore = $totscore / $totcount  (raw tscore, no bonus adjustments)
            double avgScoreAll = allPlayers.Average(p => p.Score);

            // PHP: $avgtime = $tottime / $totcount  (exclude zero-time players from average)
            var timedPlayers = allPlayers.Where(p => p.TotalTimeSeconds >= MinPlayerTimeForScaling).ToList();
            double avgTimeSeconds = timedPlayers.Any() ? timedPlayers.Average(p => p.TotalTimeSeconds) : 0.0;

            // Process each player individually against all-match averages (PHP team branch)
            foreach (var player in allPlayers)
            {
                if (!profiles.ContainsKey(player.Guid)) continue;
                ProcessPlayerRankChange(player, profiles[player.Guid], match.GameMode,
                    avgRankAll, avgScoreAll, avgTimeSeconds);
            }
        }

        /// <summary>
        /// Processes rank change for a single player.
        /// PHP team branch: rc = rankcalc(playerRank, avgRankAll, playerTScore, avgScoreAll) * 8
        /// then time-scaled, then unbalanced-rank gating.
        /// NOTE: No spree/multi/suicide adjustments — those only exist in PHP's rank-by-kills branch
        /// which is disabled for team games (CTF/BR/TAM).
        /// </summary>
        private void ProcessPlayerRankChange(
            UTPlayerMatchStats playerStats,
            UT2004PlayerProfile profile,
            UT2004GameMode gameMode,
            double avgRankAllMatch,
            double avgScoreAllMatch,
            double avgMatchTimeSeconds)
        {
            int totalPlayTime = GetTotalPlayTime(profile);
            int totalMatches = GetTotalMatches(profile, gameMode);

            if (MinRankTime > 0 && totalPlayTime < MinRankTime)
            {
                SkippedYoungPlayers++;
                return;
            }

            if (MinRankMatches > 0 && totalMatches < MinRankMatches)
            {
                SkippedYoungPlayers++;
                return;
            }

            double currentRank = GetCurrentEloRating(profile, gameMode);

            // PHP team branch: raw tscore (Score), no spree/multi/suicide adjustments
            double playerScore = playerStats.Score;

            // rc = rankcalc(playerRank, avgRankAll, playerTScore, avgTScoreAll) * 8
            double rc = RankCalc(currentRank, avgRankAllMatch, playerScore, avgScoreAllMatch)
                        * TeamGameMultiplier;

            // PHP time-scaling: (avgtime/matchlength) / (playertime/matchlength) = avgtime/playertime
            // Applied only when both values are meaningful; capped to prevent extreme amplification
            double playerTime = playerStats.TotalTimeSeconds;

            if (playerTime >= MinPlayerTimeForScaling && avgMatchTimeSeconds >= MinPlayerTimeForScaling)
            {
                if (rc > 0.0)
                {
                    double ratio = Math.Min(avgMatchTimeSeconds / playerTime, MaxTimeScaleRatio);
                    rc *= ratio;
                }
                else if (rc < 0.0)
                {
                    double ratio = Math.Min(playerTime / avgMatchTimeSeconds, MaxTimeScaleRatio);
                    rc *= ratio;
                }
            }
            // else: no time data — use raw rc unchanged (matches PHP's zero-time → rc=0 guard
            // but we don't zero it out since that caused all ratings to break)

            // PHP unbalanced-rank gating:
            // if ($rc > 0 || $avgscore > 0)
            //   if ($rc < 0 || $pr1 < $avgrank + 250 || $pr1 < $avgrank * 8)
            //     rankc += rc
            bool shouldApply = false;
            if (rc > 0.0 || avgScoreAllMatch > 0.0)
            {
                if (rc < 0.0 || currentRank < avgRankAllMatch + 250.0 || currentRank < avgRankAllMatch * 8.0)
                    shouldApply = true;
            }

            if (!shouldApply && rc < 0.0)
                shouldApply = true;

            if (!shouldApply)
                return;

            // Floor: negative ranks not allowed
            if (rc < 0.0 && (currentRank + rc) < 0.0)
                rc = -currentRank;

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
            var eloRating = gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagElo,
                UT2004GameMode.iBR  => profile.BombingRunElo,
                UT2004GameMode.TAM  => profile.TAMElo,
                _                   => null
            };

            eloRating?.UpdateRating(newRating, change);
        }

        private int GetTotalPlayTime(UT2004PlayerProfile profile)
        {
            return profile.TotalMatches * 600;
        }

        private int GetTotalMatches(UT2004PlayerProfile profile, UT2004GameMode gameMode)
        {
            return gameMode switch
            {
                UT2004GameMode.iCTF => profile.TotalCTFMatches,
                UT2004GameMode.iBR  => profile.TotalBRMatches,
                UT2004GameMode.TAM  => profile.TotalTAMMatches,
                _                   => 0
            };
        }
    }
}