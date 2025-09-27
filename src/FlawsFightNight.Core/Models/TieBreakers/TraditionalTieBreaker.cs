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
            //if (tiedTeams == null || tiedTeams.Count == 0) return ("null", "null"); 

            //Console.WriteLine("=== Tie-Breaker Resolution Started ===");
            StringBuilder tieBreakerLog = new();
            // Step 1: Head-to-head wins among tied teams
            var wins = tiedTeams.ToDictionary(t => t, t => 0);

            var headToHead = new List<PostMatch>();
            // If PostMatchesByRound is not empty, tournament is Normal Round Robin
            if (log.PostMatchesByRound.Count > 0)
            {
                headToHead = log.PostMatchesByRound
                    .SelectMany(kvp => kvp.Value)
                    .Where(pm =>
                        !pm.WasByeMatch &&
                        tiedTeams.Contains(pm.Winner) &&
                        tiedTeams.Contains(pm.Loser))
                    .ToList();
                Console.WriteLine($"PostMatchesByRound count: {log.PostMatchesByRound.Count}");
            }
            // If OpenRoundRobinPostMatches is not empty, tournament is Open Round Robin
            if (log.OpenRoundRobinPostMatches.Count > 0)
            {
                headToHead.AddRange(log.OpenRoundRobinPostMatches
                    .Where(pm =>
                        !pm.WasByeMatch &&
                        tiedTeams.Contains(pm.Winner) &&
                        tiedTeams.Contains(pm.Loser)));
                Console.WriteLine($"OpenRoundRobinPostMatches count: {log.OpenRoundRobinPostMatches.Count}");
            }

            //Console.WriteLine($"Step 1: Head-to-head matches found: {headToHead.Count}");
            tieBreakerLog.AppendLine($"Step 1: Head-to-head matches found: {headToHead.Count}");
            foreach (var pm in headToHead)
            {
                wins[pm.Winner]++;
                //Console.WriteLine($"  {pm.Winner} beat {pm.Loser} ({pm.WinnerScore}-{pm.LoserScore})");
                tieBreakerLog.AppendLine($"  {pm.Winner} beat {pm.Loser} ({pm.WinnerScore}-{pm.LoserScore})");
            }

            int maxWins = wins.Values.Max();
            var leaders = wins.Where(w => w.Value == maxWins).Select(w => w.Key).ToList();
            //Console.WriteLine($"  Leaders after Step 1 (max wins = {maxWins}): {string.Join(", ", leaders)}");
            tieBreakerLog.AppendLine($"  Leaders after Step 1 (max wins = {maxWins}): {string.Join(", ", leaders)}");
            if (leaders.Count == 1)
            {
                //Console.WriteLine($"Tie-breaker resolved by head-to-head wins → Winner: {leaders.First()}");
                tieBreakerLog.AppendLine($"Tie-breaker resolved by head-to-head wins → Winner: {leaders.First()}");
                return (tieBreakerLog.ToString(), leaders.First());
            }

            tieBreakerLog.AppendLine("\nMultiple leaders remain after Step 1, proceeding to Step 2: Point Differential Amount Tied Teams Matches");
            // Step 2: Point differential among tied teams matches
            var pointDiff = leaders.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (leaders.Contains(pm.Winner))
                {
                    int diffWinner = pm.WinnerScore - pm.LoserScore;
                    pointDiff[pm.Winner] += diffWinner;
                    //Console.WriteLine($"  Point diff for {pm.Winner}: {diffWinner:+#;-#;0}");
                    tieBreakerLog.AppendLine($"  Point diff for {pm.Winner}: {diffWinner:+#;-#;0}");
                }

                if (leaders.Contains(pm.Loser))
                {
                    int diffLoser = pm.LoserScore - pm.WinnerScore;
                    pointDiff[pm.Loser] += diffLoser;
                    //Console.WriteLine($"  Point diff for {pm.Loser}: {diffLoser:+#;-#;0}");
                    tieBreakerLog.AppendLine($"  Point diff for {pm.Loser}: {diffLoser:+#;-#;0}");
                }
            }

            int maxDiff = pointDiff.Values.Max();
            var leadersByDiff = pointDiff.Where(p => p.Value == maxDiff).Select(p => p.Key).ToList();
            //Console.WriteLine($"  Leaders after Step 2 (max point diff = {maxDiff}): {string.Join(", ", leadersByDiff)}");
            tieBreakerLog.AppendLine($"  Leaders after Step 2 (max point diff = {maxDiff}): {string.Join(", ", leadersByDiff)}");
            if (leadersByDiff.Count == 1)
            {
                //Console.WriteLine($"Tie-breaker resolved by point differential → Winner: {leadersByDiff.First()}");
                tieBreakerLog.AppendLine($"Tie-breaker resolved by point differential → Winner: {leadersByDiff.First()}");
                return (tieBreakerLog.ToString(), leadersByDiff.First());
            }

            tieBreakerLog.AppendLine("\nMultiple leaders remain after Step 2, proceeding to Step 3: Total Points Scored vs Tied Teams");
            // Step 3: Total points scored vs other tied teams
            var totalPointsVsTied = leadersByDiff.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (totalPointsVsTied.ContainsKey(pm.Winner))
                {
                    totalPointsVsTied[pm.Winner] += pm.WinnerScore;
                    //Console.WriteLine($"  Total points vs tied teams for {pm.Winner}: +{pm.WinnerScore}");
                    tieBreakerLog.AppendLine($"  Total points vs tied teams for {pm.Winner}: +{pm.WinnerScore}");
                }

                if (totalPointsVsTied.ContainsKey(pm.Loser))
                {
                    totalPointsVsTied[pm.Loser] += pm.LoserScore;
                    //Console.WriteLine($"  Total points vs tied teams for {pm.Loser}: +{pm.LoserScore}");
                    tieBreakerLog.AppendLine($"  Total points vs tied teams for {pm.Loser}: +{pm.LoserScore}");
                }
            }

            int maxPointsVsTied = totalPointsVsTied.Values.Max();
            var leadersByPointsVsTied = totalPointsVsTied
                .Where(p => p.Value == maxPointsVsTied)
                .Select(p => p.Key)
                .ToList();
            //Console.WriteLine($"  Leaders after Step 3 (max points vs tied = {maxPointsVsTied}): {string.Join(", ", leadersByPointsVsTied)}");
            tieBreakerLog.AppendLine($"  Leaders after Step 3 (max points vs tied = {maxPointsVsTied}): {string.Join(", ", leadersByPointsVsTied)}");
            if (leadersByPointsVsTied.Count == 1)
            {
                //Console.WriteLine($"Tie-breaker resolved by total points vs tied teams → Winner: {leadersByPointsVsTied.First()}");
                tieBreakerLog.AppendLine($"Tie-breaker resolved by total points vs tied teams → Winner: {leadersByPointsVsTied.First()}");
                return (tieBreakerLog.ToString(), leadersByPointsVsTied.First());
            }

            tieBreakerLog.AppendLine("\nMultiple leaders remain after Step 3, proceeding to Step 4: Total Points Scored Overall");
            // Step 4: Total points scored against all teams
            var allMatches = new List<PostMatch>();
            // If PostMatchesByRound is not empty, tournament is Normal Round Robin
            if (log.PostMatchesByRound.Count > 0)
            {
                allMatches.AddRange(log.PostMatchesByRound
                    .SelectMany(kvp => kvp.Value)
                    .Where(pm => !pm.WasByeMatch));
                Console.WriteLine($"PostMatchesByRound count: {log.PostMatchesByRound.Count}" );
            }
            // If OpenRoundRobinPostMatches is not empty, tournament is Open Round Robin
            if (log.OpenRoundRobinPostMatches.Count > 0)
            {
                allMatches.AddRange(log.OpenRoundRobinPostMatches
                    .Where(pm => !pm.WasByeMatch));
                Console.WriteLine($"OpenRoundRobinPostMatches count: {log.OpenRoundRobinPostMatches.Count}");
            }


            var totalPointsOverall = leadersByPointsVsTied.ToDictionary(t => t, t => 0);
            foreach (var pm in allMatches)
            {
                if (totalPointsOverall.ContainsKey(pm.Winner))
                    totalPointsOverall[pm.Winner] += pm.WinnerScore;

                if (totalPointsOverall.ContainsKey(pm.Loser))
                    totalPointsOverall[pm.Loser] += pm.LoserScore;
            }

            // After processing all matches, log once per player
            foreach (var kvp in totalPointsOverall)
            {
                tieBreakerLog.AppendLine($"  Total points overall for {kvp.Key}: {kvp.Value}");
            }

            int maxPointsOverall = totalPointsOverall.Values.Max();
            var leadersByPointsOverall = totalPointsOverall
                .Where(p => p.Value == maxPointsOverall)
                .Select(p => p.Key)
                .ToList();
            //Console.WriteLine($"  Leaders after Step 4 (max points overall = {maxPointsOverall}): {string.Join(", ", leadersByPointsOverall)}");
            tieBreakerLog.AppendLine($"  Leaders after Step 4 (max points overall = {maxPointsOverall}): {string.Join(", ", leadersByPointsOverall)}");
            if (leadersByPointsOverall.Count == 1)
            {
                //Console.WriteLine($"Tie-breaker resolved by total points overall → Winner: {leadersByPointsOverall.First()}");
                tieBreakerLog.AppendLine($"Tie-breaker resolved by total points overall → Winner: {leadersByPointsOverall.First()}");
                return (tieBreakerLog.ToString(), leadersByPointsOverall.First());
            }

            tieBreakerLog.AppendLine("\nMultiple leaders remain after Step 4, proceeding to Step 5: Least Points Against Overall");
            // Step 5: Least amount of points against
            var pointsAgainst = leadersByPointsOverall.ToDictionary(t => t, t => 0);
            foreach (var p in pointsAgainst)
            {
                var (forPoints, againstPoints) = log.GetPointsForAndPointsAgainstForTeam(p.Key);
                pointsAgainst[p.Key] = againstPoints;
                //Console.WriteLine($"  Points against for {p.Key}: {againstPoints}");
                tieBreakerLog.AppendLine($"  Points against for {p.Key}: {againstPoints}");
            }
            int minPointsAgainst = pointsAgainst.Values.Min();
            var leadersByLeastPointsAgainst = pointsAgainst
                .Where(p => p.Value == minPointsAgainst)
                .Select(p => p.Key)
                .ToList();
            //Console.WriteLine($"  Leaders after Step 5 (min points against = {minPointsAgainst}): {string.Join(", ", leadersByLeastPointsAgainst)}");
            tieBreakerLog.AppendLine($"  Leaders after Step 5 (min points against = {minPointsAgainst}): {string.Join(", ", leadersByLeastPointsAgainst)}");
            if (leadersByLeastPointsAgainst.Count == 1)
            {
                //Console.WriteLine($"Tie-breaker resolved by least points against → Winner: {leadersByLeastPointsAgainst.First()}");
                tieBreakerLog.AppendLine($"Tie-breaker resolved by least points against → Winner: {leadersByLeastPointsAgainst.First()}");
                return (tieBreakerLog.ToString(), leadersByLeastPointsAgainst.First());
            }

            tieBreakerLog.AppendLine("\nMultiple leaders remain after Step 5, proceeding to Step 6: 'Coin Flip'");
            // Step 6: Still tied — fallback random selection
            var chosen = leadersByPointsOverall.OrderBy(_ => Guid.NewGuid()).First();
            //Console.WriteLine($"Tie-breaker unresolved by all criteria → Randomly selected: {chosen}");
            tieBreakerLog.AppendLine($"Tie-breaker unresolved by all criteria → Randomly selected 'coin flip': {chosen}");
            return (tieBreakerLog.ToString(), chosen);
        }
    }
}
