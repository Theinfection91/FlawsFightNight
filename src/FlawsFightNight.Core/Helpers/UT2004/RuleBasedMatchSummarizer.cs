using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class RuleBasedMatchSummarizer : IMatchSummarizer
    {
        public string Summarize(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles, Dictionary<string, double>? eloChanges = null)
        {
            if (match == null) return string.Empty;

            var players = match.Players?.SelectMany(t => t).Where(p => p != null).ToList() ?? new List<UTPlayerMatchStats>();

            if (players.Count == 0)
            {
                const string empty = "No players recorded.\n";
                match.MatchSummary = empty;
                return empty;
            }

            // Build name resolvers
            string ResolveName(UTPlayerMatchStats p)
            {
                if (!string.IsNullOrWhiteSpace(p.LastKnownName))
                    return p.LastKnownName;
                if (p.Guid != null && profiles.TryGetValue(p.Guid, out var prof) && !string.IsNullOrWhiteSpace(prof.CurrentName))
                    return prof.CurrentName;
                return "Unknown";
            }

            string ResolveGuid(string? guid)
            {
                if (guid == null) return "Unknown";
                if (profiles.TryGetValue(guid, out var prof) && !string.IsNullOrWhiteSpace(prof.CurrentName))
                    return prof.CurrentName;
                var found = players.FirstOrDefault(p => p.Guid == guid);
                return found != null ? ResolveName(found) : "Unknown";
            }

            var ctx = new SummaryContext
            {
                Match = match,
                Players = players,
                Timeline = match.Timeline ?? new List<MatchEvent>(),
                Profiles = profiles,
                EloChanges = eloChanges,
                MapName = match.MapName,
                Name = ResolveName,
                NameByGuid = ResolveGuid
            };

            ComputeAnalytics(ctx);

            var sb = new StringBuilder();
            WriteMatchOverview(sb, ctx);
            WriteTeamStandings(sb, ctx);
            WritePlayerPerformanceTable(sb, ctx);
            WriteKeyMoments(sb, ctx);
            WriteAwards(sb, ctx);
            WriteRivalries(sb, ctx);
            WriteWeaponDominance(sb, ctx);
            WriteGameModeAnalysis(sb, ctx);
            WriteMomentumAnalysis(sb, ctx);
            WriteCombatRoles(sb, ctx);
            WritePerformanceTiers(sb, ctx);
            WriteEloStandings(sb, ctx);

            var result = sb.ToString();
            match.MatchSummary = result;
            return result;
        }

        // ── Internal data structures ─────────────────────────────────────────────

        private sealed class TeamInfo
        {
            public int Index { get; set; }
            public string Label { get; set; } = "";
            public List<UTPlayerMatchStats> Players { get; set; } = new();
            public int ActualTeamScore { get; set; } // <-- Add this
            public int TotalScore { get; set; }
            public int TotalKills { get; set; }
            public int TotalDeaths { get; set; }
            public int TotalCaps { get; set; }
            public bool IsWinner { get; set; }
        }

        private sealed class SummaryContext
        {
            // Inputs
            public UT2004StatLog Match { get; set; } = null!;
            public List<UTPlayerMatchStats> Players { get; set; } = new();
            public List<MatchEvent> Timeline { get; set; } = new();
            public Dictionary<string, UT2004PlayerProfile> Profiles { get; set; } = new();
            public Dictionary<string, double>? EloChanges { get; set; }
            public string MapName { get; set; } = "";
            public Func<UTPlayerMatchStats, string> Name { get; set; } = _ => "Unknown";
            public Func<string?, string> NameByGuid { get; set; } = _ => "Unknown";

            // Computed – match level
            public List<TeamInfo> Teams { get; set; } = new();
            public int PlayerCount { get; set; }
            public int TotalKills { get; set; }
            public int TotalDeaths { get; set; }
            public int TotalHeadshots { get; set; }
            public double DurationMinutes { get; set; }
            public double KillsPerMinute { get; set; }
            public double KillsPerMinutePerPlayer { get; set; }
            public double HeadshotRate { get; set; }
            public double BaselineMatchTimeSeconds { get; set; }

            // Score analysis
            public int ScoreDiff { get; set; }
            public double ScoreRatio { get; set; }
            public bool IsCloseGame { get; set; }
            public bool IsBlowout { get; set; }
            public bool IsDraw { get; set; }

            // Timeline-derived
            public MatchEvent? FirstBlood { get; set; }
            public List<MatchEvent> KillEvents { get; set; } = new();
            public List<MatchEvent> SpreeEvents { get; set; } = new();
            public List<MatchEvent> MultiKillEvents { get; set; } = new();

            // Player rankings
            public UTPlayerMatchStats? MatchMVP { get; set; }
            public UTPlayerMatchStats? TopKiller { get; set; }
            public UTPlayerMatchStats? TopKD { get; set; }
            public UTPlayerMatchStats? TopObjective { get; set; }
            public UTPlayerMatchStats? TopAccuracy { get; set; }
            public UTPlayerMatchStats? TopHeadshots { get; set; }
            public UTPlayerMatchStats? TopDamage { get; set; }
            public UTPlayerMatchStats? MostDeaths { get; set; }
            public UTPlayerMatchStats? BestStreak { get; set; }

            // Match tone descriptors
            public string PaceWord { get; set; } = "";
            public string IntensityWord { get; set; } = "";
            public string MarginWord { get; set; } = "";
            public string NarrativePhrase { get; set; } = "";
        }

        // ── Analytics computation ────────────────────────────────────────────────

        private static void ComputeAnalytics(SummaryContext ctx)
        {
            var players = ctx.Players;
            var timeline = ctx.Timeline;
            var match = ctx.Match;

            ctx.PlayerCount = players.Count;
            ctx.TotalKills = players.Sum(p => p.Kills);
            ctx.TotalDeaths = players.Sum(p => p.Deaths);
            ctx.TotalHeadshots = players.Sum(p => p.Headshots);

            // Duration – prefer stored value, fall back to last timeline event
            ctx.DurationMinutes = match.MatchDurationSeconds > 0
                ? match.MatchDurationSeconds / 60.0
                : timeline.Count > 0
                    ? Math.Max(timeline.Max(e => e.GameTimeSeconds) / 60.0, 0.5)
                    : 1.0;
            if (ctx.DurationMinutes < 0.5) ctx.DurationMinutes = 0.5;

            // Activity & Uptime baseline 
            double maxExpectedTime = Math.Max(match.MatchDurationSeconds, timeline.Count > 0 ? timeline.Max(e => e.GameTimeSeconds) : 0);
            double highestPlayerTime = players.Count > 0 ? players.Max(p => p.TotalTimeSeconds) : 0;
            // Use the absolute highest observable time length as the anchor for 100% activity
            ctx.BaselineMatchTimeSeconds = Math.Max(maxExpectedTime, highestPlayerTime);
            if (ctx.BaselineMatchTimeSeconds <= 0) ctx.BaselineMatchTimeSeconds = 1;

            ctx.KillsPerMinute = ctx.TotalKills / ctx.DurationMinutes;
            ctx.KillsPerMinutePerPlayer = ctx.PlayerCount > 0 ? ctx.KillsPerMinute / ctx.PlayerCount : 0;
            ctx.HeadshotRate = ctx.TotalKills > 0 ? (double)ctx.TotalHeadshots / ctx.TotalKills * 100.0 : 0;

            // Teams
            for (int i = 0; i < match.Players.Count; i++)
            {
                var teamPlayers = match.Players[i].Where(p => p != null).ToList();
                int teamIndex = teamPlayers.FirstOrDefault()?.Team ?? i;
                int actualScore = match.TeamScores?.TryGetValue(teamIndex, out var s) == true ? s : 0;

                ctx.Teams.Add(new TeamInfo
                {
                    Index = teamIndex,
                    Label = teamIndex == 0 ? "Red Team" : teamIndex == 1 ? "Blue Team" : $"Team {teamIndex}",
                    Players = teamPlayers,
                    ActualTeamScore = actualScore,
                    TotalScore = teamPlayers.Sum(p => p.Score),
                    TotalKills = teamPlayers.Sum(p => p.Kills),
                    TotalDeaths = teamPlayers.Sum(p => p.Deaths),
                    TotalCaps = teamPlayers.Sum(p => p.FlagCaptures + p.BallCaptures),
                    IsWinner = teamPlayers.Any(p => p.IsWinner)
                });
            }

            // Score analysis
            var winner = ctx.Teams.FirstOrDefault(t => t.IsWinner);
            var loser = ctx.Teams.FirstOrDefault(t => !t.IsWinner);

            if (winner != null && loser != null)
            {
                // We no longer need to calculate 'useObjectiveScore' here at all. 
                // We just rely directly on the game server's points.
                int winVal = winner.ActualTeamScore;
                int loseVal = loser.ActualTeamScore;
                ctx.ScoreDiff = Math.Abs(winVal - loseVal);
                ctx.ScoreRatio = loseVal > 0 ? (double)winVal / loseVal : (winVal > 0 ? 10.0 : 1.0);
            }

            ctx.IsDraw = !ctx.Teams.Any(t => t.IsWinner);
            ctx.IsCloseGame = !ctx.IsDraw && ctx.ScoreRatio < 1.35;
            ctx.IsBlowout = !ctx.IsDraw && ctx.ScoreRatio > 2.5;

            // Timeline events
            ctx.FirstBlood = timeline.FirstOrDefault(e => e.EventType == "FirstBlood");
            ctx.KillEvents = timeline.Where(e => e.EventType == "Kill").ToList();
            ctx.SpreeEvents = timeline.Where(e => e.EventType == "Spree").ToList();
            ctx.MultiKillEvents = timeline.Where(e => e.EventType == "MultiKill").ToList();

            // Player rankings
            ctx.MatchMVP = players.OrderByDescending(p => p.Score).First();
            ctx.TopKiller = players.OrderByDescending(p => p.Kills).First();
            ctx.MostDeaths = players.OrderByDescending(p => p.Deaths).First();
            ctx.BestStreak = players.OrderByDescending(p => p.BestKillStreak).First();

            ctx.TopKD = players.Where(p => p.Kills >= 3)
                .OrderByDescending(p => p.GetKillDeathRatio())
                .FirstOrDefault();

            ctx.TopHeadshots = players.Where(p => p.Headshots > 0)
                .OrderByDescending(p => p.Headshots)
                .FirstOrDefault();

            ctx.TopAccuracy = players
                .Where(p => p.WeaponStatistics.Count > 0 && p.WeaponStatistics.Values.Sum(w => w.ShotsFired) >= 15)
                .OrderByDescending(p => p.GetOverallAccuracy())
                .FirstOrDefault();

            if (match.GameMode is UT2004GameMode.iCTF or UT2004GameMode.iBR)
            {
                ctx.TopObjective = players
                    .OrderByDescending(p => p.FlagCaptures + p.BallCaptures + p.FlagReturns + p.FlagDenials + p.BallScoreAssists + p.BombPickups)
                    .FirstOrDefault();
            }

            if (match.GameMode == UT2004GameMode.TAM)
            {
                ctx.TopDamage = players.Where(p => p.TotalDamageDealt > 0)
                    .OrderByDescending(p => p.TotalDamageDealt)
                    .FirstOrDefault();
            }

            ClassifyMatchTone(ctx);
        }

        private static void ClassifyMatchTone(SummaryContext ctx)
        {
            // Pace – based on normalized kill rate per player
            ctx.PaceWord = ctx.KillsPerMinutePerPlayer switch
            {
                > 1.5 => "frenetic",
                > 1.0 => "fast-paced",
                > 0.6 => "well-paced",
                > 0.3 => "methodical",
                _ => "slow-burn"
            };

            // Intensity – sprees + multi-kills density
            double hypePerMin = (ctx.SpreeEvents.Count + ctx.MultiKillEvents.Count) / Math.Max(ctx.DurationMinutes, 1);
            ctx.IntensityWord = hypePerMin switch
            {
                > 1.5 => "explosive",
                > 0.8 => "intense",
                > 0.5 => "dynamic",
                > 0.3 => "competitive",
                _ => "measured"
            };

            // Margin
            ctx.MarginWord = ctx.IsDraw ? "deadlocked"
                : ctx.IsBlowout ? "dominant"
                : ctx.IsCloseGame ? "razor-thin"
                : "hard-fought";

            // Narrative phrase
            if (ctx.IsDraw)
                ctx.NarrativePhrase = "an evenly matched stalemate where neither team could pull ahead";
            else if (ctx.IsCloseGame && ctx.SpreeEvents.Count >= 4)
                ctx.NarrativePhrase = "a white-knuckle thriller decided in the closing moments";
            else if (ctx.IsCloseGame)
                ctx.NarrativePhrase = "a tightly contested battle that could have gone either way";
            else if (ctx.IsBlowout && ctx.KillsPerMinutePerPlayer > 1.0)
                ctx.NarrativePhrase = "a high-octane demolition from start to finish";
            else if (ctx.IsBlowout)
                ctx.NarrativePhrase = "a one-sided affair with the winners in control throughout";
            else if (hypePerMin > 1.0)
                ctx.NarrativePhrase = "an action-packed slugfest full of highlight-reel plays";
            else
                ctx.NarrativePhrase = "a persistent and hard-fought contest with both teams trading blows";
        }

        // ── Section writers ──────────────────────────────────────────────────────

        private static void WriteMatchOverview(StringBuilder sb, SummaryContext ctx)
        {
            var match = ctx.Match;
            int mins = (int)ctx.DurationMinutes;
            int secs = (int)((ctx.DurationMinutes - mins) * 60);
            string duration = mins > 0 ? $"{mins}m {secs}s" : $"{secs}s";

            sb.AppendLine($"# {match.GameModeName} Match Summary — {ctx.MapName}");
            sb.AppendLine();
            sb.AppendLine($"A {ctx.PaceWord}, {ctx.IntensityWord} {match.GameModeName} match on **({ctx.Match.MapId ?? "N/A"} | {ctx.MapName ?? "Unknown"} by {ctx.Match.MapCreator ?? "Unknown"})** lasting **{duration}** with **{ctx.PlayerCount} players**.");

            // Winner announcement
            var winner = ctx.Teams.FirstOrDefault(t => t.IsWinner);
            var loser = ctx.Teams.FirstOrDefault(t => !t.IsWinner);
            if (winner != null && loser != null)
            {
                // Directly format using ActualTeamScore
                string scoreStr = $"{winner.ActualTeamScore}-{loser.ActualTeamScore}";
                sb.AppendLine($"**{winner.Label}** secured the victory **{scoreStr}** in {ctx.NarrativePhrase}.");
            }
            else if (ctx.IsDraw)
            {
                sb.AppendLine("Neither team could break the deadlock — the match ended in a **draw**.");
            }

            // Quick stat line
            sb.Append($"> {ctx.TotalKills} total kills ({ctx.KillsPerMinute:F1}/min)");
            sb.Append($" · {ctx.TotalHeadshots} headshots ({ctx.HeadshotRate:F0}%)");
            sb.Append($" · {ctx.SpreeEvents.Count} sprees");
            sb.AppendLine($" · {ctx.MultiKillEvents.Count} multi-kills");
            sb.AppendLine();
        }

        private static void WriteTeamStandings(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## Team Standings");

            foreach (var t in ctx.Teams)
            {
                string winTag = t.IsWinner ? " 🏆" : "";
                string scoreLabel = ctx.Match.GameMode switch
                {
                    UT2004GameMode.TAM => $"{t.ActualTeamScore} Rounds Won",
                    UT2004GameMode.iCTF => $"{t.ActualTeamScore} Caps",
                    UT2004GameMode.iBR => $"{t.ActualTeamScore} Pts",
                    _ => $"{t.ActualTeamScore} Score"
                };

                sb.AppendLine($"* **{t.Label}** (Team {t.Index}): {scoreLabel} — {t.TotalKills}K / {t.TotalDeaths}D{winTag}");
            }

            if (!ctx.IsDraw)
            {
                if (ctx.IsBlowout)
                    sb.AppendLine($"> ⚠️ **Blowout** — the winning team dominated with a {ctx.ScoreDiff}-point advantage.");
                else if (ctx.IsCloseGame)
                    sb.AppendLine($"> 🔥 **Nail-biter** — decided by a {ctx.MarginWord} margin of just {ctx.ScoreDiff}.");
            }
            sb.AppendLine();
        }

        private static void WritePlayerPerformanceTable(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## Player Performance");

            bool isTAM = ctx.Match.GameMode == UT2004GameMode.TAM;
            bool hasAccuracy = ctx.Players.Any(p => p.WeaponStatistics.Count > 0);

            foreach (var p in ctx.Players.OrderByDescending(p => p.Score))
            {
                string name = ctx.Name(p);
                string team = TeamLabel(p.Team);
                string kd = p.GetKillDeathRatio().ToString("F2");
                string acc = hasAccuracy && p.GetOverallAccuracy() > 0 ? $" · Acc {p.GetOverallAccuracy():F1}%" : "";
                double actPct = Math.Clamp((p.TotalTimeSeconds / ctx.BaselineMatchTimeSeconds) * 100.0, 0, 100);
                string act = p.TotalTimeSeconds > 0 ? $"{actPct:F0}%" : "-";
                string extras = "";
                if (p.BestKillStreak >= 5) extras += $" · Str {p.BestKillStreak}k";
                if (p.BestMultiKill >= 2) extras += $" · Mlt x{p.BestMultiKill}";

                if (isTAM)
                {
                    sb.AppendLine($"**{name}** ({team}) · {p.Score}pts · {p.Kills}K/{p.Deaths}D ({kd}) · {p.TotalDamageDealt:N0} dmg · {p.RoundEndingKills} REK{acc}{extras} · Act {act}");
                }
                else
                {
                    int caps = p.FlagCaptures + p.BallCaptures;
                    sb.AppendLine($"**{name}** ({team}) · {p.Score}pts · {p.Kills}K/{p.Deaths}D ({kd}) · {caps} cap · {p.FlagReturns}/{p.FlagDenials} ret/den{acc}{extras} · Act {act}");
                }
            }
            sb.AppendLine();
        }

        private static void WriteKeyMoments(StringBuilder sb, SummaryContext ctx)
        {
            var moments = new List<(double Time, string Icon, string Text, int Priority)>();

            // First blood
            if (ctx.FirstBlood != null)
            {
                string actor = ctx.FirstBlood.ActorName ?? ctx.NameByGuid(ctx.FirstBlood.ActorGuid);
                double t = ctx.FirstBlood.GameTimeSeconds;
                string speed = t < 10 ? "blistering" : t < 20 ? "lightning-fast" : t < 45 ? "early" : "opening";
                moments.Add((t, "🩸", $"**{actor}** draws {speed} first blood at {FmtTime(t)}", 5));
            }

            // High-tier sprees (Dominating+)
            foreach (var e in ctx.SpreeEvents.Where(e => e.Detail is "Dominating" or "Unstoppable" or "Godlike" or "Wicked Sick"))
            {
                string actor = e.ActorName ?? ctx.NameByGuid(e.ActorGuid);
                int pri = e.Detail is "Godlike" or "Wicked Sick" ? 9 : 7;
                moments.Add((e.GameTimeSeconds, "🔥", $"**{actor}** goes on a **{e.Detail}** streak at {FmtTime(e.GameTimeSeconds)}", pri));
            }

            // High-tier multi-kills (Ultra Kill+)
            foreach (var e in ctx.MultiKillEvents.Where(e => e.Detail is "Ultra Kill" or "Monster Kill" or "Ludicrous Kill" or "Holy Shit"))
            {
                string actor = e.ActorName ?? ctx.NameByGuid(e.ActorGuid);
                int pri = e.Detail is "Ludicrous Kill" or "Holy Shit" ? 9 : 8; // Prioritize the highest multi-kills
                moments.Add((e.GameTimeSeconds, "💥", $"**{actor}** lands a **{e.Detail}** at {FmtTime(e.GameTimeSeconds)}", pri));
            }

            // Flag / bomb captures
            var capEvents = ctx.Timeline.Where(e => e.EventType is "FlagCapture" or "BombCapture" or "BombThrown").ToList();
            if (capEvents.Count > 0)
            {
                var first = capEvents.First();
                string firstActor = first.ActorName ?? ctx.NameByGuid(first.ActorGuid);

                string firstAction = first.EventType == "BombThrown" ? "throws the first goal (3pts)"
                    : first.EventType == "BombCapture" ? "secures the first run-in cap (7pts)"
                    : "secures the first capture";

                moments.Add((first.GameTimeSeconds, "🚩", $"**{firstActor}** {firstAction} at {FmtTime(first.GameTimeSeconds)}", 7));

                if (capEvents.Count > 1)
                {
                    var last = capEvents.Last();
                    string lastActor = last.ActorName ?? ctx.NameByGuid(last.ActorGuid);

                    string lastAction = last.EventType == "BombThrown" ? "throws the decisive final goal"
                        : last.EventType == "BombCapture" ? "runs in the decisive final ball"
                        : "delivers the decisive final capture";

                    // Force to top priority so it is never pushed off the list
                    moments.Add((last.GameTimeSeconds, "🏆", $"**{lastActor}** {lastAction} at {FmtTime(last.GameTimeSeconds)}", 10));
                }
            }

            // TAM round wins
            if (ctx.Match.GameMode == UT2004GameMode.TAM)
            {
                var roundWins = ctx.Timeline.Where(e => e.EventType == "RoundWin").ToList();
                if (roundWins.Count > 0)
                {
                    var last = roundWins.Last();
                    string actor = last.ActorName ?? ctx.NameByGuid(last.ActorGuid);
                    string detail = last.Detail != null ? $" ({last.Detail})" : "";
                    moments.Add((last.GameTimeSeconds, "⚔️", $"**{actor}** wins the decisive final round at {FmtTime(last.GameTimeSeconds)}{detail}", 7));
                }
            }

            // Clutch flag returns that saved a cap (return events near cap-attempt events)
            var returnEvents = ctx.Timeline.Where(e => e.EventType == "FlagReturn").ToList();
            foreach (var ret in returnEvents)
            {
                // Check if there was an enemy flag grab within 15 seconds before the return
                bool wasDangerous = ctx.Timeline.Any(e =>
                    e.EventType == "FlagGrab" &&
                    e.GameTimeSeconds >= ret.GameTimeSeconds - 15 &&
                    e.GameTimeSeconds <= ret.GameTimeSeconds &&
                    e.ActorGuid != ret.ActorGuid);

                if (wasDangerous)
                {
                    string actor = ret.ActorName ?? ctx.NameByGuid(ret.ActorGuid);
                    moments.Add((ret.GameTimeSeconds, "🛡️", $"**{actor}** makes a clutch flag save at {FmtTime(ret.GameTimeSeconds)}", 6));
                }
            }

            if (moments.Count == 0) return;

            sb.AppendLine("## Key Moments");
            // Sort by priority descending, then chronologically, take top 8
            foreach (var m in moments.OrderByDescending(m => m.Priority).ThenBy(m => m.Time).Take(8))
            {
                sb.AppendLine($"* {m.Icon} {m.Text}");
            }
            sb.AppendLine();
        }

        private static void WriteAwards(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## Awards");
            var awards = new List<(string Title, string Player, string Team, string Reason)>();

            // Match MVP
            if (ctx.MatchMVP != null)
            {
                string t = TeamLabel(ctx.MatchMVP.Team);
                awards.Add(("🌟 Match MVP", ctx.Name(ctx.MatchMVP), t, $"Highest overall score ({ctx.MatchMVP.Score} pts)"));
            }

            // Kill Leader
            if (ctx.TopKiller != null && ctx.TopKiller != ctx.MatchMVP)
            {
                string t = TeamLabel(ctx.TopKiller.Team);
                double kpm = ctx.DurationMinutes > 0 ? ctx.TopKiller.Kills / ctx.DurationMinutes : 0;
                string extra = kpm > 2 ? $" ({kpm:F1} kills/min)" : "";
                awards.Add(("🗡️ Kill Leader", ctx.Name(ctx.TopKiller), t, $"{ctx.TopKiller.Kills} kills{extra}"));
            }

            // Efficiency King (best K/D, different from kill leader, min kills)
            if (ctx.TopKD != null && ctx.TopKD != ctx.TopKiller && ctx.TopKD != ctx.MatchMVP)
            {
                string t = TeamLabel(ctx.TopKD.Team);
                awards.Add(("🎯 Efficiency King", ctx.Name(ctx.TopKD), t, $"{ctx.TopKD.GetKillDeathRatio():F2} K/D ({ctx.TopKD.Kills}K / {ctx.TopKD.Deaths}D)"));
            }

            // Objective MVP
            if (ctx.TopObjective != null && (ctx.TopObjective.FlagCaptures + ctx.TopObjective.BallCaptures + ctx.TopObjective.FlagReturns + ctx.TopObjective.BombPickups) > 0)
            {
                string t = TeamLabel(ctx.TopObjective.Team);
                string detail = ctx.Match.GameMode == UT2004GameMode.iCTF
                    ? $"{ctx.TopObjective.FlagCaptures} caps, {ctx.TopObjective.FlagReturns} returns, {ctx.TopObjective.FlagDenials} denials"
                    : $"{ctx.TopObjective.BallCaptures} caps, {ctx.TopObjective.BombPickups} ball pickups";
                awards.Add(("🏁 Objective MVP", ctx.Name(ctx.TopObjective), t, detail));
            }

            // Headshot Machine
            if (ctx.TopHeadshots != null && ctx.TopHeadshots.Headshots >= 3)
            {
                string t = TeamLabel(ctx.TopHeadshots.Team);
                double rate = ctx.TopHeadshots.Kills > 0 ? (double)ctx.TopHeadshots.Headshots / ctx.TopHeadshots.Kills * 100 : 0;
                awards.Add(("💀 Headshot Machine", ctx.Name(ctx.TopHeadshots), t, $"{ctx.TopHeadshots.Headshots} headshots ({rate:F0}% of kills)"));
            }

            // Streak Master
            if (ctx.BestStreak != null && ctx.BestStreak.BestKillStreak >= 5)
            {
                string t = TeamLabel(ctx.BestStreak.Team);
                awards.Add(("🔥 Streak Master", ctx.Name(ctx.BestStreak), t, $"{ctx.BestStreak.BestKillStreak}-kill streak ({SpreeTitle(ctx.BestStreak.BestKillStreak)})"));
            }

            // Sharpshooter
            if (ctx.TopAccuracy != null)
            {
                int shots = ctx.TopAccuracy.WeaponStatistics.Values.Sum(w => w.ShotsFired);
                string t = TeamLabel(ctx.TopAccuracy.Team);
                awards.Add(("🔬 Sharpshooter", ctx.Name(ctx.TopAccuracy), t, $"{ctx.TopAccuracy.GetOverallAccuracy():F1}% accuracy ({shots} shots)"));
            }

            // The Workhorse (highest activity time)
            var workhorse = ctx.Players.OrderByDescending(p => p.TotalTimeSeconds).FirstOrDefault();
            if (workhorse != null && ctx.BaselineMatchTimeSeconds > 60)
            {
                double actPct = (workhorse.TotalTimeSeconds / ctx.BaselineMatchTimeSeconds) * 100.0;
                if (actPct >= 90) // Substantial engagement required
                {
                    awards.Add(("🐎 The Workhorse", ctx.Name(workhorse), TeamLabel(workhorse.Team), $"Highest match uptime ({actPct:F0}% active combat time)"));
                }
            }

            // Average Lifespan calculation / The Survivor
            var survivor = ctx.Players.Where(p => p.TotalTimeSeconds > ctx.BaselineMatchTimeSeconds * 0.5 && p.Deaths > 0)
                                      .OrderByDescending(p => p.TotalTimeSeconds / (double)p.Deaths)
                                      .FirstOrDefault();

            if (survivor != null)
            {
                double avgDeathTime = survivor.TotalTimeSeconds / (double)survivor.Deaths;
                awards.Add(("🛡️ The Survivor", ctx.Name(survivor), TeamLabel(survivor.Team), $"Stayed alive longest per death on average ({avgDeathTime:F1}s lifespan)"));
            }

            // Damage King (TAM)
            if (ctx.TopDamage != null && ctx.Match.GameMode == UT2004GameMode.TAM)
            {
                string t = TeamLabel(ctx.TopDamage.Team);
                awards.Add(("💪 Damage King", ctx.Name(ctx.TopDamage), t, $"{ctx.TopDamage.TotalDamageDealt:N0} damage dealt"));
            }

            // Iron Wall (iCTF: top returns + denials)
            if (ctx.Match.GameMode == UT2004GameMode.iCTF)
            {
                var wall = ctx.Players.OrderByDescending(p => p.FlagReturns + p.FlagDenials).FirstOrDefault();
                if (wall != null && (wall.FlagReturns + wall.FlagDenials) >= 3 && wall != ctx.TopObjective)
                {
                    string t = TeamLabel(wall.Team);
                    awards.Add(("🧱 Iron Wall", ctx.Name(wall), t, $"{wall.FlagReturns} returns, {wall.FlagDenials} denials"));
                }
            }

            // First Blood
            if (ctx.FirstBlood != null)
            {
                string actor = ctx.FirstBlood.ActorName ?? ctx.NameByGuid(ctx.FirstBlood.ActorGuid);
                awards.Add(("🩸 First Blood", actor, "", $"at {FmtTime(ctx.FirstBlood.GameTimeSeconds)}"));
            }

            // Career-high check: did anyone set a new personal record?
            foreach (var p in ctx.Players)
            {
                if (p.Guid == null || !ctx.Profiles.TryGetValue(p.Guid, out var prof)) continue;
                if (p.Kills > 0 && p.Kills >= prof.MostKillsInMatch && prof.TotalMatches > 1)
                {
                    awards.Add(("📈 Career Game", ctx.Name(p), TeamLabel(p.Team), $"matched or set a new personal best of {p.Kills} kills"));
                    break; // Only highlight one to not flood awards
                }
            }

            foreach (var (title, player, team, reason) in awards)
            {
                string teamTag = !string.IsNullOrEmpty(team) ? $" ({team})" : "";
                sb.AppendLine($"* {title} **{player}**{teamTag} — {reason}");
            }
            sb.AppendLine();
        }

        private static void WriteRivalries(StringBuilder sb, SummaryContext ctx)
        {
            if (ctx.Match.KillMatch == null || ctx.Match.KillMatch.Count == 0) return;

            var killMatrix = ctx.Match.KillMatch;

            // Collect all directed kill pairs with threshold
            var pairs = new List<(string KillerGuid, string VictimGuid, int Kills)>();
            foreach (var (killerGuid, victims) in killMatrix)
            {
                foreach (var (victimGuid, count) in victims)
                {
                    if (count >= 2)
                        pairs.Add((killerGuid, victimGuid, count));
                }
            }
            if (pairs.Count == 0) return;

            // Build mutual rivalry entries (de-duplicate A→B and B→A)
            var processed = new HashSet<string>();
            var rivalries = new List<(string Guid1, string Guid2, int Kills1, int Kills2)>();

            foreach (var d in pairs)
            {
                string key = string.Compare(d.KillerGuid, d.VictimGuid, StringComparison.Ordinal) < 0
                    ? $"{d.KillerGuid}|{d.VictimGuid}"
                    : $"{d.VictimGuid}|{d.KillerGuid}";

                if (!processed.Add(key)) continue;

                int reverse = 0;
                if (killMatrix.TryGetValue(d.VictimGuid, out var rv))
                    rv.TryGetValue(d.KillerGuid, out reverse);

                rivalries.Add((d.KillerGuid, d.VictimGuid, d.Kills, reverse));
            }

            var top = rivalries.OrderByDescending(r => r.Kills1 + r.Kills2).Take(4).ToList();
            if (top.Count == 0) return;

            sb.AppendLine("## Rivalries & Matchups");
            foreach (var (g1, g2, k1, k2) in top)
            {
                string p1 = ctx.NameByGuid(g1);
                string p2 = ctx.NameByGuid(g2);

                if (k2 == 0)
                    sb.AppendLine($"* **{p1}** shut down **{p2}** — {k1} kills with no answer");
                else if (Math.Abs(k1 - k2) <= 1 && k1 + k2 >= 6)
                    sb.AppendLine($"* **{p1}** vs **{p2}** — fierce dead-even rivalry ({k1}-{k2})");
                else if (Math.Abs(k1 - k2) <= 1)
                    sb.AppendLine($"* **{p1}** vs **{p2}** — tied skirmishes ({k1}-{k2})");
                else
                {
                    bool p1Won = k1 > k2;
                    string w = p1Won ? p1 : p2;
                    string l = p1Won ? p2 : p1;
                    sb.AppendLine($"* **{w}** controlled the matchup vs **{l}** ({Math.Max(k1, k2)}-{Math.Min(k1, k2)})");
                }
            }

            // Call out the biggest single-direction farm / Nemesis
            var topFarm = pairs.OrderByDescending(d => d.Kills).First();
            if (topFarm.Kills >= 5)
            {
                string killer = ctx.NameByGuid(topFarm.KillerGuid);
                string victim = ctx.NameByGuid(topFarm.VictimGuid);
                int rev = 0;
                if (killMatrix.TryGetValue(topFarm.VictimGuid, out var rvf))
                    rvf.TryGetValue(topFarm.KillerGuid, out rev);

                if (rev < topFarm.Kills / 3)
                    sb.AppendLine($"> 💀 **Nemesis Alert:** **{killer}** had **{victim}'s** number all match — {topFarm.Kills} kills to {rev}");
            }
            sb.AppendLine();
        }

        private static void WriteWeaponDominance(StringBuilder sb, SummaryContext ctx)
        {
            var dmgAgg = new Dictionary<string, (int Damage, int Shots, int Hits)>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
                foreach (var (name, stats) in p.WeaponStatistics)
                {
                    if (!dmgAgg.TryGetValue(name, out var cur)) cur = (0, 0, 0);
                    dmgAgg[name] = (cur.Damage + stats.DamageDealt, cur.Shots + stats.ShotsFired, cur.Hits + stats.Hits);
                }

            var killAgg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
                foreach (var (name, kills) in p.WeaponKills)
                {
                    killAgg.TryGetValue(name, out int cur);
                    killAgg[name] = cur + kills;
                }

            if (dmgAgg.Count == 0 && killAgg.Count == 0) return;

            sb.AppendLine("## Weapon Dominance");

            if (dmgAgg.Count > 0)
            {
                foreach (var (weapon, stats) in dmgAgg.OrderByDescending(w => w.Value.Damage).Take(5))
                {
                    double acc = stats.Shots > 0 ? (double)stats.Hits / stats.Shots * 100 : 0;
                    var top = ctx.Players.Where(p => p.WeaponStatistics.ContainsKey(weapon))
                                 .OrderByDescending(p => p.WeaponStatistics[weapon].DamageDealt).FirstOrDefault();
                    sb.AppendLine($"**{CleanWeaponName(weapon)}** · {stats.Damage:N0} dmg · {acc:F1}% acc · Top: {(top != null ? ctx.Name(top) : "-")}");
                }
            }
            else
            {
                foreach (var (weapon, kills) in killAgg.OrderByDescending(w => w.Value).Take(5))
                {
                    var top = ctx.Players.Where(p => p.WeaponKills.ContainsKey(weapon))
                                 .OrderByDescending(p => p.WeaponKills[weapon]).FirstOrDefault();
                    sb.AppendLine($"**{CleanWeaponName(weapon)}** · {kills} kills · Top: {(top != null ? ctx.Name(top) : "-")}");
                }
            }
            sb.AppendLine();
        }

        private static void WriteGameModeAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            switch (ctx.Match.GameMode)
            {
                case UT2004GameMode.iCTF: WriteICTFAnalysis(sb, ctx); break;
                case UT2004GameMode.TAM: WriteTAMAnalysis(sb, ctx); break;
                case UT2004GameMode.iBR: WriteIBRAnalysis(sb, ctx); break;
            }
        }

        private static void WriteICTFAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## iCTF Deep Dive");

            int totalCaps = ctx.Players.Sum(p => p.FlagCaptures);
            int totalGrabs = ctx.Players.Sum(p => p.FlagGrabs);
            int totalPickups = ctx.Players.Sum(p => p.FlagPickups);
            int totalReturns = ctx.Players.Sum(p => p.FlagReturns);
            int totalDenials = ctx.Players.Sum(p => p.FlagDenials);
            int totalDrops = ctx.Players.Sum(p => p.FlagDrops);
            int totalProtects = ctx.Players.Sum(p => p.TeamProtectFrags);
            int totalCritFrags = ctx.Players.Sum(p => p.CriticalFrags);
            double capConversion = totalGrabs > 0 ? (double)totalCaps / totalGrabs * 100 : 0;
            double survivalRate = totalGrabs > 0 ? (1.0 - (double)totalDrops / totalGrabs) * 100 : 0;

            sb.AppendLine($"* **Flag Captures**: {totalCaps} from {totalGrabs} grabs ({capConversion:F0}% conversion)");
            sb.AppendLine($"* **Flag Pickups**: {totalPickups} (re-grabs of dropped flags)");
            sb.AppendLine($"* **Carrier Survival**: {survivalRate:F0}% ({totalDrops} drops from {totalGrabs} grabs)");
            sb.AppendLine($"* **Defensive Actions**: {totalReturns} returns, {totalDenials} denials");
            sb.AppendLine($"* **Support Play**: {totalProtects} protective frags, {totalCritFrags} critical frags");

            sb.AppendLine();
            sb.AppendLine("**Team Objective Breakdown:**");
            foreach (var t in ctx.Teams)
            {
                int tGrabs = t.Players.Sum(p => p.FlagGrabs);
                int tCaps = t.Players.Sum(p => p.FlagCaptures);
                double tConv = tGrabs > 0 ? (double)tCaps / tGrabs * 100 : 0;
                int tRet = t.Players.Sum(p => p.FlagReturns);
                int tDen = t.Players.Sum(p => p.FlagDenials);
                int tProt = t.Players.Sum(p => p.TeamProtectFrags);
                sb.AppendLine($"**{t.Label}** · {tCaps} caps / {tGrabs} grabs ({tConv:F0}%) · {tRet} ret · {tDen} den · {tProt} prot");
            }

            var capTimeline = ctx.Timeline.Where(e => e.EventType == "FlagCapture").ToList();
            if (capTimeline.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Capture Timeline:**");
                int runningRed = 0, runningBlue = 0;
                foreach (var e in capTimeline)
                {
                    string actor = e.ActorName ?? ctx.NameByGuid(e.ActorGuid);
                    var scorer = ctx.Players.FirstOrDefault(p => p.Guid == e.ActorGuid);
                    if (scorer != null && scorer.Team == 0) runningRed++;
                    else if (scorer != null && scorer.Team == 1) runningBlue++;
                    sb.AppendLine($"  [{FmtTime(e.GameTimeSeconds)}] {actor} — Score: Red {runningRed} - {runningBlue} Blue");
                }
            }
            sb.AppendLine();
        }

        private static void WriteTAMAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## TAM Deep Dive");

            int totalDmg = ctx.Players.Sum(p => p.TotalDamageDealt);
            int maxRounds = ctx.Players.Count > 0 ? ctx.Players.Max(p => p.RoundsPlayed) : 0;
            int totalREK = ctx.Players.Sum(p => p.RoundEndingKills);
            int totalFF = ctx.Players.Sum(p => p.FriendlyFireDamage);
            double avgDmgPerRound = maxRounds > 0 ? (double)totalDmg / maxRounds : 0;

            sb.AppendLine($"* **Total Damage**: {totalDmg:N0} across {maxRounds} rounds ({avgDmgPerRound:F0} avg/round)");
            sb.AppendLine($"* **Round-Ending Kills**: {totalREK}");
            if (totalFF > 0)
                sb.AppendLine($"* **Friendly Fire**: {totalFF:N0} collateral damage");

            sb.AppendLine();
            sb.AppendLine("**Team Damage Comparison:**");
            foreach (var t in ctx.Teams)
            {
                int tDmg = t.Players.Sum(p => p.TotalDamageDealt);
                int tRoundsWon = t.Players.Count > 0 ? t.Players.Max(p => p.RoundsWon) : 0;
                sb.AppendLine($"* **{t.Label}**: {tDmg:N0} dealt — {tRoundsWon} rounds won");
            }

            sb.AppendLine();
            sb.AppendLine("**Damage Breakdown:**");
            foreach (var p in ctx.Players.OrderByDescending(p => p.TotalDamageDealt))
                sb.AppendLine($"**{ctx.Name(p)}** · {p.TotalDamageDealt:N0} dealt · {p.GetDamagePerRound():F0}/rnd · {p.RoundEndingKills} REK · {p.FriendlyFireDamage} FF");

            sb.AppendLine();
        }

        private static void WriteIBRAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## iBR Deep Dive");

            int totalBallCaps = ctx.Players.Sum(p => p.BallCaptures);
            int totalAssists = ctx.Players.Sum(p => p.BallScoreAssists);
            int totalThrown = ctx.Players.Sum(p => p.BallThrownFinals);
            int totalPickups = ctx.Players.Sum(p => p.BombPickups);
            int totalDrops = ctx.Players.Sum(p => p.BombDrops);
            int totalTaken = ctx.Players.Sum(p => p.BombTaken);

            sb.AppendLine($"* **Scoring**: {totalBallCaps} run-in caps, {totalThrown} thrown goals ({totalAssists} assisted)");
            sb.AppendLine($"* **Ball Movement**: {totalPickups} pickups, {totalDrops} drops, {totalTaken} taken/stolen");

            // Per-team
            sb.AppendLine();
            foreach (var t in ctx.Teams)
            {
                int tCaps = t.Players.Sum(p => p.BallCaptures);
                int tThrown = t.Players.Sum(p => p.BallThrownFinals);
                int tAssists = t.Players.Sum(p => p.BallScoreAssists);
                int tPicks = t.Players.Sum(p => p.BombPickups);
                sb.AppendLine($"* **{t.Label}**: {tCaps} run-ins, {tThrown} throws, {tAssists} assists, {tPicks} pickups");
            }

            // Scoring Timeline — relies on parsed timeline events (BombCapture / BombThrown)
            var scoreTimeline = ctx.Timeline
                .Where(e => e.EventType is "BombCapture" or "BombThrown")
                .OrderBy(e => e.GameTimeSeconds)
                .ToList();

            sb.AppendLine();
            if (scoreTimeline.Count > 0)
            {
                sb.AppendLine("**Scoring Timeline:**");
                int runningRed = 0, runningBlue = 0;
                foreach (var e in scoreTimeline)
                {
                    string actor = e.ActorName ?? ctx.NameByGuid(e.ActorGuid);
                    var scorer = ctx.Players.FirstOrDefault(p => p.Guid == e.ActorGuid);

                    int points = e.EventType == "BombCapture" ? 7 : 3;
                    string action = e.EventType == "BombCapture" ? "runs the ball in" : "throws a goal";

                    if (scorer != null && scorer.Team == 0) runningRed += points;
                    else if (scorer != null && scorer.Team == 1) runningBlue += points;

                    sb.AppendLine($"  [{FmtTime(e.GameTimeSeconds)}] {actor} {action} (+{points}) — Score: Red {runningRed} - {runningBlue} Blue");
                }
                sb.AppendLine();
            }
        }

        private static void WriteMomentumAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            if (ctx.KillEvents.Count < 6 || ctx.DurationMinutes < 1) return;

            var guidToTeam = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
                if (p.Guid != null) guidToTeam[p.Guid] = p.Team;

            double totalSeconds = ctx.DurationMinutes * 60;
            int windowCount = Math.Clamp((int)(ctx.DurationMinutes / 3), 2, 6);
            double windowSize = totalSeconds / windowCount;

            sb.AppendLine("## Momentum Flow");

            int runningRed = 0, runningBlue = 0;
            int redStreakMax = 0, blueStreakMax = 0;

            for (int w = 0; w < windowCount; w++)
            {
                double start = w * windowSize;
                double end = (w + 1) * windowSize;
                int redK = 0, blueK = 0;

                foreach (var k in ctx.KillEvents.Where(e => e.GameTimeSeconds >= start && e.GameTimeSeconds < end))
                    if (k.ActorGuid != null && guidToTeam.TryGetValue(k.ActorGuid, out int team))
                    {
                        if (team == 0) redK++;
                        else if (team == 1) blueK++;
                    }

                runningRed += redK;
                runningBlue += blueK;
                int diff = redK - blueK;
                if (diff > 0) redStreakMax = Math.Max(redStreakMax, diff);
                if (diff < 0) blueStreakMax = Math.Max(blueStreakMax, -diff);

                string advantage = diff switch
                {
                    > 3 => $"**Red +{diff}** !!",
                    > 0 => $"Red +{diff}",
                    0 => "Even",
                    > -4 => $"Blue +{-diff}",
                    _ => $"**Blue +{-diff}** !!"
                };

                sb.AppendLine($"`{FmtTime(start)}-{FmtTime(end)}` · Red {redK} / Blue {blueK} · {advantage}");
            }

            string overallWinner = runningRed > runningBlue ? "Red Team"
                : runningBlue > runningRed ? "Blue Team"
                : "neither team";
            sb.AppendLine($"> Kill advantage: **{overallWinner}** ({runningRed}-{runningBlue})");

            if (redStreakMax >= 5 || blueStreakMax >= 5)
            {
                string dominant = redStreakMax >= blueStreakMax ? "Red" : "Blue";
                int swing = Math.Max(redStreakMax, blueStreakMax);
                sb.AppendLine($"> Biggest momentum swing: **{dominant}** had a window with a massive +{swing} kill advantage");
            }
            sb.AppendLine();
        }

        private static void WriteCombatRoles(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## Combat Role Classification");

            double avgKills = ctx.Players.Average(p => (double)p.Kills);
            double avgDeaths = ctx.Players.Average(p => (double)p.Deaths);

            foreach (var p in ctx.Players.OrderByDescending(p => p.Score))
            {
                string name = ctx.Name(p);
                string team = TeamLabel(p.Team);

                (string role, string icon, string reason) = ctx.Match.GameMode switch
                {
                    UT2004GameMode.TAM => ClassifyTAMRole(p, ctx, avgKills, avgDeaths),
                    UT2004GameMode.iBR => ClassifyIBRRole(p, ctx, avgKills, avgDeaths),
                    _ => ClassifyICTFRole(p, ctx, avgKills, avgDeaths)
                };

                sb.AppendLine($"* {icon} **{name}** ({team}): **{role}** — {reason}");
            }
            sb.AppendLine();
        }

        private static (string role, string icon, string reason) ClassifyTAMRole(
            UTPlayerMatchStats p, SummaryContext ctx, double avgKills, double avgDeaths)
        {
            double actPct = Math.Clamp((p.TotalTimeSeconds / ctx.BaselineMatchTimeSeconds) * 100.0, 0, 100);
            if (actPct < 25) return ("Late Joiner / AFK", "💤", $"low match presence ({actPct:F0}% active)");

            double kd = p.GetKillDeathRatio();
            bool highEngagement = (p.Kills + p.Deaths) > (avgKills + avgDeaths) * 1.2;
            int maxREK = ctx.Players.Max(x => x.RoundEndingKills);

            if (p.Kills == 0 && p.Deaths == 0)
                return ("Spectator", "👻", "no recorded combat actions");

            if (maxREK > 0 && p.RoundEndingKills >= 2 && p.RoundEndingKills >= maxREK * 0.75)
                return ("Clutch Closer", "⚡", $"{p.RoundEndingKills} round-ending kills");

            if (kd >= 2.0 && p.Kills >= avgKills)
                return ("Elite Slayer", "⚔️", $"{kd:F2} K/D, {p.TotalDamageDealt:N0} dmg");

            if (kd >= 1.5 && p.TotalDamageDealt > 0)
                return ("Efficient Fighter", "🎯", $"{kd:F2} K/D, {p.TotalDamageDealt:N0} dmg");

            if (highEngagement && kd >= 1.0)
                return ("Frontline Enforcer", "💥", $"{p.Kills}K/{p.Deaths}D ({kd:F2} K/D)");

            if (p.TeamProtectFrags >= 2)
                return ("Team Anchor", "🛡️", $"{p.TeamProtectFrags} protections");

            if (highEngagement)
                return ("Frontline Brawler", "💥", $"{p.Kills}K/{p.Deaths}D");

            return ("Roamer", "🔄", "balanced combat presence");
        }

        private static (string role, string icon, string reason) ClassifyIBRRole(
            UTPlayerMatchStats p, SummaryContext ctx, double avgKills, double avgDeaths)
        {
            double actPct = Math.Clamp((p.TotalTimeSeconds / ctx.BaselineMatchTimeSeconds) * 100.0, 0, 100);
            if (actPct < 25) return ("Late Joiner / AFK", "💤", $"low match presence ({actPct:F0}% active)");

            double kd = p.GetKillDeathRatio();
            bool highEngagement = (p.Kills + p.Deaths) > (avgKills + avgDeaths) * 1.2;
            int maxCaps = ctx.Players.Max(x => x.BallCaptures);
            int maxCritFrags = ctx.Players.Max(x => x.CriticalFrags);
            int maxPickups = ctx.Players.Max(x => x.BombPickups);

            if (p.Kills == 0 && p.Deaths == 0)
                return ("Spectator", "👻", "no recorded actions");

            // Primary scorer — caps the ball
            if (p.BallCaptures >= 1 && (maxCaps == 0 || p.BallCaptures >= maxCaps * 0.5))
            {
                string capDetail = p.BallThrownFinals > 0 ? $"{p.BallCaptures} run-in caps, {p.BombPickups} pickups, {p.BallThrownFinals} thrown goals" : $"{p.BallCaptures} run-in caps, {p.BombPickups} pickups";
                return ("Ball Runner", "🏃", capDetail);
            }

            // Throws the ball in for goals (finisher without capping directly)
            if (p.BallThrownFinals >= 2)
                return ("Clutch Scorer", "🎯", $"{p.BallThrownFinals} thrown goals, {p.BallScoreAssists} assists");

            // Dominant fragger — high K/D and kill volume
            if (kd >= 1.6 && p.Kills >= avgKills)
                return ("Elite Fragger", "⚔️", $"{kd:F2} K/D, {p.Kills} kills");

            // Carries the ball frequently AND pulls their weight in kills
            if (maxPickups > 0 && p.BombPickups >= Math.Max(maxPickups * 0.35, 4) && p.Kills >= avgKills * 0.6)
                return ("Ball Escort", "🛡️", $"{p.BombPickups} pickups, {p.Kills} cover kills");

            // Kills near the ball — positionally dominant around objectives
            if (maxCritFrags > 0 && p.CriticalFrags >= Math.Max(maxCritFrags * 0.55, 3))
                return ("Tactical Enforcer", "⚡", $"{p.CriticalFrags} critical frags near the ball");

            // High combat presence with positive K/D
            if (highEngagement && kd >= 1.0)
                return ("Frontline Enforcer", "💥", $"{p.Kills + p.Deaths} engagements ({kd:F2} K/D)");

            // High combat engagement but struggling
            if (highEngagement)
                return ("Frontline Fighter", "⚔️", $"{p.Kills}K/{p.Deaths}D");

            // Runs the ball frequently without capping — creates space, draws pressure
            if (maxPickups > 0 && p.BombPickups >= Math.Max(maxPickups * 0.25, 4))
                return ("Ball Carrier", "🏃", $"{p.BombPickups} ball pickups");

            // Genuine team support 
            if (p.TeamProtectFrags + p.BallScoreAssists >= 3 && (p.TeamProtectFrags > 0 || p.BallScoreAssists > 0))
                return ("Support", "🤝", $"{p.TeamProtectFrags} protections, {p.BallScoreAssists} assists");

            return ("Roamer", "🔄", "balanced combat & positioning");
        }

        private static (string role, string icon, string reason) ClassifyICTFRole(
            UTPlayerMatchStats p, SummaryContext ctx, double avgKills, double avgDeaths)
        {
            double actPct = Math.Clamp((p.TotalTimeSeconds / ctx.BaselineMatchTimeSeconds) * 100.0, 0, 100);
            if (actPct < 25) return ("Late Joiner / AFK", "💤", $"low match presence ({actPct:F0}% active)");

            double kd = p.GetKillDeathRatio();
            bool highEngagement = (p.Kills + p.Deaths) > (avgKills + avgDeaths) * 1.2;
            int maxCaps = ctx.Players.Max(x => x.FlagCaptures);
            int maxDefActions = ctx.Players.Max(x => x.FlagReturns + x.FlagDenials);
            int defActions = p.FlagReturns + p.FlagDenials;
            int supportActions = p.TeamProtectFrags + p.FlagCaptureAssists;
            int objActions = p.FlagCaptures + p.FlagReturns + p.FlagDenials + p.FlagCaptureAssists;

            if (p.Kills == 0 && p.Deaths == 0)
                return ("Spectator", "👻", "no recorded actions");

            // Primary flag carrier
            if (p.FlagCaptures >= 1 && (maxCaps == 0 || p.FlagCaptures >= maxCaps * 0.5))
                return ("Flag Runner", "🏃", $"{p.FlagCaptures} captures, {p.FlagGrabs} grabs");

            // Pure combat dominance — check BEFORE support
            if (kd >= 2.0 && p.Kills >= avgKills && objActions <= 2)
                return ("Pure Slayer", "⚔️", $"{kd:F2} K/D, focused on frags");

            // High engagement with positive K/D — check BEFORE support
            if (highEngagement && kd >= 1.0)
                return ("Frontline Aggressor", "💥", $"{p.Kills + p.Deaths} engagements ({kd:F2} K/D)");

            // Primary defender
            if (defActions >= 3 && (maxDefActions == 0 || defActions >= maxDefActions * 0.5))
                return ("Elite Defender", "🛡️", $"{p.FlagReturns} returns, {p.FlagDenials} denials");

            // High engagement but negative K/D (brawler)
            if (highEngagement)
                return ("Frontline Brawler", "💥", $"{p.Kills}K/{p.Deaths}D");

            // Kills near the flag — tactical positioning (combat role)
            if (p.CriticalFrags >= 3)
                return ("Critical Fragger", "⚡", $"{p.CriticalFrags} critical frags near flag");

            // Assist-focused playmaker
            if (p.FlagCaptureAssists >= 2)
                return ("Playmaker", "🤝", $"{p.FlagCaptureAssists} cap assists");

            // Genuine support (protections + assists)
            if (supportActions >= 3 && supportActions > 0)
                return ("Support", "🤝", $"{p.TeamProtectFrags} protections, {p.FlagCaptureAssists} assists");

            if (objActions >= 2)
                return ("Objective Specialist", "🏁", $"{objActions} objective actions");

            return ("Roamer", "🔄", "balanced combat & positioning");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string FmtTime(double seconds)
        {
            bool neg = seconds < 0;
            double abs = Math.Abs(seconds);
            int m = (int)(abs / 60);
            int s = (int)(abs % 60);
            return neg ? $"-{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }

        private static string TeamLabel(int team) => team switch
        {
            0 => "Red",
            1 => "Blue",
            _ => $"Team {team}"
        };

        /// <summary>Maps a kill-streak count to the UT2004 spree title.</summary>
        private static string SpreeTitle(int streak) => streak switch
        {
            >= 30 => "Wicked Sick",
            >= 25 => "Godlike",
            >= 20 => "Unstoppable",
            >= 15 => "Dominating",
            >= 10 => "Rampage",
            >= 5 => "Killing Spree",
            _ => $"{streak}-streak"
        };

        /// <summary>Maps a multi-kill level to the UT2004 multi-kill title.</summary>
        private static string MultiTitle(int level) => level switch
        {
            >= 7 => "Wicked Sick",
            6 => "Holy Shit",
            5 => "Ludicrous Kill",
            4 => "Monster Kill",
            3 => "Ultra Kill",
            2 => "Multi Kill",
            1 => "Double Kill",
            _ => $"x{level}"
        };

        /// <summary>Strips common weapon class prefixes for cleaner display.</summary>
        private static string CleanWeaponName(string raw)
        {
            if (raw.StartsWith("NewNet_", StringComparison.OrdinalIgnoreCase))
                return raw.Substring(7);
            if (raw.Contains('.'))
                return raw.Substring(raw.LastIndexOf('.') + 1);
            return raw;
        }

        private static void WritePerformanceTiers(StringBuilder sb, SummaryContext ctx)
        {
            sb.AppendLine("## Performance Tiers");

            // Composite performance score
            var scored = ctx.Players.Select(p =>
            {
                double combat = p.GetKillDeathRatio() * 20 + p.Kills * 2 + p.Headshots * 3;
                double objective = (p.FlagCaptures + p.BallCaptures) * 15 + p.FlagReturns * 8
                    + p.FlagDenials * 10 + p.FlagCaptureAssists * 5 + p.BallScoreAssists * 8
                    + p.CriticalFrags * 6 + p.TeamProtectFrags * 4;
                double impact = p.BestKillStreak * 3 + p.BestMultiKill * 5;

                if (ctx.Match.GameMode == UT2004GameMode.TAM)
                {
                    combat += p.TotalDamageDealt * 0.01 + p.RoundEndingKills * 10;
                    double dmgEff = p.TotalDamageTaken > 0 ? (double)p.TotalDamageDealt / p.TotalDamageTaken : 1;
                    combat += dmgEff * 10;
                }

                if (p.GetOverallAccuracy() > 0)
                    combat += p.GetOverallAccuracy() * 0.5;

                double total = combat + objective + impact;
                return new { Player = p, Score = total };
            }).OrderByDescending(x => x.Score).ToList();

            if (scored.Count == 0) return;

            double maxScore = scored.First().Score;
            double minScore = scored.Last().Score;
            double range = maxScore - minScore;

            foreach (var entry in scored)
            {
                double normalized = range > 0 ? (entry.Score - minScore) / range : 0.5;
                (string tier, string icon) = normalized switch
                {
                    >= 0.80 => ("S", "🟡"),
                    >= 0.55 => ("A", "🟢"),
                    >= 0.30 => ("B", "🔵"),
                    _ => ("C", "⚪")
                };

                string pName = ctx.Name(entry.Player);
                string team = TeamLabel(entry.Player.Team);

                // Compare to career average
                string vsCareer = "";
                if (entry.Player.Guid != null && ctx.Profiles.TryGetValue(entry.Player.Guid, out var prof) && prof.TotalMatches > 1)
                {
                    double careerAvgKills = ctx.Match.GameMode switch
                    {
                        UT2004GameMode.iCTF when prof.TotalCTFMatches > 0 => (double)prof.TotalCTFKills / prof.TotalCTFMatches,
                        UT2004GameMode.TAM when prof.TotalTAMMatches > 0 => (double)prof.TotalTAMKills / prof.TotalTAMMatches,
                        UT2004GameMode.iBR when prof.TotalBRMatches > 0 => (double)prof.TotalBRKills / prof.TotalBRMatches,
                        _ when prof.TotalMatches > 0 => (double)prof.TotalKills / prof.TotalMatches,
                        _ => 0
                    };

                    if (careerAvgKills > 0)
                    {
                        double diff = entry.Player.Kills - careerAvgKills;
                        string arrow = diff >= 2 ? "📈" : diff <= -2 ? "📉" : "➡️";
                        vsCareer = $" {arrow} {Math.Abs(diff):F1} kills {(diff >= 0 ? "above" : "below")} career avg";
                    }
                }

                sb.AppendLine($"* {icon} **Tier {tier}** — **{pName}** ({team}): {entry.Score:F0} pts{vsCareer}");
            }
            sb.AppendLine();
        }

        private static void WriteEloStandings(StringBuilder sb, SummaryContext ctx)
        {
            if (ctx.EloChanges == null || ctx.EloChanges.Count == 0) return;

            sb.AppendLine("## ELO Standings (Post-Match)");

            var ranked = ctx.Players
                .Where(p => p.Guid != null && ctx.Profiles.ContainsKey(p.Guid))
                .Select(p =>
                {
                    var prof = ctx.Profiles[p.Guid!];
                    double elo = ctx.Match.GameMode switch
                    {
                        UT2004GameMode.iCTF => prof.CaptureTheFlagElo.Rating,
                        UT2004GameMode.TAM => prof.TAMElo.Rating,
                        UT2004GameMode.iBR => prof.BombingRunElo.Rating,
                        _ => 0.0
                    };
                    double peak = ctx.Match.GameMode switch
                    {
                        UT2004GameMode.iCTF => prof.CaptureTheFlagElo.Peak,
                        UT2004GameMode.TAM => prof.TAMElo.Peak,
                        UT2004GameMode.iBR => prof.BombingRunElo.Peak,
                        _ => 0.0
                    };
                    int modeWins = ctx.Match.GameMode switch
                    {
                        UT2004GameMode.iCTF => prof.TotalCTFWins,
                        UT2004GameMode.TAM => prof.TotalTAMWins,
                        UT2004GameMode.iBR => prof.TotalBRWins,
                        _ => prof.Wins
                    };
                    int modeMatches = ctx.Match.GameMode switch
                    {
                        UT2004GameMode.iCTF => prof.TotalCTFMatches,
                        UT2004GameMode.TAM => prof.TotalTAMMatches,
                        UT2004GameMode.iBR => prof.TotalBRMatches,
                        _ => prof.TotalMatches
                    };
                    double change = ctx.EloChanges!.TryGetValue(p.Guid!, out var c) ? c : 0.0;
                    return new { Player = p, Elo = elo, Change = change, Peak = peak, Wins = modeWins, Matches = modeMatches };
                })
                .OrderByDescending(x => x.Elo)
                .ToList();

            int rank = 1;
            foreach (var r in ranked)
            {
                string changeStr = r.Change >= 0 ? $"+{r.Change:F2}" : $"{r.Change:F2}";
                string changeDir = r.Change > 0 ? "^" : r.Change < 0 ? "v" : "=";
                string peakFlag = r.Peak > 0 && Math.Abs(r.Elo - r.Peak) < 0.01 ? " *(PB)*" : "";
                int losses = r.Matches - r.Wins;
                sb.AppendLine($"{rank++}. **{ctx.Name(r.Player)}** · ELO {r.Elo:F0} ({changeDir} {changeStr}) · Peak {r.Peak:F0}{peakFlag} · {r.Wins}W/{losses}L");
            }
            sb.AppendLine();
        }
    }
}
