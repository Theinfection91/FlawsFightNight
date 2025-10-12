using FlawsFightNight.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.TieBreakers
{
    public class TraditionalTieBreaker : ITieBreakerRule
    {
        public string Name => "Traditional";
        public (string, string) ResolveTie(List<string> tiedTeams, MatchLog log)
        {
            StringBuilder tieBreakerLog = new();
            if (tiedTeams == null || tiedTeams.Count == 0)
            {
                tieBreakerLog.AppendLine("No tied teams provided.");
                return (tieBreakerLog.ToString(), "null");
            }

            // Step 1: Head-to-head wins among tied teams
            var wins = tiedTeams.ToDictionary(t => t, t => 0);

            var headToHead = new List<PostMatch>();
            if (log.PostMatchesByRound.Count > 0)
            {
                headToHead.AddRange(log.PostMatchesByRound
                    .SelectMany(kvp => kvp.Value)
                    .Where(pm => !pm.WasByeMatch &&
                                 tiedTeams.Contains(pm.Winner) &&
                                 tiedTeams.Contains(pm.Loser)));
            }
            if (log.OpenRoundRobinPostMatches.Count > 0)
            {
                headToHead.AddRange(log.OpenRoundRobinPostMatches
                    .Where(pm => !pm.WasByeMatch &&
                                 tiedTeams.Contains(pm.Winner) &&
                                 tiedTeams.Contains(pm.Loser)));
            }

            tieBreakerLog.AppendLine($"Step 1: Head-to-head matches found: {headToHead.Count}");
            foreach (var pm in headToHead)
            {
                wins[pm.Winner]++;
                tieBreakerLog.AppendLine($"  {pm.Winner} beat {pm.Loser} ({pm.WinnerScore}-{pm.LoserScore})");
            }

            if (!wins.Any())
            {
                tieBreakerLog.AppendLine("No wins recorded → returning unresolved tie");
                return (tieBreakerLog.ToString(), tiedTeams.First());
            }

            int maxWins = wins.Values.Max();
            var leaders = wins.Where(w => w.Value == maxWins).Select(w => w.Key).ToList();
            tieBreakerLog.AppendLine($"  Leaders after Step 1 (max wins = {maxWins}): {string.Join(", ", leaders)}");

            if (leaders.Count == 1) return (tieBreakerLog.ToString(), leaders.First());

            // Step 2: Point differential among tied teams
            var pointDiff = leaders.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (pointDiff.ContainsKey(pm.Winner)) pointDiff[pm.Winner] += pm.WinnerScore - pm.LoserScore;
                if (pointDiff.ContainsKey(pm.Loser)) pointDiff[pm.Loser] += pm.LoserScore - pm.WinnerScore;
            }

            if (!pointDiff.Any())
            {
                tieBreakerLog.AppendLine("No point differential data → unresolved tie");
                return (tieBreakerLog.ToString(), leaders.First());
            }

            int maxDiff = pointDiff.Values.Max();
            var leadersByDiff = pointDiff.Where(p => p.Value == maxDiff).Select(p => p.Key).ToList();
            tieBreakerLog.AppendLine($"  Leaders after Step 2 (max point diff = {maxDiff}): {string.Join(", ", leadersByDiff)}");
            if (leadersByDiff.Count == 1) return (tieBreakerLog.ToString(), leadersByDiff.First());

            // Step 3: Total points scored vs tied teams
            var totalPointsVsTied = leadersByDiff.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (totalPointsVsTied.ContainsKey(pm.Winner)) totalPointsVsTied[pm.Winner] += pm.WinnerScore;
                if (totalPointsVsTied.ContainsKey(pm.Loser)) totalPointsVsTied[pm.Loser] += pm.LoserScore;
            }

            if (!totalPointsVsTied.Any())
            {
                tieBreakerLog.AppendLine("No points vs tied teams → unresolved tie");
                return (tieBreakerLog.ToString(), leadersByDiff.First());
            }

            int maxPointsVsTied = totalPointsVsTied.Values.Max();
            var leadersByPointsVsTied = totalPointsVsTied.Where(p => p.Value == maxPointsVsTied).Select(p => p.Key).ToList();
            tieBreakerLog.AppendLine($"  Leaders after Step 3 (max points vs tied = {maxPointsVsTied}): {string.Join(", ", leadersByPointsVsTied)}");
            if (leadersByPointsVsTied.Count == 1) return (tieBreakerLog.ToString(), leadersByPointsVsTied.First());

            // Step 4: Total points overall
            var allMatches = log.PostMatchesByRound.Values.SelectMany(v => v)
                                .Concat(log.OpenRoundRobinPostMatches)
                                .Where(pm => !pm.WasByeMatch)
                                .ToList();

            var totalPointsOverall = leadersByPointsVsTied.ToDictionary(t => t, t => 0);
            foreach (var pm in allMatches)
            {
                if (totalPointsOverall.ContainsKey(pm.Winner)) totalPointsOverall[pm.Winner] += pm.WinnerScore;
                if (totalPointsOverall.ContainsKey(pm.Loser)) totalPointsOverall[pm.Loser] += pm.LoserScore;
            }

            if (!totalPointsOverall.Any())
            {
                tieBreakerLog.AppendLine("No overall points → unresolved tie");
                return (tieBreakerLog.ToString(), leadersByPointsVsTied.First());
            }

            int maxPointsOverall = totalPointsOverall.Values.Max();
            var leadersByPointsOverall = totalPointsOverall.Where(p => p.Value == maxPointsOverall).Select(p => p.Key).ToList();
            tieBreakerLog.AppendLine($"  Leaders after Step 4 (max points overall = {maxPointsOverall}): {string.Join(", ", leadersByPointsOverall)}");
            if (leadersByPointsOverall.Count == 1) return (tieBreakerLog.ToString(), leadersByPointsOverall.First());

            // Step 5: Least points against
            var pointsAgainst = leadersByPointsOverall.ToDictionary(t => t, t => 0);
            foreach (var p in pointsAgainst.Keys.ToList())
            {
                var (_, againstPoints) = log.GetPointsForAndPointsAgainstForTeam(p);
                pointsAgainst[p] = againstPoints;
            }

            if (!pointsAgainst.Any())
            {
                tieBreakerLog.AppendLine("No points against → unresolved tie");
                return (tieBreakerLog.ToString(), leadersByPointsOverall.First());
            }

            int minPointsAgainst = pointsAgainst.Values.Min();
            var leadersByLeastPointsAgainst = pointsAgainst.Where(p => p.Value == minPointsAgainst).Select(p => p.Key).ToList();
            tieBreakerLog.AppendLine($"  Leaders after Step 5 (min points against = {minPointsAgainst}): {string.Join(", ", leadersByLeastPointsAgainst)}");
            if (leadersByLeastPointsAgainst.Count == 1) return (tieBreakerLog.ToString(), leadersByLeastPointsAgainst.First());

            // Step 6: Coin flip fallback
            var chosen = leadersByPointsOverall.OrderBy(_ => Guid.NewGuid()).First();
            tieBreakerLog.AppendLine($"Tie-breaker unresolved by all criteria → Randomly selected 'coin flip': {chosen}");

            // TODO Remove later DEBUG
            Console.WriteLine($"{leadersByDiff.First()}");

            return (tieBreakerLog.ToString(), chosen);
        }
    }
}
