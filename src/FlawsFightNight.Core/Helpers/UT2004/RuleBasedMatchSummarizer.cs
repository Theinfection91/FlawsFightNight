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
                MapName = match.FileName?.Replace(".json", "") ?? "Unknown",
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

            ctx.KillsPerMinute = ctx.TotalKills / ctx.DurationMinutes;
            ctx.KillsPerMinutePerPlayer = ctx.PlayerCount > 0 ? ctx.KillsPerMinute / ctx.PlayerCount : 0;
            ctx.HeadshotRate = ctx.TotalKills > 0 ? (double)ctx.TotalHeadshots / ctx.TotalKills * 100.0 : 0;

            // Teams
            for (int i = 0; i < match.Players.Count; i++)
            {
                var teamPlayers = match.Players[i].Where(p => p != null).ToList();
                ctx.Teams.Add(new TeamInfo
                {
                    Index = i,
                    Label = i == 0 ? "Red Team" : i == 1 ? "Blue Team" : $"Team {i}",
                    Players = teamPlayers,
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
            bool useObjectiveScore = match.GameMode is UT2004GameMode.iCTF or UT2004GameMode.iBR;

            if (winner != null && loser != null)
            {
                int winVal = useObjectiveScore ? winner.TotalCaps : winner.TotalScore;
                int loseVal = useObjectiveScore ? loser.TotalCaps : loser.TotalScore;
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
                    .OrderByDescending(p => p.FlagCaptures + p.BallCaptures + p.FlagReturns + p.FlagDenials + p.BallScoreAssists)
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
                ctx.NarrativePhrase = "an action-packed slugfest packed with highlight-reel plays";
            else
                ctx.NarrativePhrase = "a hard-fought contest with both teams trading blows";
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
            sb.AppendLine($"A {ctx.PaceWord}, {ctx.IntensityWord} {match.GameModeName} match on **{ctx.MapName}** lasting **{duration}** with **{ctx.PlayerCount} players**.");

            // Winner announcement
            var winner = ctx.Teams.FirstOrDefault(t => t.IsWinner);
            var loser = ctx.Teams.FirstOrDefault(t => !t.IsWinner);
            if (winner != null && loser != null)
            {
                bool useObj = match.GameMode is UT2004GameMode.iCTF or UT2004GameMode.iBR;
                string scoreStr = useObj
                    ? $"{winner.TotalCaps}-{loser.TotalCaps}"
                    : $"{winner.TotalScore}-{loser.TotalScore}";
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
                    UT2004GameMode.iCTF => $"{t.TotalCaps} Caps",
                    UT2004GameMode.iBR => $"{t.TotalCaps} Caps",
                    _ => $"{t.TotalScore} Score"
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

            if (isTAM)
            {
                sb.AppendLine("| Player | Team | Score | K | D | K/D | Dmg Dealt | Dmg/Rnd | RndEnders | Acc% | Streak | Multi |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            }
            else
            {
                sb.AppendLine("| Player | Team | Score | K | D | K/D | HS | Caps | Ret/Den | Acc% | Streak | Multi |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            }

            foreach (var p in ctx.Players.OrderByDescending(p => p.Score))
            {
                string name = ctx.Name(p);
                string team = TeamLabel(p.Team);
                string kd = p.GetKillDeathRatio().ToString("F2");
                string streak = p.BestKillStreak >= 5 ? SpreeTitle(p.BestKillStreak) : "-";
                string multi = p.BestMultiKill >= 2 ? MultiTitle(p.BestMultiKill) : "-";
                string acc = hasAccuracy && p.GetOverallAccuracy() > 0 ? $"{p.GetOverallAccuracy():F1}" : "-";

                if (isTAM)
                {
                    string dpr = p.GetDamagePerRound() > 0 ? $"{p.GetDamagePerRound():F0}" : "-";
                    sb.AppendLine($"| {name} | {team} | {p.Score} | {p.Kills} | {p.Deaths} | {kd} | {p.TotalDamageDealt} | {dpr} | {p.RoundEndingKills} | {acc} | {streak} | {multi} |");
                }
                else
                {
                    int caps = p.FlagCaptures + p.BallCaptures;
                    string retDen = $"{p.FlagReturns}/{p.FlagDenials}";
                    sb.AppendLine($"| {name} | {team} | {p.Score} | {p.Kills} | {p.Deaths} | {kd} | {p.Headshots} | {caps} | {retDen} | {acc} | {streak} | {multi} |");
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
                moments.Add((e.GameTimeSeconds, "💥", $"**{actor}** lands a **{e.Detail}** at {FmtTime(e.GameTimeSeconds)}", 8));
            }

            // Flag / bomb captures
            var capEvents = ctx.Timeline.Where(e => e.EventType is "FlagCapture" or "BombCapture").ToList();
            if (capEvents.Count > 0)
            {
                var first = capEvents.First();
                string firstActor = first.ActorName ?? ctx.NameByGuid(first.ActorGuid);
                moments.Add((first.GameTimeSeconds, "🚩", $"**{firstActor}** secures the first capture at {FmtTime(first.GameTimeSeconds)}", 6));

                if (capEvents.Count > 1)
                {
                    var last = capEvents.Last();
                    string lastActor = last.ActorName ?? ctx.NameByGuid(last.ActorGuid);
                    moments.Add((last.GameTimeSeconds, "🏆", $"**{lastActor}** delivers the decisive final capture at {FmtTime(last.GameTimeSeconds)}", 8));
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

            // Kill Leader
            if (ctx.TopKiller != null)
            {
                string t = TeamLabel(ctx.TopKiller.Team);
                double kpm = ctx.DurationMinutes > 0 ? ctx.TopKiller.Kills / ctx.DurationMinutes : 0;
                string extra = kpm > 2 ? $" ({kpm:F1} kills/min)" : "";
                awards.Add(("🗡️ Kill Leader", ctx.Name(ctx.TopKiller), t, $"{ctx.TopKiller.Kills} kills{extra}"));
            }

            // Efficiency King (best K/D, different from kill leader, min kills)
            if (ctx.TopKD != null && ctx.TopKD != ctx.TopKiller)
            {
                string t = TeamLabel(ctx.TopKD.Team);
                awards.Add(("🎯 Efficiency King", ctx.Name(ctx.TopKD), t, $"{ctx.TopKD.GetKillDeathRatio():F2} K/D ({ctx.TopKD.Kills}K / {ctx.TopKD.Deaths}D)"));
            }

            // Objective MVP
            if (ctx.TopObjective != null)
            {
                int obj = ctx.TopObjective.FlagCaptures + ctx.TopObjective.BallCaptures + ctx.TopObjective.FlagReturns + ctx.TopObjective.FlagDenials + ctx.TopObjective.BallScoreAssists;
                if (obj > 0)
                {
                    string t = TeamLabel(ctx.TopObjective.Team);
                    string detail = ctx.Match.GameMode == UT2004GameMode.iCTF
                        ? $"{ctx.TopObjective.FlagCaptures} caps, {ctx.TopObjective.FlagReturns} returns, {ctx.TopObjective.FlagDenials} denials"
                        : $"{ctx.TopObjective.BallCaptures} caps, {ctx.TopObjective.BallScoreAssists} assists";
                    awards.Add(("🏁 Objective MVP", ctx.Name(ctx.TopObjective), t, detail));
                }
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

            // Damage King (TAM)
            if (ctx.TopDamage != null && ctx.Match.GameMode == UT2004GameMode.TAM)
            {
                string t = TeamLabel(ctx.TopDamage.Team);
                double eff = ctx.TopDamage.TotalDamageTaken > 0
                    ? (double)ctx.TopDamage.TotalDamageDealt / ctx.TopDamage.TotalDamageTaken
                    : ctx.TopDamage.TotalDamageDealt;
                awards.Add(("💪 Damage King", ctx.Name(ctx.TopDamage), t, $"{ctx.TopDamage.TotalDamageDealt:N0} damage ({eff:F2} dealt/taken ratio)"));
            }

            // Iron Wall (iCTF: top returns + denials)
            if (ctx.Match.GameMode == UT2004GameMode.iCTF)
            {
                var wall = ctx.Players.OrderByDescending(p => p.FlagReturns + p.FlagDenials).FirstOrDefault();
                if (wall != null && (wall.FlagReturns + wall.FlagDenials) >= 3)
                {
                    string t = TeamLabel(wall.Team);
                    awards.Add(("🛡️ Iron Wall", ctx.Name(wall), t, $"{wall.FlagReturns} returns, {wall.FlagDenials} denials"));
                }
            }

            // Playmaker (most assists)
            var playmaker = ctx.Players.OrderByDescending(p => p.FlagCaptureAssists + p.BallScoreAssists).FirstOrDefault();
            if (playmaker != null && (playmaker.FlagCaptureAssists + playmaker.BallScoreAssists) >= 2)
            {
                string t = TeamLabel(playmaker.Team);
                int assists = playmaker.FlagCaptureAssists + playmaker.BallScoreAssists;
                awards.Add(("🤝 Playmaker", ctx.Name(playmaker), t, $"{assists} capture assists"));
            }

            // Guardian Angel (most team protect frags)
            var guardian = ctx.Players.OrderByDescending(p => p.TeamProtectFrags).FirstOrDefault();
            if (guardian != null && guardian.TeamProtectFrags >= 3)
            {
                string t = TeamLabel(guardian.Team);
                awards.Add(("👼 Guardian Angel", ctx.Name(guardian), t, $"{guardian.TeamProtectFrags} protective frags"));
            }

            // Clutch Player (most critical frags)
            var clutch = ctx.Players.OrderByDescending(p => p.CriticalFrags).FirstOrDefault();
            if (clutch != null && clutch.CriticalFrags >= 2)
            {
                string t = TeamLabel(clutch.Team);
                awards.Add(("⚡ Clutch Performer", ctx.Name(clutch), t, $"{clutch.CriticalFrags} critical frags"));
            }

            // Bullet Magnet (most deaths, noticeably above average)
            double avgDeaths = ctx.TotalDeaths / (double)ctx.PlayerCount;
            if (ctx.MostDeaths != null && ctx.MostDeaths.Deaths > avgDeaths * 1.4)
            {
                string t = TeamLabel(ctx.MostDeaths.Team);
                awards.Add(("🎯 Bullet Magnet", ctx.Name(ctx.MostDeaths), t, $"{ctx.MostDeaths.Deaths} deaths (avg: {avgDeaths:F0})"));
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
                    break; // Only highlight one
                }
            }

            foreach (var (title, player, team, reason) in awards)
            {
                string teamTag = !string.IsNullOrEmpty(team) ? $" ({team})" : "";
                sb.AppendLine($"* {title}: **{player}**{teamTag} — {reason}");
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
                else if (Math.Abs(k1 - k2) <= 1)
                    sb.AppendLine($"* **{p1}** vs **{p2}** — dead-even rivalry ({k1}-{k2})");
                else
                {
                    bool p1Won = k1 > k2;
                    string w = p1Won ? p1 : p2;
                    string l = p1Won ? p2 : p1;
                    sb.AppendLine($"* **{w}** owned the matchup vs **{l}** ({Math.Max(k1, k2)}-{Math.Min(k1, k2)})");
                }
            }

            // Call out the biggest single-direction farm
            var topFarm = pairs.OrderByDescending(d => d.Kills).First();
            if (topFarm.Kills >= 5)
            {
                string killer = ctx.NameByGuid(topFarm.KillerGuid);
                string victim = ctx.NameByGuid(topFarm.VictimGuid);
                int rev = 0;
                if (killMatrix.TryGetValue(topFarm.VictimGuid, out var rvf))
                    rvf.TryGetValue(topFarm.KillerGuid, out rev);
                if (rev < topFarm.Kills / 2)
                    sb.AppendLine($"> 💀 **{killer}** had **{victim}'s** number all match — {topFarm.Kills} kills to {rev}");
            }
            sb.AppendLine();
        }

        private static void WriteWeaponDominance(StringBuilder sb, SummaryContext ctx)
        {
            // Aggregate weapon accuracy/damage data
            var dmgAgg = new Dictionary<string, (int Damage, int Shots, int Hits)>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
            {
                foreach (var (name, stats) in p.WeaponStatistics)
                {
                    if (!dmgAgg.TryGetValue(name, out var cur))
                        cur = (0, 0, 0);
                    dmgAgg[name] = (cur.Damage + stats.DamageDealt, cur.Shots + stats.ShotsFired, cur.Hits + stats.Hits);
                }
            }

            // Aggregate weapon kill counts
            var killAgg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
            {
                foreach (var (name, kills) in p.WeaponKills)
                {
                    killAgg.TryGetValue(name, out int cur);
                    killAgg[name] = cur + kills;
                }
            }

            if (dmgAgg.Count == 0 && killAgg.Count == 0) return;

            sb.AppendLine("## Weapon Dominance");

            if (dmgAgg.Count > 0)
            {
                sb.AppendLine("| Weapon | Total Damage | Accuracy | Top User |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var (weapon, stats) in dmgAgg.OrderByDescending(w => w.Value.Damage).Take(5))
                {
                    double acc = stats.Shots > 0 ? (double)stats.Hits / stats.Shots * 100 : 0;
                    var topUser = ctx.Players
                        .Where(p => p.WeaponStatistics.ContainsKey(weapon))
                        .OrderByDescending(p => p.WeaponStatistics[weapon].DamageDealt)
                        .FirstOrDefault();
                    string topName = topUser != null ? ctx.Name(topUser) : "-";
                    sb.AppendLine($"| {CleanWeaponName(weapon)} | {stats.Damage:N0} | {acc:F1}% | {topName} |");
                }
            }
            else
            {
                sb.AppendLine("| Weapon | Total Kills | Top User |");
                sb.AppendLine("|---|---|---|");
                foreach (var (weapon, kills) in killAgg.OrderByDescending(w => w.Value).Take(5))
                {
                    var topUser = ctx.Players
                        .Where(p => p.WeaponKills.ContainsKey(weapon))
                        .OrderByDescending(p => p.WeaponKills[weapon])
                        .FirstOrDefault();
                    string topName = topUser != null ? ctx.Name(topUser) : "-";
                    sb.AppendLine($"| {CleanWeaponName(weapon)} | {kills} | {topName} |");
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

            // Per-team efficiency comparison
            sb.AppendLine();
            sb.AppendLine("**Team Objective Breakdown:**");
            sb.AppendLine("| Team | Caps | Grabs | Conversion | Returns | Denials | Protects |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var t in ctx.Teams)
            {
                int tGrabs = t.Players.Sum(p => p.FlagGrabs);
                int tCaps = t.Players.Sum(p => p.FlagCaptures);
                double tConv = tGrabs > 0 ? (double)tCaps / tGrabs * 100 : 0;
                int tRet = t.Players.Sum(p => p.FlagReturns);
                int tDen = t.Players.Sum(p => p.FlagDenials);
                int tProt = t.Players.Sum(p => p.TeamProtectFrags);
                sb.AppendLine($"| {t.Label} | {tCaps} | {tGrabs} | {tConv:F0}% | {tRet} | {tDen} | {tProt} |");
            }

            // Cap timeline
            var capTimeline = ctx.Timeline.Where(e => e.EventType == "FlagCapture").ToList();
            if (capTimeline.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Capture Timeline:**");
                int runningRed = 0, runningBlue = 0;
                foreach (var e in capTimeline)
                {
                    string actor = e.ActorName ?? ctx.NameByGuid(e.ActorGuid);
                    // Determine which team scored based on actor
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

            // Team comparison
            sb.AppendLine();
            sb.AppendLine("**Team Damage Comparison:**");
            foreach (var t in ctx.Teams)
            {
                int tDmg = t.Players.Sum(p => p.TotalDamageDealt);
                int tTaken = t.Players.Sum(p => p.TotalDamageTaken);
                double tEff = tTaken > 0 ? (double)tDmg / tTaken : tDmg;
                int tRoundsWon = t.Players.Count > 0 ? t.Players.Max(p => p.RoundsWon) : 0;
                sb.AppendLine($"* **{t.Label}**: {tDmg:N0} dealt / {tTaken:N0} taken (ratio: {tEff:F2}) — {tRoundsWon} rounds won");
            }

            // Damage efficiency leaderboard
            sb.AppendLine();
            sb.AppendLine("**Damage Breakdown:**");
            sb.AppendLine("| Player | Dealt | Taken | Ratio | Dmg/Round | Round Enders | FF |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var p in ctx.Players.OrderByDescending(p => p.TotalDamageDealt))
            {
                double eff = p.TotalDamageTaken > 0 ? (double)p.TotalDamageDealt / p.TotalDamageTaken : p.TotalDamageDealt;
                sb.AppendLine($"| {ctx.Name(p)} | {p.TotalDamageDealt:N0} | {p.TotalDamageTaken:N0} | {eff:F2} | {p.GetDamagePerRound():F0} | {p.RoundEndingKills} | {p.FriendlyFireDamage} |");
            }
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

            sb.AppendLine($"* **Ball Captures**: {totalBallCaps} ({totalAssists} assisted, {totalThrown} thrown-in finals)");
            sb.AppendLine($"* **Ball Movement**: {totalPickups} pickups, {totalDrops} drops, {totalTaken} taken");

            // Per-team
            sb.AppendLine();
            foreach (var t in ctx.Teams)
            {
                int tCaps = t.Players.Sum(p => p.BallCaptures);
                int tAssists = t.Players.Sum(p => p.BallScoreAssists);
                int tPicks = t.Players.Sum(p => p.BombPickups);
                sb.AppendLine($"* **{t.Label}**: {tCaps} caps, {tAssists} assists, {tPicks} pickups");
            }
            sb.AppendLine();
        }

        private static void WriteMomentumAnalysis(StringBuilder sb, SummaryContext ctx)
        {
            if (ctx.KillEvents.Count < 6 || ctx.DurationMinutes < 1) return;

            // Build GUID → team lookup
            var guidToTeam = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ctx.Players)
            {
                if (p.Guid != null)
                    guidToTeam[p.Guid] = p.Team;
            }

            double totalSeconds = ctx.DurationMinutes * 60;
            int windowCount = Math.Clamp((int)(ctx.DurationMinutes / 3), 2, 6);
            double windowSize = totalSeconds / windowCount;

            sb.AppendLine("## Momentum Flow");
            sb.AppendLine("| Period | Red Kills | Blue Kills | Advantage |");
            sb.AppendLine("|---|---|---|---|");

            int runningRed = 0, runningBlue = 0;
            int redStreakMax = 0, blueStreakMax = 0;

            for (int w = 0; w < windowCount; w++)
            {
                double start = w * windowSize;
                double end = (w + 1) * windowSize;

                int redK = 0, blueK = 0;
                foreach (var k in ctx.KillEvents.Where(e => e.GameTimeSeconds >= start && e.GameTimeSeconds < end))
                {
                    if (k.ActorGuid != null && guidToTeam.TryGetValue(k.ActorGuid, out int team))
                    {
                        if (team == 0) redK++;
                        else if (team == 1) blueK++;
                    }
                }

                runningRed += redK;
                runningBlue += blueK;

                int diff = redK - blueK;
                if (diff > 0) redStreakMax = Math.Max(redStreakMax, diff);
                if (diff < 0) blueStreakMax = Math.Max(blueStreakMax, -diff);

                string advantage = diff switch
                {
                    > 3 => $"🔴 Red +{diff} 🔥",
                    > 0 => $"🔴 Red +{diff}",
                    0 => "⚔️ Even",
                    > -4 => $"🔵 Blue +{-diff}",
                    _ => $"🔵 Blue +{-diff} 🔥"
                };

                sb.AppendLine($"| {FmtTime(start)}-{FmtTime(end)} | {redK} | {blueK} | {advantage} |");
            }

            // Summary line
            string overallWinner = runningRed > runningBlue ? "Red Team"
                : runningBlue > runningRed ? "Blue Team"
                : "neither team";
            sb.AppendLine($"> Kill advantage: **{overallWinner}** ({runningRed}-{runningBlue})");

            // Detect momentum swings
            if (redStreakMax >= 4 || blueStreakMax >= 4)
            {
                string dominant = redStreakMax >= blueStreakMax ? "Red" : "Blue";
                int swing = Math.Max(redStreakMax, blueStreakMax);
                sb.AppendLine($"> Biggest momentum swing: **{dominant}** had a window with a +{swing} kill advantage");
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

                // Calculate role weights
                double kd = p.GetKillDeathRatio();
                int objActions = p.FlagCaptures + p.BallCaptures + p.FlagReturns + p.FlagDenials + p.BallScoreAssists;
                int supportActions = p.TeamProtectFrags + p.FlagCaptureAssists + p.CriticalFrags;
                int defActions = p.FlagReturns + p.FlagDenials;
                bool highEngagement = (p.Kills + p.Deaths) > (avgKills + avgDeaths) * 1.2;

                string role, icon, reason;

                if (ctx.Match.GameMode == UT2004GameMode.TAM)
                {
                    // TAM-specific roles
                    double dmgEff = p.TotalDamageTaken > 0 ? (double)p.TotalDamageDealt / p.TotalDamageTaken : 0;
                    if (p.RoundEndingKills >= 2 && p.RoundEndingKills >= ctx.Players.Max(x => x.RoundEndingKills) * 0.75)
                    {
                        role = "Clutch Closer"; icon = "⚡"; reason = $"{p.RoundEndingKills} round-ending kills";
                    }
                    else if (kd >= 2.0 && p.Kills >= avgKills)
                    {
                        role = "Elite Slayer"; icon = "⚔️"; reason = $"{kd:F2} K/D, {p.TotalDamageDealt:N0} dmg";
                    }
                    else if (dmgEff >= 1.5 && p.TotalDamageDealt > 0)
                    {
                        role = "Efficient Fighter"; icon = "🎯"; reason = $"{dmgEff:F2} dmg ratio";
                    }
                    else if (p.TeamProtectFrags >= 2)
                    {
                        role = "Team Anchor"; icon = "🛡️"; reason = $"{p.TeamProtectFrags} protections";
                    }
                    else if (highEngagement)
                    {
                        role = "Frontline Brawler"; icon = "💥"; reason = $"{p.Kills}K/{p.Deaths}D in the thick of it";
                    }
                    else
                    {
                        role = "Roamer"; icon = "🔄"; reason = "balanced combat presence";
                    }
                }
                else
                {
                    // iCTF / iBR roles
                    int caps = p.FlagCaptures + p.BallCaptures;
                    if (caps >= 2 && caps >= ctx.Players.Max(x => x.FlagCaptures + x.BallCaptures) * 0.6)
                    {
                        role = "Flag Runner"; icon = "🏃"; reason = $"{caps} captures, {p.FlagGrabs + p.BombPickups} grabs";
                    }
                    else if (defActions >= 3 && defActions >= ctx.Players.Max(x => x.FlagReturns + x.FlagDenials) * 0.6)
                    {
                        role = "Elite Defender"; icon = "🛡️"; reason = $"{p.FlagReturns} returns, {p.FlagDenials} denials";
                    }
                    else if (supportActions >= 3)
                    {
                        role = "Support"; icon = "🤝"; reason = $"{p.TeamProtectFrags} protections, {p.FlagCaptureAssists} assists";
                    }
                    else if (kd >= 2.0 && p.Kills >= avgKills && objActions <= 2)
                    {
                        role = "Pure Slayer"; icon = "⚔️"; reason = $"{kd:F2} K/D, focused on frags";
                    }
                    else if (highEngagement)
                    {
                        role = "Frontline Aggressor"; icon = "💥"; reason = $"{p.Kills + p.Deaths} combat engagements";
                    }
                    else if (objActions >= 2)
                    {
                        role = "Objective Specialist"; icon = "🏁"; reason = $"{objActions} objective actions";
                    }
                    else
                    {
                        role = "Roamer"; icon = "🔄"; reason = "balanced combat & positioning";
                    }
                }

                sb.AppendLine($"* {icon} **{name}** ({team}): **{role}** — {reason}");
            }
            sb.AppendLine();
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
            sb.AppendLine("| # | Player | ELO | Change | Peak | W/L |");
            sb.AppendLine("|---|---|---|---|---|---|");

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
                string changeIcon = r.Change > 0 ? "📈" : r.Change < 0 ? "📉" : "➡️";
                string peakFlag = r.Peak > 0 && Math.Abs(r.Elo - r.Peak) < 0.01 ? " 🏆" : "";
                int losses = r.Matches - r.Wins;
                sb.AppendLine($"| {rank++} | {ctx.Name(r.Player)} | {r.Elo:F0} | {changeIcon} {changeStr} | {r.Peak:F0}{peakFlag} | {r.Wins}W-{losses}L |");
            }
            sb.AppendLine();
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
    }
}
