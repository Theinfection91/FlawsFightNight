using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawsFightNight.Managers
{
    /// <summary>
    /// Implements the UTStatsDB ELO variant ranking system.
    /// Based on ELO chess ranking with game-specific modifiers for sprees, multikills, and objective scoring.
    /// </summary>
    public class UTStatsEloRatingService
    {
        // Configuration
        public bool RankBots { get; set; } = false;
        public int MinRankTime { get; set; } = 0; // Minimum seconds played globally
        public int MinRankMatches { get; set; } = 0; // Minimum matches played globally
        
        // Statistics
        public int SkippedYoungPlayers { get; set; } = 0;

        public UTStatsEloRatingService(bool rankBots = false, int minRankTime = 0, int minRankMatches = 0)
        {
            RankBots = rankBots;
            MinRankTime = minRankTime;
            MinRankMatches = minRankMatches;
        }

        /// <summary>
        /// Core ELO calculation formula from UTStatsDB.
        /// Formula: 16.0 * (actual_performance - expected_performance)
        /// </summary>
        private double RankCalc(double rank1, double rank2, double score1, double score2)
        {
            double mrank1 = rank1;
            double mrank2 = rank2;
            double mscore1 = score1;
            double mscore2 = score2;

            // Normalize negative scores to zero
            if (mscore1 < 0)
            {
                mscore1 -= mscore1;
                mscore2 -= mscore1;
            }
            if (mscore2 < 0)
            {
                mscore2 -= mscore2;
                mscore1 -= mscore2;
            }

            // Expected performance calculation using ELO formula
            double calc = 1.0 + Math.Pow(10.0, (-(mrank1 - mrank2) / 400.0));
            double dif = calc == 0.0 ? 1.0 : 1.0 / calc;

            // Actual performance ratio
            double basePerf = (mscore1 + mscore2) == 0 ? 0.5 : mscore1 / (mscore1 + mscore2);

            // Change = K-factor * (actual - expected)
            double change = Math.Round(16.0 * (basePerf - dif), 8);

            return change;
        }

        /// <summary>
        /// Updates ELO ratings for all players in a match.
        /// Supports CTF, Bombing Run, and TAM game modes with score-based ranking.
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            // Only process team-based game modes we support
            if (match.GameMode != UT2004GameMode.iCTF && 
                match.GameMode != UT2004GameMode.iBR && 
                match.GameMode != UT2004GameMode.TAM)
            {
                return;
            }

            // Skip if teams don't exist or are empty
            if (match.Players.Count != 2 || 
                !match.Players[0].Any() || 
                !match.Players[1].Any())
            {
                return;
            }

            var team0 = match.Players[0]; // Red team
            var team1 = match.Players[1]; // Blue team

            // Build player reference arrays (exclude bots if configured)
            var team0Players = team0.Where(p => RankBots || !p.IsBot).ToList();
            var team1Players = team1.Where(p => RankBots || !p.IsBot).ToList();

            if (!team0Players.Any() || !team1Players.Any())
            {
                return;
            }

            // Calculate average rank and score for team-based comparison
            // safe average rank using only profiles that exist
            var team0Ranks = team0Players
                .Select(p => profiles.TryGetValue(p.Guid, out var prof) ? GetCurrentEloRating(prof, match.GameMode) : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
            var team1Ranks = team1Players
                .Select(p => profiles.TryGetValue(p.Guid, out var prof) ? GetCurrentEloRating(prof, match.GameMode) : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
            if (!team0Ranks.Any() || !team1Ranks.Any()) return;
            double avgRank0 = team0Ranks.Average();
            double avgRank1 = team1Ranks.Average();
            double avgScore0 = team0Players.Average(p => p.Score);
            double avgScore1 = team1Players.Average(p => p.Score);

            // Process each player against opposing team's average
            foreach (var player in team0Players)
            {
                if (!profiles.ContainsKey(player.Guid)) continue;

                ProcessPlayerRankChange(player, profiles[player.Guid], match.GameMode, 
                    avgRank0, avgRank1, avgScore0, avgScore1);
            }

            foreach (var player in team1Players)
            {
                if (!profiles.ContainsKey(player.Guid)) continue;

                ProcessPlayerRankChange(player, profiles[player.Guid], match.GameMode, 
                    avgRank1, avgRank0, avgScore1, avgScore0);
            }
        }

        /// <summary>
        /// Processes rank change for a single player based on team performance.
        /// </summary>
        private void ProcessPlayerRankChange(
            UTPlayerMatchStats playerStats,
            UT2004PlayerProfile profile,
            UT2004GameMode gameMode,
            double myTeamAvgRank,
            double opponentTeamAvgRank,
            double myTeamAvgScore,
            double opponentTeamAvgScore)
        {
            // Check minimum requirements for ranking
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

            // Get current player rating
            double currentRank = GetCurrentEloRating(profile, gameMode);

            // Calculate adjusted score with bonuses
            double adjustedScore = CalculateAdjustedScore(playerStats, gameMode);

            // Calculate rank change using team averages
            // RankCalc already has K-factor of 16 built in
            // For team games, we use a small multiplier for more gradual changes
            double rankChange = RankCalc(currentRank, opponentTeamAvgRank, adjustedScore, opponentTeamAvgScore);

            // Apply rating floor - prevent going negative
            if (rankChange < 0)
            {
                double newRank = currentRank + rankChange;
                if (newRank < 0)
                {
                    rankChange = -currentRank; // Cap at 0
                }
            }

            // Apply the rank change
            SetEloRating(profile, gameMode, currentRank + rankChange, rankChange);
        }

        /// <summary>
        /// Calculates adjusted score with spree and multikill bonuses per UTStatsDB formula.
        /// </summary>
        private double CalculateAdjustedScore(UTPlayerMatchStats stats, UT2004GameMode gameMode)
        {
            double score = stats.Score;

            // Add killing spree bonuses
            // BestKillStreak translates to spree levels:
            // 5-9 = Killing Spree (level 0)
            // 10-14 = Rampage (level 1)
            // 15-19 = Dominating (level 2)
            // 20-24 = Unstoppable (level 3)
            // 25-29 = God Like (level 4)
            // 30+ = Wicked Sick (level 5)
            if (stats.BestKillStreak >= 5)
            {
                int spreeLevel = Math.Min((stats.BestKillStreak - 5) / 5, 5);
                for (int i = 0; i <= spreeLevel; i++)
                {
                    score += (i + 1); // Level 0 = +1, Level 1 = +2, etc.
                }
            }

            // Add multikill bonuses
            // BestMultiKill: 2=Double, 3=Multi, 4=Mega, 5=Ultra, 6=Monster, 7=Ludicrous, 8+=Holy Shit
            // PHP code shows bonuses: level 0 = +1, level 1 = +2, etc.
            if (stats.BestMultiKill >= 2)
            {
                int multiLevel = Math.Min(stats.BestMultiKill - 2, 6);
                for (int i = 0; i <= multiLevel; i++)
                {
                    score += (i + 1);
                }
            }

            // Subtract suicide penalty
            // Formula: (suicides / (kills + deaths)) * total_interaction_count
            // For team games, we approximate this by reducing score proportionally
            if (stats.Suicides > 0 && (stats.Kills + stats.Deaths) > 0)
            {
                double suicideRatio = (double)stats.Suicides / (stats.Kills + stats.Deaths);
                double suicidePenalty = Math.Ceiling(suicideRatio * stats.Score);
                score -= suicidePenalty;
            }

            // Ensure score doesn't go negative
            return Math.Max(0, score);
        }

        /// <summary>
        /// Gets current ELO rating for specified game mode.
        /// </summary>
        private double GetCurrentEloRating(UT2004PlayerProfile profile, UT2004GameMode gameMode)
        {
            return gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagElo.Rating,
                UT2004GameMode.iBR => profile.BombingRunElo.Rating,
                UT2004GameMode.TAM => profile.TAMElo.Rating,
                _ => 0.0
            };
        }

        /// <summary>
        /// Sets ELO rating for specified game mode.
        /// </summary>
        private void SetEloRating(UT2004PlayerProfile profile, UT2004GameMode gameMode, double newRating, double change)
        {
            var eloRating = gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagElo,
                UT2004GameMode.iBR => profile.BombingRunElo,
                UT2004GameMode.TAM => profile.TAMElo,
                _ => null
            };

            eloRating?.UpdateRating(newRating, change);
        }

        /// <summary>
        /// Gets total play time across all game modes (in seconds).
        /// </summary>
        private int GetTotalPlayTime(UT2004PlayerProfile profile)
        {
            // Note: You may need to add tracking for time played per match
            // For now, we'll use match count as a proxy
            return profile.TotalMatches * 600; // Assume 10 min average per match
        }

        /// <summary>
        /// Gets total matches played for a specific game mode.
        /// </summary>
        private int GetTotalMatches(UT2004PlayerProfile profile, UT2004GameMode gameMode)
        {
            return gameMode switch
            {
                UT2004GameMode.iCTF => profile.TotalCTFMatches,
                UT2004GameMode.iBR => profile.TotalBRMatches,
                UT2004GameMode.TAM => profile.TotalTAMMatches,
                _ => 0
            };
        }
    }
}