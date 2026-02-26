using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;
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
    public class UTStatsDBEloRatingService
    {
        public bool RankBots { get; set; } = false;
        public int MinRankTime { get; set; } = 60;
        public int MinRankMatches { get; set; } = 5;
        public int SkippedYoungPlayers { get; set; } = 0;
        public double TeamGameMultiplier { get; set; } = 8.0;
        private const double MaxTimeScaleRatio = 1.0;
        private const double MinPlayerTimeForScaling = 60.0;
        public double MaxSingleMatchRatingChange { get; set; } = 100.0;
        private const int KFactor = 16;

        // Debugging helpers
        public bool VerboseLogging { get; set; } = false;
        public string? VerbosePlayerGuid { get; set; } = "f65f3f7e0496815de17a4713604e5016";

        // Game Mode weighters for objective-specific score adjustments (CTF/BR/TAM)
        private readonly Dictionary<UT2004GameMode, IUTGameModeWeight> _weighters;

        public UTStatsDBEloRatingService(bool rankBots = false, int minRankTime = 60, int minRankMatches = 5)
        {
            RankBots = rankBots;
            MinRankTime = minRankTime;
            MinRankMatches = minRankMatches;

            // Defaults - replace or extend by injecting your own IGameModeScoreWeighter if desired
            _weighters = new Dictionary<UT2004GameMode, IUTGameModeWeight>
            {
                [UT2004GameMode.iCTF] = new CTFScoreWeighter(),
                [UT2004GameMode.TAM] = new TAMScoreWeighter(),
                [UT2004GameMode.iBR]  = new BRScoreWeighter()
            };
        }

        private IUTGameModeWeight GetWeighter(UT2004GameMode mode)
            => _weighters.TryGetValue(mode, out var w) ? w : _weighters[UT2004GameMode.iCTF];

        // RankCalc implements UTStatsDB core: change = round(KFactor * (actual - expected), 8)
        private double RankCalc(double rank1, double rank2, double score1, double score2)
        {
            double mscore1 = Math.Max(0.0, score1);
            double mscore2 = Math.Max(0.0, score2);

            double calc = 1.0 + Math.Pow(10.0, (-(rank1 - rank2) / 400.0));
            double dif = calc == 0.0 ? 1.0 : 1.0 / calc;

            double basePerf = (mscore1 + mscore2) == 0.0 ? 0.5 : mscore1 / (mscore1 + mscore2);

            return Math.Round(KFactor * (basePerf - dif), 8);
        }

        private void PrintDebug(UT2004StatLog match, List<UTPlayerMatchStats> playerStats)
        {
            foreach (var stat in  playerStats)
            {
                if (stat.FlagReturns > 100)
                {
                    Console.WriteLine($"[DEBUG] Player {stat.LastKnownName} ({stat.Guid}) has unusually high FlagReturns: {stat.FlagReturns} on {match.MatchDate}");
                }
                if (stat.Deaths > 200)
                {
                    Console.WriteLine($"[DEBUG] Player {stat.LastKnownName} ({stat.Guid}) has unusually high Deaths: {stat.Deaths} on {match.MatchDate}");
                }
            }
        }

        /// <summary>
        /// Updates ELO ratings for a team match (CTF/BR/TAM).
        /// Uses opponent-team averages per documentation mode with conservative safety guards.
        /// If a per-opponent kill matrix is present on the match, use pairwise UTStatsDB logic.
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

            // Debug
            PrintDebug(match, match.Players.SelectMany(team => team).ToList());

            // If a kill-matrix is present, use pairwise UTStatsDB matching (closest parity with original)
            if (match.KillMatch != null && match.KillMatch.Count > 0)
            {
                // Accumulate per-player rating changes (like $player[..]->rankc in PHP)
                var pendingChanges = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                // Helper to ensure key exists
                void EnsurePlayerPending(string guid) {
                    if (!pendingChanges.ContainsKey(guid)) pendingChanges[guid] = 0.0;
                }

                // For each opposing pair across teams
                foreach (var p in team0)
                {
                    if (string.IsNullOrEmpty(p.Guid) || !profiles.ContainsKey(p.Guid)) continue;
                    foreach (var q in team1)
                    {
                        if (string.IsNullOrEmpty(q.Guid) || !profiles.ContainsKey(q.Guid)) continue;

                        // Basic young/short playtime gating mirroring PHP checks
                        if ((p.TotalTimeSeconds + q.TotalTimeSeconds) <= 20) // both must have > 10 each in original, use combined conservative check
                            continue;

                        // Get pairwise raw kill counts from matrix
                        int score_pq = 0;
                        if (match.KillMatch.TryGetValue(p.Guid, out var innerP) && innerP.TryGetValue(q.Guid, out var v1))
                            score_pq = v1;
                        int score_qp = 0;
                        if (match.KillMatch.TryGetValue(q.Guid, out var innerQ) && innerQ.TryGetValue(p.Guid, out var v2))
                            score_qp = v2;

                        // If no opposing contact, skip
                        if (score_pq == 0 && score_qp == 0) continue;

                        // Build adjusted scores with spree & multi bonuses (UTStatsDB rules)
                        double spreeContributionP = 0.0;
                        double multiContributionP = 0.0;
                        double adjustedScoreP = score_pq;
                        if (p.SpreeCounts != null)
                        {
                            for (int i = 0; i < p.SpreeCounts.Length && i < 6; i++)
                            {
                                var c = p.SpreeCounts[i] * (i + 1);
                                spreeContributionP += c;
                                adjustedScoreP += c;
                            }
                        }
                        if (p.MultiCounts != null)
                        {
                            for (int i = 0; i < p.MultiCounts.Length && i < 7; i++)
                            {
                                var c = p.MultiCounts[i] * (i + 1);
                                multiContributionP += c;
                                adjustedScoreP += c;
                            }
                        }

                        double spreeContributionQ = 0.0;
                        double multiContributionQ = 0.0;
                        double adjustedScoreQ = score_qp;
                        if (q.SpreeCounts != null)
                        {
                            for (int i = 0; i < q.SpreeCounts.Length && i < 6; i++)
                            {
                                var c = q.SpreeCounts[i] * (i + 1);
                                spreeContributionQ += c;
                                adjustedScoreQ += c;
                            }
                        }
                        if (q.MultiCounts != null)
                        {
                            for (int i = 0; i < q.MultiCounts.Length && i < 7; i++)
                            {
                                var c = q.MultiCounts[i] * (i + 1);
                                multiContributionQ += c;
                                adjustedScoreQ += c;
                            }
                        }

                        // Apply objective-specific weights (CTF/BR/TAM) - pairwise we can consider opponent context
                        var weighter = GetWeighter(match.GameMode);
                        double objectiveAddP = weighter.ApplyObjectiveWeights(p, adjustedScoreP, q);
                        double objectiveAddQ = weighter.ApplyObjectiveWeights(q, adjustedScoreQ, p);
                        adjustedScoreP += objectiveAddP;
                        adjustedScoreQ += objectiveAddQ;

                        // Suicide adjustment (as described by UTStatsDB): suicr = suicides / (kills+deaths)
                        double suicideDeductP = 0.0;
                        if (p.Suicides > 0)
                        {
                            int skirm = p.Kills + p.Deaths;
                            if (skirm > 0)
                            {
                                double suicr = (double)p.Suicides / skirm;
                                double as1 = Math.Ceiling(suicr * (adjustedScoreP + adjustedScoreQ));
                                if (as1 < 1) as1 = 1;
                                suicideDeductP = as1;
                                adjustedScoreP -= as1;
                            }
                        }
                        double suicideDeductQ = 0.0;
                        if (q.Suicides > 0)
                        {
                            int skirm = q.Kills + q.Deaths;
                            if (skirm > 0)
                            {
                                double suicr = (double)q.Suicides / skirm;
                                double as1 = Math.Ceiling(suicr * (adjustedScoreP + adjustedScoreQ));
                                if (as1 < 1) as1 = 1;
                                suicideDeductQ = as1;
                                adjustedScoreQ -= as1;
                            }
                        }

                        // Clamp opponent averages minimal semantics retained at later stage when needed
                        adjustedScoreP = Math.Max(0.0, adjustedScoreP);
                        adjustedScoreQ = Math.Max(0.0, adjustedScoreQ);

                        // Get current ranks
                        double rankP = GetCurrentEloRating(profiles[p.Guid], match.GameMode);
                        double rankQ = GetCurrentEloRating(profiles[q.Guid], match.GameMode);

                        // Compute rank change for this pair (no team multiplier here; UTStatsDB used KFactor inside and *1 pairwise)
                        double rc = RankCalc(rankP, rankQ, adjustedScoreP, adjustedScoreQ);

                        // Verbose per-pair breakdown
                        if (VerboseLogging && (VerbosePlayerGuid == null || VerbosePlayerGuid == p.Guid || VerbosePlayerGuid == q.Guid))
                        {
                            Console.WriteLine($"[ELO TRACE - PAIRWISE] MatchDate={match.MatchDate:yyyy-MM-dd} Mode={match.GameMode}");
                            Console.WriteLine($"  Pair: {p.LastKnownName} ({p.Guid}) vs {q.LastKnownName} ({q.Guid})");
                            Console.WriteLine($"  Ranks: {rankP:F4} vs {rankQ:F4}");
                            Console.WriteLine($"  RawKills: {score_pq} vs {score_qp}");
                            Console.WriteLine($"  Spree(+): {spreeContributionP:F2}  Multi(+): {multiContributionP:F2}  Objective(+): {objectiveAddP:F2}  Suicide(-): {suicideDeductP:F2}");
                            Console.WriteLine($"  => AdjustedScores: {adjustedScoreP:F2} vs {adjustedScoreQ:F2}");
                            Console.WriteLine($"  RankCalc(before*K): {rc:F6} (K={KFactor})");
                        }

                        // Apply gating/unbalanced checks same as original:
                        // For p
                        EnsurePlayerPending(p.Guid);
                        EnsurePlayerPending(q.Guid);
                        if (rc > 0.0 || adjustedScoreQ > 0.0)
                        {
                            if (rc < 0.0 || rankP < rankQ + 250.0 || rankP < rankQ * 8.0)
                                pendingChanges[p.Guid] += rc;
                        }
                        if (rc < 0.0 || adjustedScoreP > 0.0)
                        {
                            if (rc > 0.0 || rankQ < rankP + 250.0 || rankQ < rankP * 8.0)
                                pendingChanges[q.Guid] -= rc;
                        }
                    }
                }

                // After all pairs processed, apply accumulated changes with conservative caps & floor at 0
                foreach (var kv in pendingChanges)
                {
                    var guid = kv.Key;
                    var totalChange = kv.Value;

                    if (!profiles.TryGetValue(guid, out var profile)) continue;

                    double currentRank = GetCurrentEloRating(profile, match.GameMode);

                    // Clamp per-match rating change
                    bool clamped = false;
                    double preClamp = totalChange;
                    if (!double.IsNaN(MaxSingleMatchRatingChange) && MaxSingleMatchRatingChange > 0.0)
                    {
                        totalChange = Math.Max(-MaxSingleMatchRatingChange, Math.Min(MaxSingleMatchRatingChange, totalChange));
                        clamped = Math.Abs(totalChange - preClamp) > double.Epsilon;
                    }

                    // Floor at 0
                    if (totalChange < 0.0 && (currentRank + totalChange) < 0.0) totalChange = -currentRank;

                    // Verbose trace for players we touch (aggregate)
                    if (VerboseLogging && (VerbosePlayerGuid == null || VerbosePlayerGuid == guid))
                    {
                        Console.WriteLine($"[ELO TRACE - PAIRWISE APPLY] GUID={guid} Mode={match.GameMode} CurrRank={currentRank:F4}");
                        Console.WriteLine($"  AggregateChange(beforeClamp)={preClamp:F6} afterClamp={totalChange:F6} NewRank={Math.Max(0.0, currentRank + totalChange):F6} Clamped={clamped}");
                    }

                    // Apply
                    SetEloRating(profile, match.GameMode, currentRank + totalChange, totalChange);

                    // Update peak if applicable
                    if (MinRankMatches <= 0 || GetTotalMatches(profile, match.GameMode) >= MinRankMatches)
                    {
                        var elo = match.GameMode switch
                        {
                            UT2004GameMode.iCTF => profile.CaptureTheFlagElo,
                            UT2004GameMode.iBR  => profile.BombingRunElo,
                            UT2004GameMode.TAM  => profile.TAMElo,
                            _                   => null
                        };
                        elo?.UpdatePeak(match.MatchDate);
                    }
                }

                return;
            }

            // Fallback: team-average approach (existing conservative behavior)
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

                // Skip trivial/non-event matches (conservative); prevents RankCalc explosion on degenerate logs
                if (rawAvgScore0 <= 5.0 && p.Score <= 5.0)
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

            // Apply UTStatsDB-style spree & multi bonuses (spree[0]*1, spree[1]*2, ..., multi[0]*1, ...)
            double spreeContribution = 0.0;
            double multiContribution = 0.0;
            double adjustedPlayerScore = playerScore;

            if (playerStats.SpreeCounts != null)
            {
                // SpreeCounts length is expected 6; add weights 1..6
                for (int i = 0; i < playerStats.SpreeCounts.Length && i < 6; i++)
                {
                    var c = playerStats.SpreeCounts[i] * (i + 1);
                    spreeContribution += c;
                    adjustedPlayerScore += c;
                }
            }

            if (playerStats.MultiCounts != null)
            {
                // MultiCounts length is expected 7; add weights 1..7
                for (int i = 0; i < playerStats.MultiCounts.Length && i < 7; i++)
                {
                    var c = playerStats.MultiCounts[i] * (i + 1);
                    multiContribution += c;
                    adjustedPlayerScore += c;
                }
            }

            // Apply objective weights per game mode (CTF/BR/TAM)
            var weighter = GetWeighter(gameMode);
            double objectiveContribution = weighter.ApplyObjectiveWeights(playerStats, adjustedPlayerScore, null);
            adjustedPlayerScore += objectiveContribution;

            // Ensure opponentAvgScore respects the conservative minimum
            opponentAvgScore = Math.Max(1.0, opponentAvgScore);

            // Suicide adjustment: approximate suicr = suicides / (kills + deaths), then subtract ceil(suicr * (score + oppScore))
            double suicideDeduct = 0.0;
            if (playerStats.Suicides > 0)
            {
                int skirm = playerStats.Kills + playerStats.Deaths;
                if (skirm > 0)
                {
                    double suicr = (double)playerStats.Suicides / skirm;
                    double as1 = Math.Ceiling(suicr * (adjustedPlayerScore + opponentAvgScore));
                    suicideDeduct = as1;
                    adjustedPlayerScore -= as1;
                }
            }

            double rcBefore = RankCalc(currentRank, opponentAvgRank, adjustedPlayerScore, opponentAvgScore);
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
            double preClampRc = rc;
            if (!double.IsNaN(MaxSingleMatchRatingChange) && MaxSingleMatchRatingChange > 0.0)
            {
                rc = Math.Max(-MaxSingleMatchRatingChange, Math.Min(MaxSingleMatchRatingChange, rc));
            }
            bool wasClamped = Math.Abs(rc - preClampRc) > double.Epsilon;

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

            // Verbose trace (filtered) - extended breakdown
            if (VerboseLogging && (VerbosePlayerGuid == null || VerbosePlayerGuid == profile.Guid))
            {
                Console.WriteLine($"[ELO TRACE] MatchDate={matchDate:yyyy-MM-dd} GUID={profile.Guid} Name={profile.CurrentName}");
                Console.WriteLine($"  Mode={gameMode} CurrRank={currentRank:F4}");
                Console.WriteLine($"  RawScore={playerScore:F2} Spree+={spreeContribution:F2} Multi+={multiContribution:F2} Objective+={objectiveContribution:F2} Suicide-={suicideDeduct:F2}");
                Console.WriteLine($"  AdjustedScore={adjustedPlayerScore:F2} OppAvgScore={opponentAvgScore:F2} OppAvgRank={opponentAvgRank:F4}");
                Console.WriteLine($"  RankCalc(before*K)={rcBefore:F6} TeamK={TeamGameMultiplier} rcRaw={rcBefore * TeamGameMultiplier:F6}");
                Console.WriteLine($"  AppliedScale={appliedScale:F4} rcAfterScale={preClampRc:F6} rcAfterClamp={rc:F6} Clamped={wasClamped}");
                Console.WriteLine($"  FinalChange={rc:F6} NewRank={Math.Max(0.0, currentRank + rc):F6}");
            }

            // Apply rating change
            SetEloRating(profile, gameMode, currentRank + rc, rc);

            // Update peak if player meets configured minimums. ProcessPlayerRankChange already enforced
            // MinRankMatches/MinRankTime above, so this is safe and keeps peak-update logic local.
            // If you want peaks only after a minimum number of matches per-mode, that is respected by GetTotalMatches.
            if (MinRankMatches <= 0 || GetTotalMatches(profile, gameMode) >= MinRankMatches)
            {
                var elo = gameMode switch
                {
                    UT2004GameMode.iCTF => profile.CaptureTheFlagElo,
                    UT2004GameMode.iBR  => profile.BombingRunElo,
                    UT2004GameMode.TAM  => profile.TAMElo,
                    _                   => null
                };
                elo?.UpdatePeak(matchDate);
            }
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