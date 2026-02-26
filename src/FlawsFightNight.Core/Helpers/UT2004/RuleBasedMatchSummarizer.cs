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

            var sb = new StringBuilder();
            var players = match.Players?.SelectMany(t => t).Where(p => p != null).ToList() ?? new List<UTPlayerMatchStats>();

            if (!players.Any())
            {
                sb.AppendLine("No players recorded.");
                var emptyResult = sb.ToString();
                match.MatchSummary = emptyResult;
                return emptyResult;
            }

            string mapName = match.FileName?.Replace(".json", "") ?? "Unknown";
            
            // 1. Intro
            sb.AppendLine($"This log details a {match.GameModeName} match on the map {mapName}.");
            sb.AppendLine("The match was a high-scoring, fast-paced game. Here is the breakdown of player and team performance.");
            sb.AppendLine();

            // 2. Team Standings
            sb.AppendLine("## Team Standings");
            var teams = match.Players.Select((list, idx) => new
            {
                Index = idx,
                Players = list.Where(p => p != null).ToList(),
                Score = list.Sum(p => p.Score),
                Caps = list.Sum(p => p.FlagCaptures + p.BallCaptures),
                IsWinner = list.Any(p => p != null && p.IsWinner)
            }).ToList();

            foreach (var t in teams)
            {
                string teamName = t.Index == 0 ? "Red Team" : t.Index == 1 ? "Blue Team" : $"Team {t.Index}";
                string winnerText = t.IsWinner ? " (Winners)" : "";
                string scoreText = match.GameMode == Enums.UT2004.UT2004GameMode.iCTF ? $"{t.Caps} Caps" : $"{t.Score} Score";
                sb.AppendLine($"* {teamName} (Team {t.Index}): {scoreText}{winnerText}");
            }
            sb.AppendLine();

            // 3. Player Stats Table
            sb.AppendLine("## Player Performance");
            sb.AppendLine("| Player | Team | Kills | Deaths | Caps | Returns/Denials | Notable Streaks |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var p in players.OrderByDescending(p => p.Score))
            {
                string teamName = p.Team == 0 ? "Red" : p.Team == 1 ? "Blue" : p.Team.ToString();
                int caps = p.FlagCaptures + p.BallCaptures;
                string retDen = $"{p.FlagReturns}/{p.FlagDenials}";
                
                List<string> streaks = new List<string>();
                if (p.BestKillStreak >= 5) streaks.Add($"Spree x{p.BestKillStreak}");
                if (p.BestMultiKill >= 3) streaks.Add($"Multi x{p.BestMultiKill}");
                string streakStr = streaks.Any() ? string.Join(", ", streaks) : "-";

                sb.AppendLine($"| {p.LastKnownName} | {teamName} | {p.Kills} | {p.Deaths} | {caps} | {retDen} | {streakStr} |");
            }
            sb.AppendLine();

            // 4. Performance Analysis (The MVPs)
            sb.AppendLine("## Performance Analysis");
            sb.AppendLine("### The MVPs");
            
            var topKiller = players.OrderByDescending(p => p.Kills).FirstOrDefault();
            if (topKiller != null)
            {
                string teamName = topKiller.Team == 0 ? "Red" : topKiller.Team == 1 ? "Blue" : topKiller.Team.ToString();
                sb.AppendLine($"* **{topKiller.LastKnownName} ({teamName})**: The statistical powerhouse of the match. Led the server in total kills ({topKiller.Kills}).");
            }

            var topObj = players.OrderByDescending(p => p.FlagCaptures + p.BallCaptures + p.FlagReturns).FirstOrDefault();
            if (topObj != null && topObj != topKiller && (topObj.FlagCaptures > 0 || topObj.FlagReturns > 0))
            {
                string teamName = topObj.Team == 0 ? "Red" : topObj.Team == 1 ? "Blue" : topObj.Team.ToString();
                sb.AppendLine($"* **{topObj.LastKnownName} ({teamName})**: The most efficient objective player. Secured {topObj.FlagCaptures + topObj.BallCaptures} captures and {topObj.FlagReturns} returns.");
            }
            sb.AppendLine();

            // 5. Scoring Mechanics & Special Stats
            sb.AppendLine("### Scoring Mechanics & Special Stats");
            sb.AppendLine("| Special Event | Total Count | Leaders |");
            sb.AppendLine("|---|---|---|");

            int totalHs = players.Sum(p => p.Headshots);
            var hsLeaders = players.Where(p => p.Headshots > 0).OrderByDescending(p => p.Headshots).Take(2).Select(p => $"{p.LastKnownName} ({p.Headshots})");
            if (totalHs > 0) sb.AppendLine($"| Headshots | {totalHs} | {string.Join(", ", hsLeaders)} |");

            int totalDenials = players.Sum(p => p.FlagDenials);
            var denLeaders = players.Where(p => p.FlagDenials > 0).OrderByDescending(p => p.FlagDenials).Take(2).Select(p => $"{p.LastKnownName} ({p.FlagDenials})");
            if (totalDenials > 0) sb.AppendLine($"| Flag Denials | {totalDenials} | {string.Join(", ", denLeaders)} |");

            int totalProt = players.Sum(p => p.TeamProtectFrags);
            var protLeaders = players.Where(p => p.TeamProtectFrags > 0).OrderByDescending(p => p.TeamProtectFrags).Take(2).Select(p => $"{p.LastKnownName} ({p.TeamProtectFrags})");
            if (totalProt > 0) sb.AppendLine($"| Team Protections | {totalProt} | {string.Join(", ", protLeaders)} |");

            int totalMulti = players.Sum(p => p.BestMultiKill);
            var multiLeaders = players.Where(p => p.BestMultiKill > 1).OrderByDescending(p => p.BestMultiKill).Take(2).Select(p => $"{p.LastKnownName} (x{p.BestMultiKill})");
            if (totalMulti > 0) sb.AppendLine($"| Multi-Kills | - | {string.Join(", ", multiLeaders)} |");
            sb.AppendLine();

            // 6. Objective Efficiency Index
            sb.AppendLine("## Objective Efficiency Index");
            sb.AppendLine("Efficiency = (Kills / Objective Actions). A lower number means they were more focused on the flag.");
            var effPlayers = players.Select(p => new { 
                Player = p, 
                Obj = p.FlagCaptures + p.BallCaptures + p.FlagReturns + p.FlagDenials,
                Eff = p.Kills / (double)Math.Max(1, p.FlagCaptures + p.BallCaptures + p.FlagReturns + p.FlagDenials)
            }).OrderBy(x => x.Eff).ToList();

            foreach (var ep in effPlayers)
            {
                string teamName = ep.Player.Team == 0 ? "Red" : ep.Player.Team == 1 ? "Blue" : ep.Player.Team.ToString();
                sb.AppendLine($"* **{ep.Player.LastKnownName} ({teamName})**: {ep.Eff:F2} (Kills: {ep.Player.Kills}, Objectives: {ep.Obj})");
            }
            sb.AppendLine();

            // 7. Combat Roles Visualization
            sb.AppendLine("## Combat Roles Visualization");
            var frontline = players.OrderByDescending(p => p.Kills + p.Deaths).FirstOrDefault();
            if (frontline != null) sb.AppendLine($"* **Frontline Aggressor**: {frontline.LastKnownName} (Most kills/deaths)");

            var objSpec = players.OrderByDescending(p => p.FlagReturns + p.FlagDenials + p.FlagCaptures).FirstOrDefault();
            if (objSpec != null) sb.AppendLine($"* **Objective Specialist**: {objSpec.LastKnownName} (Most returns/denials/caps)");

            var finisher = players.OrderByDescending(p => p.FlagCaptures + p.FlagCaptureFirstTouch).FirstOrDefault();
            if (finisher != null) sb.AppendLine($"* **The Finisher**: {finisher.LastKnownName} (Secured the most captures/touches)");

            var support = players.OrderByDescending(p => p.TeamProtectFrags + p.FlagCaptureAssists).FirstOrDefault();
            if (support != null) sb.AppendLine($"* **Support/Roamer**: {support.LastKnownName} (Most protections/assists)");
            sb.AppendLine();

            // 8. Current Standings (ELO)
            if (eloChanges != null && eloChanges.Any())
            {
                sb.AppendLine("## Current Standings (After Match)");
                sb.AppendLine("| Rank | Player | ELO Rating | Change | Role Designation |");
                sb.AppendLine("|---|---|---|---|---|");

                var rankedPlayers = players.OrderByDescending(p => p.Score).ToList();
                int rank = 1;
                foreach (var p in rankedPlayers)
                {
                    double change = eloChanges.ContainsKey(p.Guid) ? eloChanges[p.Guid] : 0.0;
                    double currentElo = 0.0;
                    if (profiles.TryGetValue(p.Guid, out var pr))
                    {
                        currentElo = match.GameMode switch
                        {
                            Enums.UT2004.UT2004GameMode.iCTF => pr.CaptureTheFlagElo.Rating,
                            Enums.UT2004.UT2004GameMode.TAM => pr.TAMElo.Rating,
                            Enums.UT2004.UT2004GameMode.iBR => pr.BombingRunElo.Rating,
                            _ => 0.0
                        };
                    }

                    string role = "Roamer";
                    if (p == frontline) role = "Pure Slayer";
                    else if (p == objSpec) role = "Elite Defender";
                    else if (p == finisher) role = "Objective Specialist";
                    else if (p == support) role = "Support";

                    string changeStr = change >= 0 ? $"+{change:F2}" : $"{change:F2}";
                    sb.AppendLine($"| {rank++} | {p.LastKnownName} | {currentElo:F0} | {changeStr} | {role} |");
                }
                sb.AppendLine();
            }

            var result = sb.ToString();
            // Persist the generated summary on the match so callers don't need to pass eloChanges explicitly
            match.MatchSummary = result;
            return result;
        }
    }
}
