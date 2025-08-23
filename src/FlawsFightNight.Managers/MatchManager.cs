using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class MatchManager : BaseDataDriven
    {
        class UnorderedPairComparer : IEqualityComparer<(string A, string B)>
        {
            // Super advanced, I got help with this one.
            public bool Equals((string A, string B) x, (string A, string B) y) =>
                (string.Equals(x.A, y.A, StringComparison.OrdinalIgnoreCase) && string.Equals(x.B, y.B, StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(x.A, y.B, StringComparison.OrdinalIgnoreCase) && string.Equals(x.B, y.A, StringComparison.OrdinalIgnoreCase));

            public int GetHashCode((string A, string B) obj)
            {
                var a = obj.A.ToLowerInvariant();
                var b = obj.B.ToLowerInvariant();
                var first = string.CompareOrdinal(a, b) <= 0 ? a : b;
                var second = string.CompareOrdinal(a, b) <= 0 ? b : a;
                return HashCode.Combine(first, second);
            }
        }

        public MatchManager(DataManager dataManager) : base("MatchManager", dataManager)
        {

        }

        public string? GenerateMatchId()
        {
            bool isUnique = false;
            string uniqueId;

            while (!isUnique)
            {
                Random random = new();
                int randomInt = random.Next(100, 1000);
                uniqueId = $"M{randomInt}";

                // Check if the generated ID is unique
                if (!IsMatchIdInDatabase(uniqueId))
                {
                    isUnique = true;
                    return uniqueId;
                }
            }
            return null;
        }

        #region Bools
        public bool IsMatchIdInDatabase(string matchId)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
                {
                    foreach (var match in round)
                    {
                        if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                        {
                            return true; // Match ID found
                        }
                    }
                }
                foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
                {
                    foreach (var match in round)
                    {
                        if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                        {
                            return true; // Match ID found
                        }
                    }
                }
            }
            return false;
        }

        public bool IsPostMatchInCurrentRound(Tournament tournament, string matchId)
        {
            if (tournament.MatchLog.PostMatchesByRound.TryGetValue(tournament.CurrentRound, out var matches))
            {
                foreach (var match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Match found in current round
                    }
                }
            }
            return false; // Match not found in current round
        }

        public bool HasByeMatchRemaining(Tournament tournament)
        {
            foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
            {
                foreach (var match in round)
                {
                    if (match.IsByeMatch)
                    {
                        return true; // Found a bye match
                    }
                }
            }
            return false; // No bye matches found
        }

        public bool HasMatchBeenPlayed(Tournament tournament, string matchId)
        {
            foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Match has been played
                    }
                }
            }
            return false; // Match not found in played matches
        }

        public bool IsGivenTeamNameInPostMatch(string teamName, PostMatch postMatch)
        {
            return (!string.IsNullOrEmpty(postMatch.Winner) && postMatch.Winner.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(postMatch.Loser) && postMatch.Loser.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsMatchMadeForTeam(Tournament tournament, string teamName)
        {
            if (tournament == null)
            {
                Console.WriteLine("Tournament is null.");
                return false;
            }

            if (tournament.MatchLog == null)
            {
                Console.WriteLine("MatchLog is null.");
                return false;
            }

            if (tournament.MatchLog.MatchesToPlayByRound == null)
            {
                Console.WriteLine("MatchesToPlayByRound is null.");
                return false;
            }

            int currentRound = tournament.CurrentRound;
            Console.WriteLine($"Checking only current round: {currentRound}");

            if (!tournament.MatchLog.MatchesToPlayByRound.TryGetValue(currentRound, out var matches))
            {
                Console.WriteLine($"No entry for round {currentRound} in MatchesToPlayByRound.");
                return false;
            }

            if (matches == null)
            {
                Console.WriteLine($"Match list for round {currentRound} is null.");
                return false;
            }

            foreach (var match in matches)
            {
                if (match == null)
                {
                    Console.WriteLine("Encountered null match in list, skipping.");
                    continue;
                }

                Console.WriteLine($"Checking match: TeamA = {match.TeamA}, TeamB = {match.TeamB}");

                if (!string.IsNullOrEmpty(match.TeamA) &&
                    match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Match found for team '{teamName}' as TeamA in round {currentRound}.");
                    return true;
                }

                if (!string.IsNullOrEmpty(match.TeamB) &&
                    match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Match found for team '{teamName}' as TeamB in round {currentRound}.");
                    return true;
                }
            }

            Console.WriteLine($"No match found for team '{teamName}' in round {currentRound}.");
            return false;
        }

        public bool IsTieBreakerNeeded(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("=== Debug: Starting Tie-Breaker Check ===");

            // Count wins for each team (single or double)
            foreach (var round in matchLog.PostMatchesByRound)
            {
                Console.WriteLine($"Checking Round {round.Key} with {round.Value.Count} matches");

                foreach (var postMatch in round.Value)
                {
                    if (!postMatch.WasByeMatch)
                    {
                        // Treat Winner as string directly
                        string winnerKey = postMatch.Winner ?? "UNKNOWN";

                        Console.WriteLine($"  Winner found: {winnerKey}");

                        if (!teamWins.ContainsKey(winnerKey))
                        {
                            teamWins[winnerKey] = 0;
                            Console.WriteLine($"  -> New entry created for {winnerKey}");
                        }

                        teamWins[winnerKey]++;
                        Console.WriteLine($"  -> {winnerKey} now has {teamWins[winnerKey]} wins");
                    }
                    else
                    {
                        Console.WriteLine("  Skipping bye match");
                    }
                }
            }

            Console.WriteLine("=== Debug: Final Team Wins ===");
            foreach (var kvp in teamWins)
            {
                Console.WriteLine($"Team {kvp.Key} : {kvp.Value} wins");
            }

            // Check for ties
            var winCounts = teamWins.Values.GroupBy(w => w).ToList();
            foreach (var group in winCounts)
            {
                Console.WriteLine($"Checking win count {group.Key} -> {group.Count()} teams");
                if (group.Count() > 1 && group.Key > 0) // Multiple teams with same non-zero wins
                {
                    Console.WriteLine("Tie detected! Tie-breaker needed.");
                    return true;
                }
            }

            Console.WriteLine("No ties detected. No tie-breaker needed.");
            return false;
        }
        #endregion

        #region Gets
        public Match GetMatchById(string matchId)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
                {
                    foreach (var match in round)
                    {
                        if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                        {
                            return match; // Match found
                        }
                    }
                }
            }
            return null; // Match ID not found
        }

        public Match GetMatchByIdInTournament(Tournament tournament, string matchId)
        {
            foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
            {
                foreach (var match in round)
                {
                    if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return match; // Match found
                    }
                }
            }
            return null; // Match ID not found
        }

        public Match GetMatchByTeamName(Tournament tournament, string teamName)
        {
            int currentRound = tournament.CurrentRound;
            Console.WriteLine($"Looking in round: {currentRound}");

            if (!tournament.MatchLog.MatchesToPlayByRound.TryGetValue(currentRound, out var matches))
            {
                Console.WriteLine($"No entry for round {currentRound} in MatchesToPlayByRound.");
                return null;
            }

            if (matches == null)
            {
                Console.WriteLine($"Match list for round {currentRound} is null.");
                return null;
            }

            foreach (var match in matches)
            {
                if (match == null)
                {
                    Console.WriteLine("Encountered null match in list, skipping.");
                    continue;
                }

                Console.WriteLine($"Checking match: TeamA = {match.TeamA}, TeamB = {match.TeamB}");

                if (!string.IsNullOrEmpty(match.TeamA) &&
                    match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }

                if (!string.IsNullOrEmpty(match.TeamB) &&
                    match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }
            }

            Console.WriteLine($"No match found for team '{teamName}' in round {currentRound}.");
            return null;
        }

        public string GetLosingTeamName(Match match, string winningTeamName)
        {
            if (match.TeamA != null && match.TeamA.Equals(winningTeamName, StringComparison.OrdinalIgnoreCase))
            {
                return match.TeamB;
            }
            else if (match.TeamB != null && match.TeamB.Equals(winningTeamName, StringComparison.OrdinalIgnoreCase))
            {
                return match.TeamA;
            }
            return null; // No losing team found
        }

        public string GetMostWinsWinner(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Count wins for each team
            foreach (var round in matchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!postMatch.WasByeMatch)
                    {
                        if (!teamWins.ContainsKey(postMatch.Winner))
                            teamWins[postMatch.Winner] = 0;
                        teamWins[postMatch.Winner]++;
                    }
                }
            }
            // Find the team with the most wins
            if (teamWins.Count == 0)
                return null;
            return teamWins.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        }

        public int GetNumberOfTeamsTied(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Count wins for each team
            foreach (var round in matchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!postMatch.WasByeMatch)
                    {
                        if (!teamWins.ContainsKey(postMatch.Winner))
                            teamWins[postMatch.Winner] = 0;
                        teamWins[postMatch.Winner]++;
                    }
                }
            }
            // Check for ties
            var winCounts = teamWins.Values.GroupBy(w => w).ToList();
            int tiedTeams = 0;
            foreach (var group in winCounts)
            {
                if (group.Count() > 1 && group.Key > 0) // More than one team with the same non-zero win count
                {
                    tiedTeams += group.Count();
                }
            }
            return tiedTeams; // Return number of teams involved in ties
        }

        public PostMatch GetPostMatchByIdInTournament(Tournament tournament, string matchId)
        {
            foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return postMatch; // PostMatch found
                    }
                }
            }
            return null; // PostMatch ID not found
        }

        public List<string> GetTiedTeams(MatchLog matchLog, bool isDoubleRoundRobin)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("=== Debug: Starting Tied Teams Check ===");

            // Count wins
            foreach (var roundKvp in matchLog.PostMatchesByRound)
            {
                Console.WriteLine($"Checking Round {roundKvp.Key} with {roundKvp.Value.Count} matches");
                foreach (var postMatch in roundKvp.Value)
                {
                    if (!postMatch.WasByeMatch)
                    {
                        string winnerKey = postMatch.Winner;

                        if (!teamWins.ContainsKey(winnerKey))
                        {
                            teamWins[winnerKey] = 0;
                            Console.WriteLine($"  -> New entry created for {winnerKey}");
                        }

                        teamWins[winnerKey]++;
                        Console.WriteLine($"  Winner found: {winnerKey} -> Total wins now: {teamWins[winnerKey]}");
                    }
                    else
                    {
                        Console.WriteLine("  Skipping bye match");
                    }
                }
            }

            if (teamWins.Count == 0)
            {
                Console.WriteLine("No wins recorded. Returning empty list.");
                return new List<string>();
            }

            // Check ties: any win count that occurs for 2 or more teams
            var winGroups = teamWins.Values.GroupBy(w => w).ToList();
            List<string> tiedTeams = new List<string>();

            foreach (var group in winGroups)
            {
                Console.WriteLine($"Checking win count {group.Key} -> {group.Count()} teams");
                if (group.Count() > 1 && group.Key > 0) // Only check for multiple teams with same wins
                {
                    var groupTeams = teamWins.Where(kvp => kvp.Value == group.Key).Select(kvp => kvp.Key).ToList();
                    tiedTeams.AddRange(groupTeams);
                    Console.WriteLine($"  Tie detected for teams: {string.Join(", ", groupTeams)}");
                }
            }

            Console.WriteLine($"=== Debug: Tied Teams Result ===\n{string.Join(", ", tiedTeams)}");
            return tiedTeams;
        }
        #endregion

        public void BuildMatchScheduleResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    // Ladder tournaments do not have a match schedule resolver
                    break;

                case TournamentType.RoundRobin:
                    BuildRoundRobinMatchSchedule(tournament, tournament.IsDoubleRoundRobin);
                    break;

                default:
                    Console.WriteLine($"Match schedule resolver not implemented for tournament type: {tournament.Type}");
                    break;
            }
        }

        #region Match/PostMatch Schedule Build/Validate/Clear
        public void BuildRoundRobinMatchSchedule(Tournament tournament, bool isDoubleRoundRobin = true)
        {
            const int maxRetries = 10;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                ClearMatchSchedule(tournament);

                var teams = tournament.Teams.Select(t => t.Name).ToList();
                bool hasBye = false;
                const string byePlaceholder = "BYE";
                if (teams.Count % 2 != 0)
                {
                    hasBye = true;
                    teams.Add(byePlaceholder);
                }

                int numRounds = teams.Count - 1;
                int half = teams.Count / 2;
                var rotating = new List<string>(teams); // first element fixed

                // Single Round Robin Logic
                for (int round = 1; round <= numRounds; round++)
                {
                    var pairings = new List<Match>();
                    for (int i = 0; i < half; i++)
                    {
                        string a = rotating[i];
                        string b = rotating[teams.Count - 1 - i];
                        if (a == byePlaceholder && b == byePlaceholder) continue;

                        bool isByeMatch = hasBye && (a == byePlaceholder || b == byePlaceholder);
                        var match = new Match(
                            a == byePlaceholder ? "BYE" : a,
                            b == byePlaceholder ? "BYE" : b)
                        {
                            Id = GenerateMatchId(),
                            IsByeMatch = isByeMatch,
                            RoundNumber = round,
                            CreatedOn = DateTime.UtcNow
                        };
                        pairings.Add(match);
                    }
                    tournament.MatchLog.MatchesToPlayByRound[round] = pairings;

                    // rotate teams, first element fixed
                    var last = rotating[^1];
                    rotating.RemoveAt(rotating.Count - 1);
                    rotating.Insert(1, last);
                }

                // Double Round Robin Logic
                if (isDoubleRoundRobin)
                {
                    int currentMaxRound = tournament.MatchLog.MatchesToPlayByRound.Count;
                    for (int round = 1; round <= currentMaxRound; round++)
                    {
                        var original = tournament.MatchLog.MatchesToPlayByRound[round];
                        var reversed = original.Select(m => new Match(m.TeamB, m.TeamA)
                        {
                            Id = GenerateMatchId(),
                            IsByeMatch = m.IsByeMatch,
                            RoundNumber = round + currentMaxRound,
                            CreatedOn = DateTime.UtcNow
                        }).ToList();

                        tournament.MatchLog.MatchesToPlayByRound[round + currentMaxRound] = reversed;
                    }
                }

                if (ValidateRoundRobin(tournament, isDoubleRoundRobin))
                {
                    tournament.TotalRounds = tournament.MatchLog.MatchesToPlayByRound.Count;
                    break;
                }
                else
                {
                    Console.WriteLine($"Validation failed on attempt {attempt}, retrying build...");
                }
            }
            if (attempt == maxRetries)
                Console.WriteLine("Failed to build a valid round-robin schedule after max retries.");
        }

        private bool ValidateRoundRobin(Tournament tournament, bool isDoubleRoundRobin)
        {
            var teams = tournament.Teams.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Build the set of all expected unique team pairings
            var expected = new HashSet<(string, string)>(new UnorderedPairComparer());
            for (int i = 0; i < teams.Count; i++)
                for (int j = i + 1; j < teams.Count; j++)
                    expected.Add((teams[i], teams[j]));

            var actual = new HashSet<(string, string)>(new UnorderedPairComparer());
            var conflicts = new List<string>();

            foreach (var kv in tournament.MatchLog.MatchesToPlayByRound.OrderBy(k => k.Key))
            {
                var seenThisRound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var match in kv.Value)
                {
                    if (match.IsByeMatch) continue;

                    if (match.TeamA != null && !seenThisRound.Add(match.TeamA))
                        conflicts.Add($"Round {kv.Key}: {match.TeamA} appears twice.");
                    if (match.TeamB != null && !seenThisRound.Add(match.TeamB))
                        conflicts.Add($"Round {kv.Key}: {match.TeamB} appears twice.");

                    if (match.TeamA != null && match.TeamB != null)
                        actual.Add((match.TeamA, match.TeamB));
                }
            }

            // For double round robin, allow each pair to appear twice
            var pairCounts = new Dictionary<(string, string), int>(new UnorderedPairComparer());
            foreach (var pair in actual)
            {
                if (!pairCounts.ContainsKey(pair)) pairCounts[pair] = 0;
                pairCounts[pair]++;
            }

            var duplicates = new List<(string, string)>();
            foreach (var kvp in pairCounts)
            {
                int allowed = isDoubleRoundRobin ? 2 : 1;
                if (kvp.Value > allowed)
                    duplicates.Add(kvp.Key);
            }

            var missing = expected.Except(actual, new UnorderedPairComparer()).ToList();
            var unexpected = actual.Except(expected, new UnorderedPairComparer()).ToList();

            if (!missing.Any() && !duplicates.Any() && !unexpected.Any() && !conflicts.Any())
                return true; // No issues, silent success

            // Only print actual errors
            if (missing.Any()) Console.WriteLine("Missing pairs: " + string.Join(", ", missing.Select(p => $"{p.Item1}-{p.Item2}")));
            if (duplicates.Any()) Console.WriteLine("Duplicate pairs: " + string.Join(", ", duplicates.Select(p => $"{p.Item1}-{p.Item2}")));
            if (unexpected.Any()) Console.WriteLine("Unexpected pairs: " + string.Join(", ", unexpected.Select(p => $"{p.Item1}-{p.Item2}")));
            if (conflicts.Any()) Console.WriteLine("Per-round conflicts: " + string.Join("; ", conflicts));

            return false;
        }

        public void ClearMatchSchedule(Tournament tournament)
        {
            // Clear the match schedule for the tournament
            tournament.MatchLog.MatchesToPlayByRound.Clear();
        }

        public PostMatch CreateNewPostMatch(string matchId, string winningTeamName, int winnerScore, string losingTeamName, int loserScore, DateTime originalCreationDateTime, bool wasByeMatch = false)
        {
            return new PostMatch(matchId, winningTeamName, winnerScore, losingTeamName, loserScore, originalCreationDateTime, wasByeMatch);
        }

        public void ConvertMatchToPostMatch(Tournament tournament, Match match, string winningTeamName, int winnerScore, string losingTeamName, int loserScore, bool wasByeMatch = false)
        {
            if (!tournament.MatchLog.MatchesToPlayByRound.ContainsKey(match.RoundNumber))
            {
                Console.WriteLine($"Round {match.RoundNumber} does not exist in the match schedule.");
                return;
            }
            var matchesInRound = tournament.MatchLog.MatchesToPlayByRound[match.RoundNumber];
            if (!matchesInRound.Contains(match))
            {
                Console.WriteLine("The specified match does not exist in the given round.");
                return;
            }

            // Create PostMatch
            PostMatch postMatch = CreateNewPostMatch(match.Id, winningTeamName, winnerScore, losingTeamName, loserScore, match.CreatedOn, wasByeMatch);

            // Add to PostMatchesByRound
            if (!tournament.MatchLog.PostMatchesByRound.ContainsKey(match.RoundNumber))
                tournament.MatchLog.PostMatchesByRound[match.RoundNumber] = new List<PostMatch>();
            tournament.MatchLog.PostMatchesByRound[match.RoundNumber].Add(postMatch);

            // Remove from MatchesToPlayByRound
            matchesInRound.Remove(match);

            // If no matches left in the round or a bye match is left, remove the round entry
            if (matchesInRound.Count == 0)
            {
                tournament.MatchLog.MatchesToPlayByRound.Remove(match.RoundNumber);

                switch (tournament.Type)
                {
                    case TournamentType.RoundRobin:
                        // If this was the last round, set IsRoundComplete to true
                        tournament.IsRoundComplete = true;
                        Console.WriteLine($"All matches for round {match.RoundNumber} have been completed. Admins now need to double check scores and then lock in the round before advancing.");
                        break;
                }
            }
        }
        #endregion

        #region Tiebreaker Methods
        public string ResolveTieBreaker(List<string> tiedTeams, MatchLog log)
        {
            Console.WriteLine("=== Tie-Breaker Resolution Started ===");

            // Step 1: Head-to-head wins among tied teams
            var wins = tiedTeams.ToDictionary(t => t, t => 0);

            var headToHead = log.PostMatchesByRound
                .SelectMany(kvp => kvp.Value)
                .Where(pm =>
                    !pm.WasByeMatch &&
                    tiedTeams.Contains(pm.Winner) &&
                    tiedTeams.Contains(pm.Loser))
                .ToList();

            Console.WriteLine($"Step 1: Head-to-head matches found: {headToHead.Count}");
            foreach (var pm in headToHead)
            {
                wins[pm.Winner]++;
                Console.WriteLine($"  {pm.Winner} beat {pm.Loser} ({pm.WinnerScore}-{pm.LoserScore})");
            }

            int maxWins = wins.Values.Max();
            var leaders = wins.Where(w => w.Value == maxWins).Select(w => w.Key).ToList();
            Console.WriteLine($"  Leaders after Step 1 (max wins = {maxWins}): {string.Join(", ", leaders)}");
            if (leaders.Count == 1)
            {
                Console.WriteLine($"Tie-breaker resolved by head-to-head wins → Winner: {leaders.First()}");
                return leaders.First();
            }

            // Step 2: Point differential among tied teams
            var pointDiff = leaders.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (leaders.Contains(pm.Winner))
                {
                    int diffWinner = pm.WinnerScore - pm.LoserScore;
                    pointDiff[pm.Winner] += diffWinner;
                    Console.WriteLine($"  Point diff for {pm.Winner}: {diffWinner:+#;-#;0}");
                }

                if (leaders.Contains(pm.Loser))
                {
                    int diffLoser = pm.LoserScore - pm.WinnerScore;
                    pointDiff[pm.Loser] += diffLoser;
                    Console.WriteLine($"  Point diff for {pm.Loser}: {diffLoser:+#;-#;0}");
                }
            }

            int maxDiff = pointDiff.Values.Max();
            var leadersByDiff = pointDiff.Where(p => p.Value == maxDiff).Select(p => p.Key).ToList();
            Console.WriteLine($"  Leaders after Step 2 (max point diff = {maxDiff}): {string.Join(", ", leadersByDiff)}");
            if (leadersByDiff.Count == 1)
            {
                Console.WriteLine($"Tie-breaker resolved by point differential → Winner: {leadersByDiff.First()}");
                return leadersByDiff.First();
            }

            // Step 3: Total points scored vs other tied teams
            var totalPointsVsTied = leadersByDiff.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (totalPointsVsTied.ContainsKey(pm.Winner))
                {
                    totalPointsVsTied[pm.Winner] += pm.WinnerScore;
                    Console.WriteLine($"  Total points vs tied teams for {pm.Winner}: +{pm.WinnerScore}");
                }

                if (totalPointsVsTied.ContainsKey(pm.Loser))
                {
                    totalPointsVsTied[pm.Loser] += pm.LoserScore;
                    Console.WriteLine($"  Total points vs tied teams for {pm.Loser}: +{pm.LoserScore}");
                }
            }

            int maxPointsVsTied = totalPointsVsTied.Values.Max();
            var leadersByPointsVsTied = totalPointsVsTied
                .Where(p => p.Value == maxPointsVsTied)
                .Select(p => p.Key)
                .ToList();
            Console.WriteLine($"  Leaders after Step 3 (max points vs tied = {maxPointsVsTied}): {string.Join(", ", leadersByPointsVsTied)}");
            if (leadersByPointsVsTied.Count == 1)
            {
                Console.WriteLine($"Tie-breaker resolved by total points vs tied teams → Winner: {leadersByPointsVsTied.First()}");
                return leadersByPointsVsTied.First();
            }

            // Step 4: Total points scored against all teams
            var allMatches = log.PostMatchesByRound
                .SelectMany(kvp => kvp.Value)
                .Where(pm => !pm.WasByeMatch);

            var totalPointsOverall = leadersByPointsVsTied.ToDictionary(t => t, t => 0);
            foreach (var pm in allMatches)
            {
                if (totalPointsOverall.ContainsKey(pm.Winner))
                {
                    totalPointsOverall[pm.Winner] += pm.WinnerScore;
                    Console.WriteLine($"  Total points overall for {pm.Winner}: +{pm.WinnerScore}");
                }

                if (totalPointsOverall.ContainsKey(pm.Loser))
                {
                    totalPointsOverall[pm.Loser] += pm.LoserScore;
                    Console.WriteLine($"  Total points overall for {pm.Loser}: +{pm.LoserScore}");
                }
            }

            int maxPointsOverall = totalPointsOverall.Values.Max();
            var leadersByPointsOverall = totalPointsOverall
                .Where(p => p.Value == maxPointsOverall)
                .Select(p => p.Key)
                .ToList();
            Console.WriteLine($"  Leaders after Step 4 (max points overall = {maxPointsOverall}): {string.Join(", ", leadersByPointsOverall)}");
            if (leadersByPointsOverall.Count == 1)
            {
                Console.WriteLine($"Tie-breaker resolved by total points overall → Winner: {leadersByPointsOverall.First()}");
                return leadersByPointsOverall.First();
            }

            // Step 5: Still tied — fallback random selection
            var chosen = leadersByPointsOverall.OrderBy(_ => Guid.NewGuid()).First();
            Console.WriteLine($"Tie-breaker unresolved by all criteria → Randomly selected: {chosen}");
            return chosen;
        }
        #endregion
    }
}
